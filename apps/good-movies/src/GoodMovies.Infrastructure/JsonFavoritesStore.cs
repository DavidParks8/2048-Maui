using System.Collections.Concurrent;
using System.Text.Json;
using GoodMovies.Core;

namespace GoodMovies.Infrastructure;

public sealed class FavoritesStoreException : IOException
{
    public FavoritesStoreException(string message)
        : base(message) { }

    public FavoritesStoreException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Atomic, process-safe favorites storage. Only the ID and verified US release
/// date are persisted, which keeps offline pruning independent of the catalog.
/// </summary>
public class JsonFavoritesStore : IFavoritesService, IFavoritesRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly IFileSystemPathProvider? _pathProvider;
    private readonly string? _explicitPath;
    private readonly GoodMoviesInfrastructureOptions _options;
    private readonly IAtomicFileWriter _atomicFileWriter;
    private readonly ReleaseWindowPolicy _releaseWindowPolicy;
    private readonly MovieSafetyPolicy _movieSafetyPolicy;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _gate;

    public JsonFavoritesStore(
        IFileSystemPathProvider pathProvider,
        GoodMoviesInfrastructureOptions? options = null,
        IClock? clock = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _options = options ?? new GoodMoviesInfrastructureOptions();
        _options.Validate();
        _atomicFileWriter = atomicFileWriter ?? new AtomicFileWriter();
        _releaseWindowPolicy = releaseWindowPolicy ?? ReleaseWindowPolicy.Default;
        _movieSafetyPolicy = movieSafetyPolicy ?? new MovieSafetyPolicy();
        _clock = clock ?? new SystemClock();
        _gate = Gates.GetOrAdd(GetFavoritesPath(), static _ => new SemaphoreSlim(1, 1));
    }

    public JsonFavoritesStore(
        string filePath,
        IClock? clock = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : this(
            ResolvePath(filePath, "good-movies-favorites.json"),
            new GoodMoviesInfrastructureOptions
            {
                FavoritesFileName = Path.GetFileName(
                    ResolvePath(filePath, "good-movies-favorites.json")
                ),
            },
            clock,
            atomicFileWriter,
            releaseWindowPolicy,
            movieSafetyPolicy,
            explicitPath: true
        ) { }

    public JsonFavoritesStore(
        string directoryPath,
        GoodMoviesInfrastructureOptions options,
        IClock? clock = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : this(
            ResolvePath(directoryPath, options?.FavoritesFileName ?? "good-movies-favorites.json"),
            options ?? new GoodMoviesInfrastructureOptions(),
            clock,
            atomicFileWriter,
            releaseWindowPolicy,
            movieSafetyPolicy,
            explicitPath: true
        ) { }

    private JsonFavoritesStore(
        string filePath,
        GoodMoviesInfrastructureOptions options,
        IClock? clock,
        IAtomicFileWriter? atomicFileWriter,
        ReleaseWindowPolicy? releaseWindowPolicy,
        MovieSafetyPolicy? movieSafetyPolicy,
        bool explicitPath
    )
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A favorites file path is required.", nameof(filePath));
        }

        _explicitPath = Path.GetFullPath(filePath);
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _atomicFileWriter = atomicFileWriter ?? new AtomicFileWriter();
        _releaseWindowPolicy = releaseWindowPolicy ?? ReleaseWindowPolicy.Default;
        _movieSafetyPolicy = movieSafetyPolicy ?? new MovieSafetyPolicy();
        _clock = clock ?? new SystemClock();
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

            FavoriteEntry[] visible = FilterVisible(read.Entries, today);
            if (!read.Entries.SequenceEqual(visible))
            {
                FavoritesResult writeResult = await WriteRawAsync(visible, cancellationToken)
                    .ConfigureAwait(false);
                if (!writeResult.Succeeded)
                {
                    return writeResult;
                }
            }

            return FavoritesResult.Success(visible);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<FavoritesResult> ListAsync(
        DateOnly today,
        CancellationToken cancellationToken = default
    ) => GetAsync(today, cancellationToken);

    public Task<FavoritesResult> ListAsync(CancellationToken cancellationToken = default) =>
        GetAsync(_clock.Today, cancellationToken);

    public Task<FavoritesResult> GetFavoritesAsync(
        DateOnly today,
        CancellationToken cancellationToken = default
    ) => GetAsync(today, cancellationToken);

    public Task<FavoritesResult> GetFavoritesAsync(CancellationToken cancellationToken = default) =>
        GetAsync(_clock.Today, cancellationToken);

    public Task<FavoritesResult> GetAsync(CancellationToken cancellationToken = default) =>
        GetAsync(_clock.Today, cancellationToken);

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
                return new FavoriteToggleResult(
                    FavoriteToggleStatus.Failed,
                    favorite,
                    error: read.Error
                );
            }

