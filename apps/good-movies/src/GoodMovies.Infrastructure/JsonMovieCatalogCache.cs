using System.Collections.Concurrent;
using System.Text.Json;
using GoodMovies.Core;

namespace GoodMovies.Infrastructure;

internal sealed class CatalogCacheException : IOException
{
    public CatalogCacheException(string message)
        : base(message) { }

    public CatalogCacheException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// JSON catalog cache with policy revalidation on every read.
/// </summary>
internal sealed class JsonMovieCatalogCache : IMovieCatalogCache
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly IFileSystemPathProvider _pathProvider;
    private readonly GoodMoviesInfrastructureOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly IAtomicFileWriter _atomicFileWriter;
    private readonly PosterUrlBuilder _posterUrlBuilder;
    private readonly SemaphoreSlim _gate;

    public JsonMovieCatalogCache(
        IFileSystemPathProvider pathProvider,
        GoodMoviesInfrastructureOptions? options = null,
        TimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null
    )
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _options = options ?? new GoodMoviesInfrastructureOptions();
        _options.Validate();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _atomicFileWriter = atomicFileWriter ?? new AtomicFileWriter();
        _posterUrlBuilder = new PosterUrlBuilder(_options);
        _gate = Gates.GetOrAdd(GetCachePath(), static _ => new SemaphoreSlim(1, 1));
    }

    public async Task<CatalogCacheReadResult> ReadAsync(
        DateOnly today,
        CancellationToken cancellationToken = default
    )
    {
        string path = GetCachePath();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(path))
            {
                return CatalogCacheReadResult.NoCache();
            }

            CatalogCacheDocument? document;
            try
            {
                await using FileStream stream = new(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 16 * 1024,
                    options: FileOptions.SequentialScan
                );
                document = await JsonSerializer
                    .DeserializeAsync(
                        stream,
                        GoodMoviesJsonContext.Default.CatalogCacheDocument,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (JsonException exception)
            {
                return CatalogCacheReadResult.Failure(
                    CatalogCacheStatus.Corrupted,
                    new CatalogCacheException("The catalog cache contains invalid JSON.", exception)
                );
            }
            catch (FileNotFoundException)
            {
                return CatalogCacheReadResult.NoCache();
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                return CatalogCacheReadResult.Failure(
                    CatalogCacheStatus.ReadFailed,
                    new CatalogCacheException("The catalog cache could not be read.", exception)
                );
            }

            if (document is null || document.RefreshedAt == default || document.Movies is null)
            {
                return CatalogCacheReadResult.Failure(
                    CatalogCacheStatus.Corrupted,
                    new CatalogCacheException("The catalog cache document is incomplete.")
                );
            }

            MovieCatalogSnapshot snapshot;
            try
            {
                Movie[] cachedMovies = document
                    .Movies.Where(static movie => movie is not null)
                    .Select(ToMovie)
                    .Where(static movie => movie is not null)
                    .Cast<Movie>()
                    .ToArray();
                snapshot = new MovieCatalogSnapshot(cachedMovies, today);
            }
            catch (Exception exception)
            {
                return CatalogCacheReadResult.Failure(
                    CatalogCacheStatus.Corrupted,
                    new CatalogCacheException(
                        "The catalog cache document could not be interpreted.",
                        exception
                    )
                );
            }

            TimeSpan age = _timeProvider.GetUtcNow() - document.RefreshedAt;
            if (age < TimeSpan.Zero)
            {
                age = TimeSpan.Zero;
            }

            return CatalogCacheReadResult.Available(snapshot.Movies, age >= _options.CacheLifetime);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CatalogCacheWriteResult> WriteAsync(
        MovieCatalogSnapshot snapshot,
        DateTimeOffset refreshedAt,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (refreshedAt == default)
        {
            return new CatalogCacheWriteResult(
                CatalogCacheWriteStatus.Failed,
                error: new CatalogCacheException("A successful refresh timestamp is required.")
            );
        }

        string path = GetCachePath();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            MovieCatalogSnapshot safeSnapshot = new MovieCatalogSnapshot(
                snapshot.Movies,
                snapshot.AsOfDate
            );
            CatalogCacheDocument document = new()
            {
                RefreshedAt = refreshedAt.ToUniversalTime(),
                Movies = safeSnapshot.Movies.Select(ToCachedMovie).ToList(),
            };

            try
            {
                await _atomicFileWriter
                    .WriteAsync(
                        path,
                        stream =>
                            JsonSerializer.SerializeAsync(
                                stream,
                                document,
                                GoodMoviesJsonContext.Default.CatalogCacheDocument,
                                cancellationToken
                            ),
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                return new CatalogCacheWriteResult(CatalogCacheWriteStatus.Succeeded);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return new CatalogCacheWriteResult(
                    CatalogCacheWriteStatus.Failed,
                    error: new CatalogCacheException(
                        "The catalog cache could not be written.",
                        exception
                    )
                );
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private string GetCachePath() => _pathProvider.GetPath(_options.CatalogCacheFileName);

    private CachedMovie ToCachedMovie(Movie movie)
    {
        Uri? posterUri = _posterUrlBuilder.Build(movie.PosterPath);
        return new CachedMovie
        {
            Id = movie.Id,
            Title = movie.Title,
            Overview = movie.Overview,
            PosterPath = posterUri is null ? null : movie.PosterPath,
            OriginalLanguage = movie.OriginalLanguage,
            Certification = movie.Certification?.Code,
            GenreIds = movie.GenreIds.ToList(),
            Genres = movie
                .Genres.Select(static genre => new CachedGenre { Id = genre.Id, Name = genre.Name })
                .ToList(),
            Releases = movie
                .Releases.Select(static release => new CachedRelease
                {
                    ReleaseDate = release.ReleaseDate,
                    CountryCode = release.CountryCode,
                    ReleaseType = release.ReleaseType,
                })
                .ToList(),
        };
    }

    private Movie? ToMovie(CachedMovie? cachedMovie)
    {
        if (cachedMovie is null || cachedMovie.Id <= 0)
        {
            return null;
        }

        MovieGenre[] genres = (cachedMovie.Genres ?? new List<CachedGenre>())
            .Where(static genre => genre is not null)
            .Select(static genre => new MovieGenre(genre.Id, genre.Name))
            .ToArray();
        TheatricalRelease[] releases = (cachedMovie.Releases ?? new List<CachedRelease>())
            .Where(static release => release is not null)
            .Select(static release => new TheatricalRelease(
                release.ReleaseDate,
                release.CountryCode,
                release.ReleaseType
            ))
            .ToArray();
        if (
            releases.Length == 0
            && cachedMovie.LegacyUsTheatricalReleaseDate is DateOnly legacyReleaseDate
        )
        {
            releases =
            [
                new TheatricalRelease(legacyReleaseDate, "US", TheatricalRelease.TheatricalType),
            ];
        }

        Uri? posterUri = _posterUrlBuilder.Build(cachedMovie.PosterPath);
        string? safePosterPath = posterUri is null ? null : cachedMovie.PosterPath;

        return new Movie(
            cachedMovie.Id,
            cachedMovie.Title,
            cachedMovie.Certification,
            releases,
            genres,
            cachedMovie.Overview,
            safePosterPath,
            posterUri,
            cachedMovie.OriginalLanguage,
            cachedMovie.GenreIds
        );
    }
}
