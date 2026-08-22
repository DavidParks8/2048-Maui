using System.Collections.Concurrent;
using System.Text.Json;
using GoodMovies.Core;

namespace GoodMovies.Infrastructure;

public sealed class CatalogCacheException : IOException
{
    public CatalogCacheException(string message)
        : base(message) { }

    public CatalogCacheException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// JSON catalog cache with policy revalidation on every read.
/// </summary>
public class JsonMovieCatalogCache : IMovieCatalogCache
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly IFileSystemPathProvider? _pathProvider;
    private readonly string? _explicitPath;
    private readonly GoodMoviesInfrastructureOptions _options;
    private readonly IGoodMoviesTimeProvider _timeProvider;
    private readonly IAtomicFileWriter _atomicFileWriter;
    private readonly PosterUrlBuilder _posterUrlBuilder;
    private readonly IClock _clock;
    private readonly ReleaseWindowPolicy _releaseWindowPolicy;
    private readonly MovieSafetyPolicy _movieSafetyPolicy;
    private readonly SemaphoreSlim _gate;

    public JsonMovieCatalogCache(
        IFileSystemPathProvider pathProvider,
        GoodMoviesInfrastructureOptions? options = null,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _options = options ?? new GoodMoviesInfrastructureOptions();
        _options.Validate();
        _clock = clock ?? new SystemClock();
        _timeProvider = timeProvider ?? new SystemGoodMoviesTimeProvider();
        _atomicFileWriter = atomicFileWriter ?? new AtomicFileWriter();
        _posterUrlBuilder = new PosterUrlBuilder(_options);
        _releaseWindowPolicy = releaseWindowPolicy ?? ReleaseWindowPolicy.Default;
        _movieSafetyPolicy = movieSafetyPolicy ?? new MovieSafetyPolicy();
        _gate = Gates.GetOrAdd(GetCachePath(), static _ => new SemaphoreSlim(1, 1));
    }