            List<FavoriteEntry> entries = FilterVisible(read.Entries, today).ToList();
            bool wasFavorite = entries.Any(entry => entry.MovieId == favorite.MovieId);
            FavoriteToggleStatus status;

            if (wasFavorite)
            {
                entries.RemoveAll(entry => entry.MovieId == favorite.MovieId);
                status = FavoriteToggleStatus.Removed;
            }
            else if (favorite.MovieId <= 0 || !_releaseWindowPolicy.IsVisible(favorite, today))
            {
                status = FavoriteToggleStatus.Rejected;
            }
            else
            {
                entries.Add(favorite);
                entries = Normalize(entries).ToList();
                status = FavoriteToggleStatus.Added;
            }

            if (
                status is FavoriteToggleStatus.Added or FavoriteToggleStatus.Removed
                || !read.Entries.SequenceEqual(entries)
            )
            {
                FavoritesResult writeResult = await WriteRawAsync(entries, cancellationToken)
                    .ConfigureAwait(false);
                if (!writeResult.Succeeded)
                {
                    return new FavoriteToggleResult(
                        FavoriteToggleStatus.Failed,
                        favorite,
                        entries,
                        writeResult.Error
                    );
                }
            }

            return new FavoriteToggleResult(status, favorite, entries);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<FavoriteToggleResult> ToggleAsync(
        Movie movie,
        DateOnly today,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(movie);
        FavoriteEntry? favorite = movie.CreateFavoriteEntry();
        return favorite is FavoriteEntry value
            ? ToggleAsync(value, today, cancellationToken)
            : Task.FromResult(
                new FavoriteToggleResult(
                    FavoriteToggleStatus.Rejected,
                    new FavoriteEntry(movie.Id, default)
                )
            );
    }

    public Task<FavoriteToggleResult> ToggleAsync(
        FavoriteEntry favorite,
        CancellationToken cancellationToken = default
    ) => ToggleAsync(favorite, _clock.Today, cancellationToken);

    public Task<FavoriteToggleResult> ToggleAsync(
        int movieId,
        DateOnly releaseDate,
        DateOnly today,
        CancellationToken cancellationToken = default
    ) => ToggleAsync(new FavoriteEntry(movieId, releaseDate), today, cancellationToken);

    public Task<FavoriteToggleResult> ToggleAsync(
        int movieId,
        DateOnly releaseDate,
        CancellationToken cancellationToken = default
    ) => ToggleAsync(new FavoriteEntry(movieId, releaseDate), _clock.Today, cancellationToken);

    public async Task<FavoritesResult> PruneAsync(
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

            FavoriteEntry[] visible = FilterVisible(read.Entries, today);
            if (!read.Entries.SequenceEqual(visible))
            {
                FavoritesResult writeResult = await WriteRawAsync(visible, cancellationToken)
                    .ConfigureAwait(false);
                if (!writeResult.Succeeded)
                {
                    return writeResult;
                }
            }

            return FavoritesResult.Success(visible);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<FavoritesResult> PruneAsync(CancellationToken cancellationToken = default) =>
        PruneAsync(_clock.Today, cancellationToken);

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
            foreach (Movie movie in refreshedMovies)
            {
                FavoriteEntry? entry = movie?.CreateFavoriteEntry();
                if (
                    entry is FavoriteEntry value
                    && _movieSafetyPolicy.IsSafe(movie)
                    && _releaseWindowPolicy.IsVisible(value, today)
                )
                {
                    refreshed[value.MovieId] = value;
                }
            }

            FavoriteEntry[] reconciled = Normalize(
                    FilterVisible(read.Entries, today)
                        .Where(entry => refreshed.TryGetValue(entry.MovieId, out _))
                        .Select(entry => refreshed[entry.MovieId])
                )
                .ToArray();

            if (!read.Entries.SequenceEqual(reconciled))
            {
                FavoritesResult writeResult = await WriteRawAsync(reconciled, cancellationToken)
                    .ConfigureAwait(false);
                if (!writeResult.Succeeded)
                {
                    return writeResult;
                }
            }

            return FavoritesResult.Success(reconciled);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<FavoritesResult> ReconcileAsync(
        MovieCatalogSnapshot snapshot,
        DateOnly today,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return ReconcileAsync(snapshot.Movies, today, cancellationToken);
    }

    public Task<FavoritesResult> ReconcileAsync(
        IEnumerable<Movie> refreshedMovies,
        CancellationToken cancellationToken = default
    ) => ReconcileAsync(refreshedMovies, _clock.Today, cancellationToken);

    public Task<FavoritesResult> ReconcileAgainstSnapshotAsync(
        MovieCatalogSnapshot snapshot,
        DateOnly today,
        CancellationToken cancellationToken = default
    ) => ReconcileAsync(snapshot, today, cancellationToken);

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
        catch (IOException exception)
        {
            return FavoritesRead.Failure(
                FavoritesResultStatus.Failed,
                new FavoritesStoreException("The favorites document could not be read.", exception)
            );
        }
        catch (UnauthorizedAccessException exception)
        {
            return FavoritesRead.Failure(
                FavoritesResultStatus.Failed,
                new FavoritesStoreException("The favorites document could not be read.", exception)
            );
        }
    }

