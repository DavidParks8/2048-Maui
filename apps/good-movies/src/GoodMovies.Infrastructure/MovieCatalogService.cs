using GoodMovies.Core;

namespace GoodMovies.Infrastructure;

/// <summary>
/// Coordinates cache-first loading, remote revalidation, and favorite
/// reconciliation. A failed refresh never replaces a good cache.
/// </summary>
public class MovieCatalogService : IMovieCatalogService
{
    private readonly IMovieCatalogProvider _provider;
    private readonly IMovieCatalogCache _cache;
    private readonly IClock _clock;
    private readonly IGoodMoviesTimeProvider _timeProvider;
    private readonly ReleaseWindowPolicy _releaseWindowPolicy;
    private readonly MovieSafetyPolicy _movieSafetyPolicy;
    private readonly IFavoritesStore? _favoritesStore;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public MovieCatalogService(
        IMovieCatalogProvider provider,
        IMovieCatalogCache cache,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        IFavoritesStore? favoritesStore = null
    )
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _clock = clock ?? new SystemClock();
        _timeProvider = timeProvider ?? new SystemGoodMoviesTimeProvider();
        _releaseWindowPolicy = releaseWindowPolicy ?? ReleaseWindowPolicy.Default;
        _movieSafetyPolicy = movieSafetyPolicy ?? new MovieSafetyPolicy();
        _favoritesStore = favoritesStore;
    }

    public async Task<CatalogResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        CatalogCacheReadResult cached = await _cache
            .ReadAsync(_clock.Today, cancellationToken)
            .ConfigureAwait(false);
        return FromCache(cached);
    }

    public async Task<CatalogResult> GetCatalogAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        return await GetCatalogSerializedAsync(forceRefresh, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<CatalogResult> GetAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    ) => GetCatalogAsync(forceRefresh, cancellationToken);

    public Task<CatalogResult> RefreshAsync(CancellationToken cancellationToken = default) =>
        GetCatalogSerializedAsync(forceRefresh: true, cancellationToken);

    private async Task<CatalogResult> GetCatalogSerializedAsync(
        bool forceRefresh,
        CancellationToken cancellationToken
    )
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Read after acquiring the gate so a queued refresh never falls back
            // to a snapshot that another refresh has already replaced.
            CatalogCacheReadResult cached = await _cache
                .ReadAsync(_clock.Today, cancellationToken)
                .ConfigureAwait(false);

            if (!forceRefresh && cached.Status == CatalogCacheStatus.Available && !cached.IsStale)
            {
                return FromCache(cached);
            }

            return await RefreshWithFallbackAsync(cached, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task<CatalogResult> RefreshWithFallbackAsync(
        CatalogCacheReadResult cached,
        CancellationToken cancellationToken
    )
    {
        CatalogFetchResult fetched;
        try
        {
            fetched = await _provider
                .FetchAsync(_clock.Today, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (GoodMoviesConfigurationException exception)
        {
            fetched = CatalogFetchResult.MissingConfiguration(exception);
        }
        catch (Exception exception)
        {
            fetched = CatalogFetchResult.Failure(exception);
        }

        if (!fetched.Succeeded)
        {
            CatalogResultStatus status =
                fetched.Status == CatalogFetchStatus.MissingConfiguration
                    ? CatalogResultStatus.MissingConfiguration
                    : CatalogResultStatus.RefreshFailed;
            return new CatalogResult(
                status,
                cached.HasUsableCache ? cached.Movies : Array.Empty<Movie>(),
                cached.LastSuccessfulRefresh,
                cached.Age,
                cached.IsStale,
                cached.HasUsableCache,
                fetched.Error ?? cached.Error,
                cached.HasUsableCache
                    ? MovieCatalogSnapshot.Create(
                        cached.Movies,
                        _clock.Today,
                        _releaseWindowPolicy,
                        _movieSafetyPolicy
                    )
                    : null,
                cached.Status
            );
        }

        MovieCatalogSnapshot snapshot = MovieCatalogSnapshot.Create(
            fetched.Movies,
            _clock.Today,
            _releaseWindowPolicy,
            _movieSafetyPolicy
        );
        DateTimeOffset refreshedAt =
            fetched.RefreshedAt is DateTimeOffset value && value != default
                ? value
                : _timeProvider.UtcNow;
        CatalogCacheWriteResult written;
        try
        {
            written = await _cache
                .WriteAsync(snapshot, refreshedAt, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            written = new CatalogCacheWriteResult(
                CatalogCacheWriteStatus.Failed,
                error: new CatalogCacheException(
                    "The catalog cache could not be written.",
                    exception
                )
            );
        }

        if (_favoritesStore is not null)
        {
            // Reconciliation is intentionally tied to a successful provider
            // response, not to a cache write or a transient API failure.
            await _favoritesStore
                .ReconcileAsync(snapshot.Movies, _clock.Today, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!written.Succeeded)
        {
            return new CatalogResult(
                CatalogResultStatus.RefreshSucceededCacheWriteFailed,
                snapshot.Movies,
                refreshedAt,
                TimeSpan.Zero,
                isStale: false,
                usedCache: false,
                error: written.Error,
                snapshot: snapshot,
                cacheStatus: CatalogCacheStatus.Available
            );
        }

        return new CatalogResult(
            CatalogResultStatus.Refreshed,
            snapshot.Movies,
            refreshedAt,
            TimeSpan.Zero,
            isStale: false,
            usedCache: false,
            snapshot: snapshot,
            cacheStatus: CatalogCacheStatus.Available
        );
    }

    private CatalogResult FromCache(CatalogCacheReadResult cached)
    {
        CatalogResultStatus status = cached.Status switch
        {
            CatalogCacheStatus.NoCache => CatalogResultStatus.NoCache,
            CatalogCacheStatus.Corrupted => CatalogResultStatus.CacheCorrupted,
            CatalogCacheStatus.ReadFailed => CatalogResultStatus.CacheReadFailed,
            CatalogCacheStatus.Available when cached.IsStale => CatalogResultStatus.StaleCache,
            CatalogCacheStatus.Available => CatalogResultStatus.FreshCache,
            _ => CatalogResultStatus.CacheReadFailed,
        };

        return new CatalogResult(
            status,
            cached.HasUsableCache ? cached.Movies : Array.Empty<Movie>(),
            cached.LastSuccessfulRefresh,
            cached.Age,
            cached.IsStale,
            cached.HasUsableCache,
            cached.Error,
            cached.HasUsableCache
                ? MovieCatalogSnapshot.Create(
                    cached.Movies,
                    _clock.Today,
                    _releaseWindowPolicy,
                    _movieSafetyPolicy
                )
                : null,
            cached.Status
        );
    }
}

public class GoodMoviesCatalogService : MovieCatalogService
{
    public GoodMoviesCatalogService(
        IMovieCatalogProvider provider,
        IMovieCatalogCache cache,
        IClock? clock = null,
        IGoodMoviesTimeProvider? timeProvider = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        IFavoritesStore? favoritesStore = null
    )
        : base(
            provider,
            cache,
            clock,
            timeProvider,
            releaseWindowPolicy,
            movieSafetyPolicy,
            favoritesStore
        ) { }
}