    public JsonMovieCatalogCache(
        string filePath,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null,
        TimeSpan? cacheLifetime = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : this(
            ResolvePath(filePath, "good-movies-catalog.json"),
            new GoodMoviesInfrastructureOptions
            {
                CatalogCacheFileName = Path.GetFileName(
                    ResolvePath(filePath, "good-movies-catalog.json")
                ),
                CacheLifetime = cacheLifetime ?? TimeSpan.FromHours(6),
            },
            clock,
            timeProvider,
            atomicFileWriter,
            releaseWindowPolicy,
            movieSafetyPolicy,
            explicitPath: true
        ) { }

    public JsonMovieCatalogCache(
        string directoryPath,
        GoodMoviesInfrastructureOptions options,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : this(
            ResolvePath(directoryPath, options?.CatalogCacheFileName ?? "good-movies-catalog.json"),
            options ?? new GoodMoviesInfrastructureOptions(),
            clock,
            timeProvider,
            atomicFileWriter,
            releaseWindowPolicy,
            movieSafetyPolicy,
            explicitPath: true
        ) { }

    private JsonMovieCatalogCache(
        string filePath,
        GoodMoviesInfrastructureOptions options,
        IClock? clock,
        IGoodMoviesTimeProvider? timeProvider,
        IAtomicFileWriter? atomicFileWriter,
        ReleaseWindowPolicy? releaseWindowPolicy,
        MovieSafetyPolicy? movieSafetyPolicy,
        bool explicitPath
    )
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A cache file path is required.", nameof(filePath));
        }

        _explicitPath = Path.GetFullPath(filePath);
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _clock = clock ?? new SystemClock();
        _timeProvider = timeProvider ?? new SystemGoodMoviesTimeProvider();
        _atomicFileWriter = atomicFileWriter ?? new AtomicFileWriter();
        _posterUrlBuilder = new PosterUrlBuilder(_options);
        _releaseWindowPolicy = releaseWindowPolicy ?? ReleaseWindowPolicy.Default;
        _movieSafetyPolicy = movieSafetyPolicy ?? new MovieSafetyPolicy();
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
            catch (IOException exception)
            {
                return CatalogCacheReadResult.Failure(
                    CatalogCacheStatus.ReadFailed,
                    new CatalogCacheException("The catalog cache could not be read.", exception)
                );
            }
            catch (UnauthorizedAccessException exception)
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
                snapshot = MovieCatalogSnapshot.Create(
                    cachedMovies,
                    today,
                    _releaseWindowPolicy,
                    _movieSafetyPolicy
                );
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

            TimeSpan age = _timeProvider.UtcNow - document.RefreshedAt;
            if (age < TimeSpan.Zero)
            {
                age = TimeSpan.Zero;
            }

            return CatalogCacheReadResult.Available(
                snapshot.Movies,
                document.RefreshedAt,
                age,
                age >= _options.CacheLifetime
            );
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
            MovieCatalogSnapshot safeSnapshot = MovieCatalogSnapshot.Create(
                snapshot.Movies,
                snapshot.AsOfDate ?? _clock.Today,
                _releaseWindowPolicy,
                _movieSafetyPolicy
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
                return new CatalogCacheWriteResult(
                    CatalogCacheWriteStatus.Succeeded,
                    document.RefreshedAt
                );
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

    public Task<CatalogCacheReadResult> LoadAsync(
        DateOnly today,
        CancellationToken cancellationToken = default
    ) => ReadAsync(today, cancellationToken);

    public Task<CatalogCacheReadResult> LoadAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(_clock.Today, cancellationToken);

    public Task<CatalogCacheReadResult> ReadAsync(CancellationToken cancellationToken = default) =>
        ReadAsync(_clock.Today, cancellationToken);

    public Task<CatalogCacheWriteResult> SaveAsync(
        MovieCatalogSnapshot snapshot,
        DateTimeOffset refreshedAt,
        CancellationToken cancellationToken = default
    ) => WriteAsync(snapshot, refreshedAt, cancellationToken);

    public async Task<CatalogCacheWriteResult> WriteAsync(
        IEnumerable<Movie> movies,
        DateOnly today,
        DateTimeOffset refreshedAt,
        CancellationToken cancellationToken = default
    ) =>
        await WriteAsync(
                MovieCatalogSnapshot.Create(
                    movies ?? Array.Empty<Movie>(),
                    today,
                    _releaseWindowPolicy,
                    _movieSafetyPolicy
                ),
                refreshedAt,
                cancellationToken
            )
            .ConfigureAwait(false);

    public Task<CatalogCacheWriteResult> SaveAsync(
        IEnumerable<Movie> movies,
        DateOnly today,
        DateTimeOffset refreshedAt,
        CancellationToken cancellationToken = default
    ) => WriteAsync(movies, today, refreshedAt, cancellationToken);

    public Task<CatalogCacheWriteResult> SaveAsync(
        IEnumerable<Movie> movies,
        DateTimeOffset refreshedAt,
        CancellationToken cancellationToken = default
    ) => WriteAsync(movies, _clock.Today, refreshedAt, cancellationToken);

    private string GetCachePath() =>
        _explicitPath ?? _pathProvider!.GetPath(_options.CatalogCacheFileName);

    private static string ResolvePath(string path, string defaultFileName) =>
        Directory.Exists(path) || string.IsNullOrEmpty(Path.GetExtension(path))
            ? Path.Combine(path, defaultFileName)
            : path;

    private CachedMovie ToCachedMovie(Movie movie)
    {
        Uri? posterUri = _posterUrlBuilder.Build(movie.PosterPath);
        return new CachedMovie
        {
            Id = movie.Id,
            Title = movie.Title,
            Overview = movie.Overview,
            PosterPath = posterUri is null ? null : movie.PosterPath,
            PosterUri = posterUri?.ToString(),
            OriginalLanguage = movie.OriginalLanguage,
            Certification = movie.CertificationCode,
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
            Trailers = movie
                .Trailers.Select(static trailer => new CachedTrailer
                {
                    Key = trailer.Key,
                    Name = trailer.Name,
                    Site = trailer.Site,
                    Type = trailer.Type,
                    IsOfficial = trailer.IsOfficial,
                    LanguageCode = trailer.LanguageCode,
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
        MovieTrailer[] trailers = (cachedMovie.Trailers ?? new List<CachedTrailer>())
            .Where(static trailer => trailer is not null)
            .Select(static trailer => new MovieTrailer(
                trailer.Key,
                trailer.Name,
                trailer.Site,
                trailer.Type,
                trailer.IsOfficial,
                trailer.LanguageCode
            ))
            .ToArray();
        Uri? posterUri = _posterUrlBuilder.Build(cachedMovie.PosterPath);
        string? safePosterPath = posterUri is null ? null : cachedMovie.PosterPath;

        return new Movie(
            cachedMovie.Id,
            cachedMovie.Title,
            cachedMovie.Certification,
            releases,
            genres,
            trailers,
            cachedMovie.Overview,
            safePosterPath,
            posterUri?.ToString(),
            cachedMovie.OriginalLanguage,
            cachedMovie.GenreIds
        );
    }
}

/// <summary>
/// Friendly alias for callers that name the implementation by its storage
/// mechanism.
/// </summary>
public class FileMovieCatalogCache : JsonMovieCatalogCache
{
    public FileMovieCatalogCache(
        IFileSystemPathProvider pathProvider,
        GoodMoviesInfrastructureOptions? options = null,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : base(
            pathProvider,
            options,
            clock,
            timeProvider,
            atomicFileWriter,
            releaseWindowPolicy,
            movieSafetyPolicy
        ) { }

    public FileMovieCatalogCache(
        string filePath,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null,
        TimeSpan? cacheLifetime = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : base(
            filePath,
            clock,
            timeProvider,
            atomicFileWriter,
            cacheLifetime,
            releaseWindowPolicy,
            movieSafetyPolicy
        ) { }
}

public class FileCatalogCache : JsonMovieCatalogCache
{
    public FileCatalogCache(
        IFileSystemPathProvider pathProvider,
        GoodMoviesInfrastructureOptions? options = null,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : base(
            pathProvider,
            options,
            clock,
            timeProvider,
            atomicFileWriter,
            releaseWindowPolicy,
            movieSafetyPolicy
        ) { }

    public FileCatalogCache(
        string filePath,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null,
        TimeSpan? cacheLifetime = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : base(
            filePath,
            clock,
            timeProvider,
            atomicFileWriter,
            cacheLifetime,
            releaseWindowPolicy,
            movieSafetyPolicy
        ) { }
}