    private async Task<FavoritesResult> WriteRawAsync(
        IEnumerable<FavoriteEntry> entries,
        CancellationToken cancellationToken
    )
    {
        List<FavoriteFileEntry> values = Normalize(entries)
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
            return FavoritesResult.Success(
                values.Select(static value => new FavoriteEntry(
                    value.MovieId,
                    value.UsTheatricalReleaseDate
                ))
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new FavoritesResult(
                FavoritesResultStatus.Failed,
                values.Select(static value => new FavoriteEntry(
                    value.MovieId,
                    value.UsTheatricalReleaseDate
                )),
                new FavoritesStoreException(
                    "The favorites document could not be written.",
                    exception
                )
            );
        }
    }

    private string GetFavoritesPath() =>
        _explicitPath ?? _pathProvider!.GetPath(_options.FavoritesFileName);

    private static string ResolvePath(string path, string defaultFileName) =>
        Directory.Exists(path) || string.IsNullOrEmpty(Path.GetExtension(path))
            ? Path.Combine(path, defaultFileName)
            : path;

    private FavoriteEntry[] FilterVisible(IEnumerable<FavoriteEntry> entries, DateOnly today) =>
        Normalize(entries.Where(entry => _releaseWindowPolicy.IsVisible(entry, today))).ToArray();

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
            bool succeeded,
            FavoritesResultStatus status,
            IEnumerable<FavoriteEntry>? entries,
            Exception? error
        )
        {
            Succeeded = succeeded;
            Status = status;
            Entries = (entries ?? Array.Empty<FavoriteEntry>()).ToArray();
            Error = error;
        }

        public bool Succeeded { get; }

        public FavoritesResultStatus Status { get; }

        public FavoriteEntry[] Entries { get; }

        public Exception? Error { get; }

        public static FavoritesRead Success(IEnumerable<FavoriteEntry> entries) =>
            new(true, FavoritesResultStatus.Succeeded, entries, null);

        public static FavoritesRead Failure(FavoritesResultStatus status, Exception error) =>
            new(false, status, null, error);
    }
}

public class FileFavoritesStore : JsonFavoritesStore
{
    public FileFavoritesStore(
        IFileSystemPathProvider pathProvider,
        GoodMoviesInfrastructureOptions? options = null,
        IClock? clock = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : base(
            pathProvider,
            options,
            clock,
            atomicFileWriter,
            releaseWindowPolicy,
            movieSafetyPolicy
        ) { }

    public FileFavoritesStore(
        string filePath,
        IClock? clock = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : base(filePath, clock, atomicFileWriter, releaseWindowPolicy, movieSafetyPolicy) { }
}

public class FavoritesStore : JsonFavoritesStore
{
    public FavoritesStore(
        IFileSystemPathProvider pathProvider,
        GoodMoviesInfrastructureOptions? options = null,
        IClock? clock = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : base(
            pathProvider,
            options,
            clock,
            atomicFileWriter,
            releaseWindowPolicy,
            movieSafetyPolicy
        ) { }

    public FavoritesStore(
        string filePath,
        IClock? clock = null,
        IAtomicFileWriter? atomicFileWriter = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
        : base(filePath, clock, atomicFileWriter, releaseWindowPolicy, movieSafetyPolicy) { }
}
