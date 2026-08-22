namespace GoodMovies.Core;

/// <summary>
/// Describes the result of reading the on-device catalog cache.
/// </summary>
public enum CatalogCacheStatus
{
    NoCache,
    Available,
    Corrupted,
    ReadFailed,
    NoUsableCache = Corrupted,
}

/// <summary>
/// A cache read result. A failed or corrupt cache is deliberately different from
/// a valid cache containing zero movies.
/// </summary>
public sealed class CatalogCacheReadResult
{
    public CatalogCacheReadResult(
        CatalogCacheStatus status,
        IEnumerable<Movie>? movies = null,
        DateTimeOffset? lastSuccessfulRefresh = null,
        TimeSpan? age = null,
        bool isStale = false,
        Exception? error = null
    )
    {
        Status = status;
        Movies = Array.AsReadOnly((movies ?? Array.Empty<Movie>()).ToArray());
        LastSuccessfulRefresh = lastSuccessfulRefresh;
        Age = age;
        IsStale = isStale;
        Error = error;
    }

    public CatalogCacheStatus Status { get; }

    public CatalogCacheStatus State => Status;

    public IReadOnlyList<Movie> Movies { get; }

    public IReadOnlyList<Movie> Items => Movies;

    public DateTimeOffset? LastSuccessfulRefresh { get; }

    public DateTimeOffset? RefreshedAt => LastSuccessfulRefresh;

    public DateTimeOffset? SuccessfulRefreshAt => LastSuccessfulRefresh;

    public TimeSpan? Age { get; }

    public TimeSpan? CacheAge => Age;

    public bool IsStale { get; }

    public bool HasUsableCache => Status == CatalogCacheStatus.Available;

    public bool HasCache => HasUsableCache;

    public bool HasUsableData => HasUsableCache;

    public Exception? Error { get; }

    public static CatalogCacheReadResult NoCache() => new(CatalogCacheStatus.NoCache);

    public static CatalogCacheReadResult Available(
        IEnumerable<Movie> movies,
        DateTimeOffset refreshedAt,
        TimeSpan age,
        bool isStale
    ) => new(CatalogCacheStatus.Available, movies, refreshedAt, age, isStale);

    public static CatalogCacheReadResult Failure(CatalogCacheStatus status, Exception error) =>
        new(status, error: error);
}

/// <summary>
/// Describes whether a newly fetched catalog was persisted.
/// </summary>
public enum CatalogCacheWriteStatus
{
    Succeeded,
    Failed,
}

public sealed class CatalogCacheWriteResult
{
    public CatalogCacheWriteResult(
        CatalogCacheWriteStatus status,
        DateTimeOffset? refreshedAt = null,
        Exception? error = null
    )
    {
        Status = status;
        RefreshedAt = refreshedAt;
        Error = error;
    }

    public CatalogCacheWriteStatus Status { get; }

    public bool Succeeded => Status == CatalogCacheWriteStatus.Succeeded;

    public DateTimeOffset? RefreshedAt { get; }

    public DateTimeOffset? SuccessfulRefreshAt => RefreshedAt;

    public Exception? Error { get; }
}

/// <summary>
/// Describes a remote catalog refresh before it is written to the cache.
/// </summary>
public enum CatalogFetchStatus
{
    Succeeded,
    MissingConfiguration,
    Failed,
    RefreshFailed = Failed,
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
        Movies = Array.AsReadOnly((movies ?? Array.Empty<Movie>()).ToArray());
        RefreshedAt = refreshedAt;
        Error = error;
    }

    public CatalogFetchStatus Status { get; }

    public bool Succeeded => Status == CatalogFetchStatus.Succeeded;

    public IReadOnlyList<Movie> Movies { get; }

    public DateTimeOffset? RefreshedAt { get; }

    public DateTimeOffset? SuccessfulRefreshAt => RefreshedAt;

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

/// <summary>
/// The state a ViewModel can use to distinguish fresh data, stale data, and
/// failures while still displaying a previously successful cache.
/// </summary>
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
    CacheHit = FreshCache,
    CacheStale = StaleCache,
    RefreshSuccess = Refreshed,
}

public sealed class CatalogResult
{
    public CatalogResult(
        CatalogResultStatus status,
        IEnumerable<Movie>? movies = null,
        DateTimeOffset? lastSuccessfulRefresh = null,
        TimeSpan? cacheAge = null,
        bool isStale = false,
        bool usedCache = false,
        Exception? error = null,
        MovieCatalogSnapshot? snapshot = null,
        CatalogCacheStatus? cacheStatus = null
    )
    {
        Status = status;
        Movies = Array.AsReadOnly((movies ?? Array.Empty<Movie>()).ToArray());
        Snapshot = snapshot ?? MovieCatalogSnapshot.Empty;
        LastSuccessfulRefresh = lastSuccessfulRefresh;
        CacheAge = cacheAge;
        IsStale = isStale;
        UsedCache = usedCache;
        Error = error;
        CacheStatus = cacheStatus;
    }

    public CatalogResultStatus Status { get; }

    public CatalogResultStatus State => Status;

    public IReadOnlyList<Movie> Movies { get; }

    public IReadOnlyList<Movie> Items => Movies;

    public MovieCatalogSnapshot Snapshot { get; }

    public DateOnly? AsOfDate => Snapshot.AsOfDate;

    public DateTimeOffset? LastSuccessfulRefresh { get; }

    public DateTimeOffset? RefreshedAt => LastSuccessfulRefresh;

    public DateTimeOffset? SuccessfulRefreshAt => LastSuccessfulRefresh;

    public TimeSpan? CacheAge { get; }

    public TimeSpan? Age => CacheAge;

    public bool IsStale { get; }

    public bool UsedCache { get; }

    public CatalogCacheStatus? CacheStatus { get; }

    public CatalogCacheStatus? CacheState => CacheStatus;

    public bool HasUsableData =>
        Movies.Count > 0
        || UsedCache
        || Status
            is CatalogResultStatus.Refreshed
                or CatalogResultStatus.RefreshSucceededCacheWriteFailed;

    public Exception? Error { get; }

    public bool IsRefreshFailure =>
        Status
            is CatalogResultStatus.RefreshFailed
                or CatalogResultStatus.MissingConfiguration
                or CatalogResultStatus.RefreshSucceededCacheWriteFailed;
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

public interface IMovieCatalogClient : IMovieCatalogProvider { }

public interface IMovieCatalogService
{
    Task<CatalogResult> LoadAsync(CancellationToken cancellationToken = default);

    Task<CatalogResult> GetCatalogAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    );

    Task<CatalogResult> RefreshAsync(CancellationToken cancellationToken = default);
}
