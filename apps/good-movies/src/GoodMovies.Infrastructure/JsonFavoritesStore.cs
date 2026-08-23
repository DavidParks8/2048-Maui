using System.Collections.Concurrent;
using System.Text.Json;
using GoodMovies.Core;

namespace GoodMovies.Infrastructure;

internal sealed class FavoritesStoreException : IOException
{
    public FavoritesStoreException(string message)
        : base(message) { }

    public FavoritesStoreException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Favorites storage coordinated across in-process instances and written atomically. Only the ID and verified US release
/// date are persisted, which keeps offline pruning independent of the catalog.
/// </summary>
internal sealed class JsonFavoritesStore : IFavoritesStore
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly IFileSystemPathProvider _pathProvider;
    private readonly GoodMoviesInfrastructureOptions _options;
    private readonly IAtomicFileWriter _atomicFileWriter;
    private readonly SemaphoreSlim _gate;

    public JsonFavoritesStore(
        IFileSystemPathProvider pathProvider,
        GoodMoviesInfrastructureOptions? options = null,
        IAtomicFileWriter? atomicFileWriter = null
    )
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _options = options ?? new GoodMoviesInfrastructureOptions();
        _options.Validate();
        _atomicFileWriter = atomicFileWriter ?? new AtomicFileWriter();
        _gate = Gates.GetOrAdd(GetFavoritesPath(), static _ => new SemaphoreSlim(1, 1));
    }

    public async Task<FavoritesResult> GetAsync(
        DateOnly today,
        CancellationToken cancellationToken = default
    )
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            FavoritesRead read = await ReadRawAsync(cancellationToken).ConfigureAwait(false);
            if (!read.Succeeded)
            {
                return FavoritesResult.Failure(read.Status, read.Error!);
            }

            FavoriteEntry[] visible = FilterVisible(read.Entries, today).ToArray();
            if (!read.Entries.SequenceEqual(visible))
            {
                Exception? writeError = await WriteRawAsync(visible, cancellationToken)
                    .ConfigureAwait(false);
                if (writeError is not null)
                {
                    return FavoritesResult.Failure(FavoritesResultStatus.Failed, writeError);
                }
            }

            return FavoritesResult.Success(visible);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<FavoriteToggleResult> ToggleAsync(
        FavoriteEntry favorite,
        DateOnly today,
        CancellationToken cancellationToken = default
    )
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            FavoritesRead read = await ReadRawAsync(cancellationToken).ConfigureAwait(false);
            if (!read.Succeeded)
            {
                return new FavoriteToggleResult(FavoriteToggleStatus.Failed, error: read.Error);
            }

            List<FavoriteEntry> entries = FilterVisible(read.Entries, today).ToList();
            bool wasFavorite = entries.Any(entry => entry.MovieId == favorite.MovieId);
            FavoriteToggleStatus status;

            if (wasFavorite)
            {
                entries.RemoveAll(entry => entry.MovieId == favorite.MovieId);
                status = FavoriteToggleStatus.Removed;
            }
            else if (favorite.MovieId <= 0 || !ReleaseWindowPolicy.IsVisible(favorite, today))
            {
                status = FavoriteToggleStatus.Rejected;
            }
            else
            {
                entries.Add(favorite);
                entries.Sort(static (left, right) => left.MovieId.CompareTo(right.MovieId));
                status = FavoriteToggleStatus.Added;
            }

            if (
                status is FavoriteToggleStatus.Added or FavoriteToggleStatus.Removed
                || !read.Entries.SequenceEqual(entries)
            )
            {
                Exception? writeError = await WriteRawAsync(entries, cancellationToken)
                    .ConfigureAwait(false);
                if (writeError is not null)
                {
                    return new FavoriteToggleResult(FavoriteToggleStatus.Failed, writeError);
                }
            }

            return new FavoriteToggleResult(status);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<FavoritesResult> ReconcileAsync(
        IEnumerable<Movie> refreshedMovies,
        DateOnly today,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(refreshedMovies);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            FavoritesRead read = await ReadRawAsync(cancellationToken).ConfigureAwait(false);
            if (!read.Succeeded)
            {
                return FavoritesResult.Failure(read.Status, read.Error!);
            }

            Dictionary<int, FavoriteEntry> refreshed = new();
            foreach (Movie? movie in refreshedMovies)
            {
                TheatricalRelease? release = ReleaseWindowPolicy.GetVisibleRelease(movie, today);
                if (movie is not null && release is not null && MovieSafetyPolicy.IsSafe(movie))
                {
                    FavoriteEntry entry = new(movie.Id, release.ReleaseDate);
                    refreshed[entry.MovieId] = entry;
                }
            }

            FavoriteEntry[] reconciled = FilterVisible(read.Entries, today)
                .Where(entry => refreshed.ContainsKey(entry.MovieId))
                .Select(entry => refreshed[entry.MovieId])
                .ToArray();

            if (!read.Entries.SequenceEqual(reconciled))
            {
                Exception? writeError = await WriteRawAsync(reconciled, cancellationToken)
                    .ConfigureAwait(false);
                if (writeError is not null)
                {
                    return FavoritesResult.Failure(FavoritesResultStatus.Failed, writeError);
                }
            }

            return FavoritesResult.Success(reconciled);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<FavoritesRead> ReadRawAsync(CancellationToken cancellationToken)
    {
        string path = GetFavoritesPath();
        if (!File.Exists(path))
        {
            return FavoritesRead.Success(Array.Empty<FavoriteEntry>());
        }

        try
        {
            await using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 8 * 1024,
                options: FileOptions.SequentialScan
            );
            List<FavoriteFileEntry>? values = await JsonSerializer
                .DeserializeAsync(
                    stream,
                    GoodMoviesJsonContext.Default.ListFavoriteFileEntry,
                    cancellationToken
                )
                .ConfigureAwait(false);
            if (values is null)
            {
                return FavoritesRead.Failure(
                    FavoritesResultStatus.Corrupted,
                    new FavoritesStoreException("The favorites document is incomplete.")
                );
            }

            List<FavoriteEntry> entries = new(values.Count);
            foreach (FavoriteFileEntry value in values)
            {
                if (value is null || value.MovieId <= 0)
                {
                    return FavoritesRead.Failure(
                        FavoritesResultStatus.Corrupted,
                        new FavoritesStoreException(
                            "The favorites document contains an invalid entry."
                        )
                    );
                }

                DateOnly releaseDate =
                    value.UsTheatricalReleaseDate != default
                        ? value.UsTheatricalReleaseDate
                        : value.ReleaseDate ?? default;
                if (releaseDate == default)
                {
                    return FavoritesRead.Failure(
                        FavoritesResultStatus.Corrupted,
                        new FavoritesStoreException(
                            "The favorites document contains an invalid release date."
                        )
                    );
                }

                entries.Add(new FavoriteEntry(value.MovieId, releaseDate));
            }

            return FavoritesRead.Success(Normalize(entries));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            return FavoritesRead.Failure(
                FavoritesResultStatus.Corrupted,
                new FavoritesStoreException(
                    "The favorites document contains invalid JSON.",
                    exception
                )
            );
        }
        catch (FileNotFoundException)
        {
            return FavoritesRead.Success(Array.Empty<FavoriteEntry>());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return FavoritesRead.Failure(
                FavoritesResultStatus.Failed,
                new FavoritesStoreException("The favorites document could not be read.", exception)
            );
        }
    }

    private async Task<Exception?> WriteRawAsync(
        IEnumerable<FavoriteEntry> entries,
        CancellationToken cancellationToken
    )
    {
        List<FavoriteFileEntry> values = entries
            .Select(static entry => new FavoriteFileEntry
            {
                MovieId = entry.MovieId,
                UsTheatricalReleaseDate = entry.UsTheatricalReleaseDate,
            })
            .ToList();

        try
        {
            await _atomicFileWriter
                .WriteAsync(
                    GetFavoritesPath(),
                    stream =>
                        JsonSerializer.SerializeAsync(
                            stream,
                            values,
                            GoodMoviesJsonContext.Default.ListFavoriteFileEntry,
                            cancellationToken
                        ),
                    cancellationToken
                )
                .ConfigureAwait(false);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new FavoritesStoreException(
                "The favorites document could not be written.",
                exception
            );
        }
    }

    private string GetFavoritesPath() => _pathProvider.GetPath(_options.FavoritesFileName);

    private static IEnumerable<FavoriteEntry> FilterVisible(
        IEnumerable<FavoriteEntry> entries,
        DateOnly today
    ) => entries.Where(entry => ReleaseWindowPolicy.IsVisible(entry, today));

    private static IEnumerable<FavoriteEntry> Normalize(IEnumerable<FavoriteEntry> entries) =>
        entries
            .Where(static entry => entry.MovieId > 0)
            .GroupBy(static entry => entry.MovieId)
            .Select(static group =>
                group.OrderBy(static entry => entry.UsTheatricalReleaseDate).First()
            )
            .OrderBy(static entry => entry.MovieId);

    private sealed class FavoritesRead
    {
        private FavoritesRead(
            FavoritesResultStatus status,
            IEnumerable<FavoriteEntry>? entries,
            Exception? error
        )
        {
            Status = status;
            Entries = (entries ?? Array.Empty<FavoriteEntry>()).ToArray();
            Error = error;
        }

        public bool Succeeded => Status == FavoritesResultStatus.Succeeded;

        public FavoritesResultStatus Status { get; }

        public FavoriteEntry[] Entries { get; }

        public Exception? Error { get; }

        public static FavoritesRead Success(IEnumerable<FavoriteEntry> entries) =>
            new(FavoritesResultStatus.Succeeded, entries, null);

        public static FavoritesRead Failure(FavoritesResultStatus status, Exception error) =>
            new(status, null, error);
    }
}
