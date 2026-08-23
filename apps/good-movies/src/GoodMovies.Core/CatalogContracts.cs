namespace GoodMovies.Core;

public enum CatalogCacheStatus
{
    NoCache,
    Available,
    Corrupted,
    ReadFailed,
}

public sealed class CatalogCacheReadResult
{
    public CatalogCacheReadResult(
        CatalogCacheStatus status,
        IEnumerable<Movie>? movies = null,
        bool isStale = false,
        Exception? error = null
    )
    {
        Status = status;
        Movies = CollectionSnapshot.Create(movies);
        IsStale = isStale;
        Error = error;
    }

    public CatalogCacheStatus Status { get; }

    public IReadOnlyList<Movie> Movies { get; }

    public bool IsStale { get; }

    public bool HasUsableCache => Status == CatalogCacheStatus.Available;

    public Exception? Error { get; }

    public static CatalogCacheReadResult NoCache() => new(CatalogCacheStatus.NoCache);

    public static CatalogCacheReadResult Available(IEnumerable<Movie> movies, bool isStale) =>
        new(CatalogCacheStatus.Available, movies, isStale);

    public static CatalogCacheReadResult Failure(CatalogCacheStatus status, Exception error) =>
        new(status, error: error);
}

public enum CatalogCacheWriteStatus
{
    Succeeded,
    Failed,
}

public sealed class CatalogCacheWriteResult
{
    public CatalogCacheWriteResult(CatalogCacheWriteStatus status, Exception? error = null)
    {
        Status = status;
        Error = error;
    }

    public CatalogCacheWriteStatus Status { get; }

    public bool Succeeded => Status == CatalogCacheWriteStatus.Succeeded;

    public Exception? Error { get; }
}

public enum CatalogFetchStatus
{
    Succeeded,
    MissingConfiguration,
    Failed,
}

public sealed class CatalogFetchResult
{
    public CatalogFetchResult(
        CatalogFetchStatus status,
        IEnumerable<Movie>? movies = null,
        DateTimeOffset? refreshedAt = null,
        Exception? error = null
    )
    {
        Status = status;
        Movies = CollectionSnapshot.Create(movies);
        RefreshedAt = refreshedAt;
        Error = error;
    }

    public CatalogFetchStatus Status { get; }

    public bool Succeeded => Status == CatalogFetchStatus.Succeeded;

    public IReadOnlyList<Movie> Movies { get; }

    public DateTimeOffset? RefreshedAt { get; }

    public Exception? Error { get; }

    public static CatalogFetchResult Success(
        IEnumerable<Movie> movies,
        DateTimeOffset refreshedAt
    ) => new(CatalogFetchStatus.Succeeded, movies, refreshedAt);

    public static CatalogFetchResult MissingConfiguration(Exception error) =>
        new(CatalogFetchStatus.MissingConfiguration, error: error);

    public static CatalogFetchResult Failure(Exception error) =>
        new(CatalogFetchStatus.Failed, error: error);
}

public enum CatalogResultStatus
{
    NoCache,
    FreshCache,
    StaleCache,
    Refreshed,
    CacheCorrupted,
    CacheReadFailed,
    MissingConfiguration,
    RefreshFailed,
    RefreshSucceededCacheWriteFailed,
}

public sealed class CatalogResult
{
    public CatalogResult(
        CatalogResultStatus status,
        IEnumerable<Movie>? movies = null,
        bool isStale = false,
        bool usedCache = false,
        Exception? error = null
    )
    {
        Status = status;
        Movies = CollectionSnapshot.Create(movies);
        IsStale = isStale;
        UsedCache = usedCache;
        Error = error;
    }

    public CatalogResultStatus Status { get; }

    public IReadOnlyList<Movie> Movies { get; }

    public bool IsStale { get; }

    public bool UsedCache { get; }

    public bool HasUsableData =>
        Movies.Count > 0
        || UsedCache
        || Status
            is CatalogResultStatus.Refreshed
                or CatalogResultStatus.RefreshSucceededCacheWriteFailed;

    public Exception? Error { get; }
}

public interface IMovieCatalogCache
{
    Task<CatalogCacheReadResult> ReadAsync(
        DateOnly today,
        CancellationToken cancellationToken = default
    );

    Task<CatalogCacheWriteResult> WriteAsync(
        MovieCatalogSnapshot snapshot,
        DateTimeOffset refreshedAt,
        CancellationToken cancellationToken = default
    );
}

public interface IMovieCatalogProvider
{
    Task<CatalogFetchResult> FetchAsync(
        DateOnly today,
        CancellationToken cancellationToken = default
    );
}

public interface IMovieCatalogService
{
    Task<CatalogResult> LoadAsync(CancellationToken cancellationToken = default);

    Task<CatalogResult> GetCatalogAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    );

    Task<CatalogResult> RefreshAsync(CancellationToken cancellationToken = default);
}
