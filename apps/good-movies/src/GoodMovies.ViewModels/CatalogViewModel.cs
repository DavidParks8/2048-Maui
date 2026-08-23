using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// Coordinates the cache-first catalog, local search, favorites, and detail
/// navigation for the three catalog sections.
/// </summary>
public sealed partial class CatalogViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan DefaultSearchDebounce = TimeSpan.FromMilliseconds(250);

    private readonly IMovieCatalogService _catalogService;
    private readonly IFavoritesStore? _favoritesStore;
    private readonly IClock _clock;
    private readonly INavigationService? _navigationService;
    private readonly IWordLevelSpeechService? _speechService;
    private readonly IMovieTrailerLookup? _trailerLookup;
    private readonly ITrailerLauncher? _trailerLauncher;
    private readonly INetworkStatusService? _networkStatusService;
    private readonly TimeSpan _searchDebounce;
    private readonly object _operationSync = new();
    private readonly SemaphoreSlim _favoriteGate = new(1, 1);

    private Task? _initializeTask;
    private Task<CatalogResult>? _refreshTask;
    private Task<CatalogResult>? _checkForUpdatesTask;
    private long _operationVersion;
    private long _searchVersion;
    private long _favoriteVersion;
    private CancellationTokenSource? _searchDebounceCts;
    private Task _searchTask = Task.CompletedTask;
    private bool _disposed;
    private bool _hasCatalogData;
    private IReadOnlyList<Movie> _catalogMovies = Array.Empty<Movie>();
    private Dictionary<int, MovieCardViewModel> _cardsByMovieId = new();
    private Dictionary<int, FavoriteEntry> _favoriteEntries = new();

    public event EventHandler? FeedReplacementStarting;

    public event EventHandler? FeedReplacementCompleted;

    public CatalogViewModel(
        IMovieCatalogService catalogService,
        IFavoritesStore? favoritesStore = null,
        IClock? clock = null,
        INavigationService? navigationService = null,
        IWordLevelSpeechService? speechService = null,
        IMovieTrailerLookup? trailerLookup = null,
        ITrailerLauncher? trailerLauncher = null,
        TimeSpan? searchDebounce = null,
        INetworkStatusService? networkStatusService = null
    )
    {
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _favoritesStore = favoritesStore;
        _clock = clock ?? new SystemClock();
        _navigationService = navigationService;
        _speechService = speechService;
        _trailerLookup = trailerLookup;
        _trailerLauncher = trailerLauncher;
        _networkStatusService = networkStatusService;
        _searchDebounce = searchDebounce.GetValueOrDefault(DefaultSearchDebounce);
        if (_searchDebounce < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(searchDebounce));
        }

        if (_networkStatusService is not null)
        {
            _networkStatusService.NetworkStatusChanged += OnNetworkStatusChanged;
        }
    }

    [ObservableProperty]
    private ObservableCollection<MovieGroupViewModel> _movieGroups = new();

    [ObservableProperty]
    private ObservableCollection<MovieCardViewModel> _movieCards = new();

    private bool IsOffline => _networkStatusService is { IsInternetAvailable: false };

    private MovieDetailViewModel? _selectedMovieDetail;

    public MovieDetailViewModel? SelectedMovieDetail
    {
        get => _selectedMovieDetail;
        private set => SetProperty(ref _selectedMovieDetail, value);
    }

    [ObservableProperty]
    private CatalogSection _selectedSection = CatalogSection.ComingSoon;

    [ObservableProperty]
    private MovieRatingFilter _selectedRatingFilter = MovieRatingFilter.All;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private int _comingSoonCount;

    [ObservableProperty]
    private int _favoriteCount;

    [ObservableProperty]
    private int _currentCount;

    [ObservableProperty]
    private CatalogViewState _state = CatalogViewState.Idle;

    [ObservableProperty]
    private CatalogMessageKey _messageKey;

    [ObservableProperty]
    private CatalogMessageKey _warningKey;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isStale;

    [ObservableProperty]
    private bool _isWarning;

    private bool _isError;

    private bool _isMissingToken;

    private CatalogMessageKey ErrorKey { get; set; }

    private CatalogResult? _lastResult;

    internal Task SearchDebounceTask => _searchTask;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_operationSync)
        {
            if (_initializeTask is not null)
            {
                return _initializeTask;
            }

            long version = Volatile.Read(ref _operationVersion);
            Task task = InitializeCoreAsync(version, cancellationToken);
            _initializeTask = task;
            _ = ClearInitializeTaskAsync(task);
            return task;
        }
    }

    public Task<CatalogResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        lock (_operationSync)
        {
            if (_refreshTask is not null)
            {
                return _refreshTask;
            }

            long version = ++_operationVersion;
            Task<CatalogResult> task = RefreshCoreAsync(version, cancellationToken);
            _refreshTask = task;
            _ = ClearRefreshTaskAsync(task);
            return task;
        }
    }

    /// <summary>
    /// Re-applies local expiration rules without requiring a network refresh.
    /// Used when the local calendar day changes while the app stays open.
    /// </summary>
    public async Task ReapplyCurrentDatePoliciesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_disposed)
        {
            return;
        }

        ReplaceCatalog(_catalogMovies, _hasCatalogData, preserveFeedPosition: true);
        long favoriteVersion = Interlocked.Increment(ref _favoriteVersion);
        await LoadFavoritesAsync(favoriteVersion, cancellationToken);
        if (SelectedMovieDetail is { } detail && !_cardsByMovieId.ContainsKey(detail.MovieId))
        {
            detail.ReapplyCurrentDatePolicies();
            if (_navigationService is null)
            {
                CloseDetail();
            }
            else
            {
                await _navigationService.NavigateBackAsync(cancellationToken);
                if (ReferenceEquals(SelectedMovieDetail, detail))
                {
                    CloseDetail();
                }
            }
        }
        else
        {
            SelectedMovieDetail?.ReapplyCurrentDatePolicies();
        }

        UpdatePresentationState();
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task<CatalogResult> Refresh() => RefreshAsync();

    /// <summary>
    /// Revalidates the catalog using the service's cache policy. A fresh cache
    /// remains local; stale or missing data is refreshed by the service.
    /// </summary>
    public Task<CatalogResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        lock (_operationSync)
        {
            if (_refreshTask is { IsCompleted: false })
            {
                return _refreshTask;
            }

            if (_checkForUpdatesTask is { IsCompleted: false })
            {
                return _checkForUpdatesTask;
            }

            if (_initializeTask is { IsCompleted: false })
            {
                return WaitForInitializationAsync(_initializeTask, cancellationToken);
            }

            long version = ++_operationVersion;
            Task<CatalogResult> task = CheckForUpdatesCoreAsync(version, cancellationToken);
            _checkForUpdatesTask = task;
            _ = ClearCheckForUpdatesTaskAsync(task);
            return task;
        }
    }

    public async Task<CatalogResult> CheckForUpdatesAndReapplyDateAsync(
        CancellationToken cancellationToken = default
    )
    {
        await ReapplyCurrentDatePoliciesAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return await CheckForUpdatesAsync(cancellationToken);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task<CatalogResult> Retry() => RefreshAsync();

    [RelayCommand]
    public void SwitchSection(CatalogSection section)
    {
        if (section != SelectedSection)
        {
            CloseDetail();
            Query = string.Empty;
        }

        SelectedSection = section;
    }

    [RelayCommand]
    public void SelectRatingFilter(MovieRatingFilter filter)
    {
        if (!Enum.IsDefined(filter))
        {
            throw new ArgumentOutOfRangeException(nameof(filter));
        }

        SelectedRatingFilter = filter;
    }

    public Task OpenDetailAsync(
        MovieCardViewModel? card,
        CancellationToken cancellationToken = default
    )
    {
        if (card is null)
        {
            return Task.CompletedTask;
        }

        if (SelectedMovieDetail is not null)
        {
            SelectedMovieDetail.FavoriteChanged -= OnDetailFavoriteChanged;
            SelectedMovieDetail.Dispose();
        }

        MovieDetailViewModel detail = new(
            card.Movie,
            _clock,
            _favoritesStore,
            _speechService,
            _trailerLookup,
            _trailerLauncher,
            card.IsFavorite
        );
        detail.FavoriteChanged += OnDetailFavoriteChanged;
        SelectedMovieDetail = detail;

        if (_navigationService is null)
        {
            return Task.CompletedTask;
        }

        Task navigation = _navigationService.NavigateToMovieDetailAsync(
            card.MovieId,
            cancellationToken
        );
        return navigation ?? Task.CompletedTask;
    }

    public void CloseDetail()
    {
        if (SelectedMovieDetail is null)
        {
            return;
        }

        SelectedMovieDetail.FavoriteChanged -= OnDetailFavoriteChanged;
        SelectedMovieDetail.Dispose();
        SelectedMovieDetail = null;
    }

    private async Task InitializeCoreAsync(long initialVersion, CancellationToken cancellationToken)
    {
        if (!IsCurrentVersion(initialVersion))
        {
            return;
        }

        IsLoading = true;
        IsRefreshing = false;
        _isError = false;
        _isMissingToken = false;
        IsWarning = false;
        WarningKey = CatalogMessageKey.None;
        ErrorKey = CatalogMessageKey.None;
        UpdatePresentationState();

        CatalogResult loaded;
        try
        {
            loaded = await _catalogService.LoadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (IsCurrentVersion(initialVersion))
            {
                IsLoading = false;
                UpdatePresentationState();
            }

            throw;
        }
        catch (Exception exception)
        {
            loaded = new CatalogResult(CatalogResultStatus.CacheReadFailed, error: exception);
        }

        loaded ??= new CatalogResult(
            CatalogResultStatus.CacheReadFailed,
            error: new InvalidOperationException("The catalog service returned no result.")
        );

        if (!IsCurrentVersion(initialVersion))
        {
            return;
        }

        _lastResult = loaded;
        bool hasCache =
            loaded.UsedCache
            || loaded.Status is CatalogResultStatus.FreshCache or CatalogResultStatus.StaleCache;
        if (hasCache || loaded.Status is CatalogResultStatus.Refreshed)
        {
            ReplaceCatalog(loaded.Movies, hasCatalogData: true);
        }
        else
        {
            ReplaceCatalog(Array.Empty<Movie>(), hasCatalogData: false);
        }

        IsStale = loaded.IsStale || loaded.Status == CatalogResultStatus.StaleCache;
        WarningKey = IsStale ? GetRefreshWarningKey() : CatalogMessageKey.None;
        IsLoading = false;
        UpdatePresentationState();

        long favoriteVersion = Volatile.Read(ref _favoriteVersion);
        await LoadFavoritesAsync(favoriteVersion, cancellationToken);
        if (_hasCatalogData && IsCurrentVersion(initialVersion))
        {
            await ReconcileFavoritesAsync(initialVersion, cancellationToken);
        }

        bool needsRefresh =
            loaded.Status
                is CatalogResultStatus.NoCache
                    or CatalogResultStatus.StaleCache
                    or CatalogResultStatus.CacheCorrupted
                    or CatalogResultStatus.CacheReadFailed
            || (!loaded.HasUsableData && loaded.Status != CatalogResultStatus.FreshCache);
        if (needsRefresh && IsCurrentVersion(initialVersion))
        {
            IsLoading = !_hasCatalogData;
            IsRefreshing = true;
            UpdatePresentationState();
            await RefreshAsync(cancellationToken);
        }
        else if (IsCurrentVersion(initialVersion))
        {
            UpdatePresentationState();
        }
    }

    private async Task<CatalogResult> RefreshCoreAsync(
        long version,
        CancellationToken cancellationToken
    )
    {
        if (IsCurrentVersion(version))
        {
            IsRefreshing = true;
            WarningKey = CatalogMessageKey.None;
            ErrorKey = CatalogMessageKey.None;
            _isError = false;
            _isMissingToken = false;
            UpdatePresentationState();
        }

        CatalogResult result;
        try
        {
            result = await _catalogService.RefreshAsync(cancellationToken);
            result ??= CreateRefreshFailure(
                new InvalidOperationException("The catalog service returned no result.")
            );
        }
        catch (OperationCanceledException)
        {
            if (IsCurrentVersion(version))
            {
                IsLoading = false;
                IsRefreshing = false;
                UpdatePresentationState();
            }

            throw;
        }
        catch (Exception exception)
        {
            result = CreateRefreshFailure(exception);
        }

        if (!IsCurrentVersion(version))
        {
            return result;
        }

        try
        {
            _lastResult = result;

            bool successful =
                result.Status
                is CatalogResultStatus.Refreshed
                    or CatalogResultStatus.RefreshSucceededCacheWriteFailed;
            if (successful)
            {
                ReplaceCatalog(result.Movies, hasCatalogData: true, preserveFeedPosition: true);
                IsStale = false;
                _isError = false;
                _isMissingToken = false;
                IsWarning = result.Status == CatalogResultStatus.RefreshSucceededCacheWriteFailed;
                WarningKey = IsWarning ? GetRefreshWarningKey() : CatalogMessageKey.None;
                ErrorKey = CatalogMessageKey.None;

                await ReconcileFavoritesAsync(version, cancellationToken);
            }
            else
            {
                ApplyRefreshFailure(result);
            }

            if (!successful)
            {
                long favoriteVersion = Interlocked.Increment(ref _favoriteVersion);
                await LoadFavoritesAsync(favoriteVersion, cancellationToken);
            }

            return result;
        }
        finally
        {
            if (IsCurrentVersion(version))
            {
                IsLoading = false;
                IsRefreshing = false;
                UpdatePresentationState();
            }
        }
    }

    private async Task<CatalogResult> CheckForUpdatesCoreAsync(
        long version,
        CancellationToken cancellationToken
    )
    {
        if (IsCurrentVersion(version))
        {
            IsLoading = !_hasCatalogData;
            IsRefreshing = true;
            WarningKey = CatalogMessageKey.None;
            ErrorKey = CatalogMessageKey.None;
            _isError = false;
            _isMissingToken = false;
            UpdatePresentationState();
        }

        CatalogResult result;
        try
        {
            result = await _catalogService.GetCatalogAsync(
                forceRefresh: false,
                cancellationToken: cancellationToken
            );
            result ??= CreateRefreshFailure(
                new InvalidOperationException("The catalog service returned no result.")
            );
        }
        catch (OperationCanceledException)
        {
            if (IsCurrentVersion(version))
            {
                IsLoading = false;
                IsRefreshing = false;
                UpdatePresentationState();
            }

            throw;
        }
        catch (Exception exception)
        {
            result = CreateRefreshFailure(exception);
        }

        if (!IsCurrentVersion(version))
        {
            return result;
        }

        try
        {
            _lastResult = result;

            bool remoteRefreshSucceeded =
                result.Status
                is CatalogResultStatus.Refreshed
                    or CatalogResultStatus.RefreshSucceededCacheWriteFailed;
            bool cacheReadSucceeded =
                result.Status is CatalogResultStatus.FreshCache or CatalogResultStatus.StaleCache;

            if (remoteRefreshSucceeded)
            {
                ReplaceCatalog(result.Movies, hasCatalogData: true, preserveFeedPosition: true);
                IsStale = false;
                _isError = false;
                _isMissingToken = false;
                IsWarning = result.Status == CatalogResultStatus.RefreshSucceededCacheWriteFailed;
                WarningKey = IsWarning ? CatalogMessageKey.RefreshWarning : CatalogMessageKey.None;
                ErrorKey = CatalogMessageKey.None;
                await ReconcileFavoritesAsync(version, cancellationToken);
            }
            else if (cacheReadSucceeded)
            {
                ReplaceCatalog(
                    result.Movies,
                    hasCatalogData: result.Status
                        is CatalogResultStatus.FreshCache
                            or CatalogResultStatus.StaleCache
                        || result.UsedCache
                        || result.HasUsableData,
                    preserveFeedPosition: true
                );
                IsStale = result.IsStale || result.Status == CatalogResultStatus.StaleCache;
                _isError = false;
                _isMissingToken = false;
                IsWarning = IsStale;
                WarningKey = IsWarning ? GetRefreshWarningKey() : CatalogMessageKey.None;
                ErrorKey = CatalogMessageKey.None;
            }
            else
            {
                ApplyRefreshFailure(result);
            }

            if (!remoteRefreshSucceeded)
            {
                long favoriteVersion = Interlocked.Increment(ref _favoriteVersion);
                await LoadFavoritesAsync(favoriteVersion, cancellationToken);
            }

            return result;
        }
        finally
        {
            if (IsCurrentVersion(version))
            {
                IsLoading = false;
                IsRefreshing = false;
                UpdatePresentationState();
            }
        }
    }

    private async Task<CatalogResult> WaitForInitializationAsync(
        Task initializationTask,
        CancellationToken cancellationToken
    )
    {
        await initializationTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return _lastResult ?? new CatalogResult(CatalogResultStatus.NoCache);
    }

    private CatalogResult CreateRefreshFailure(Exception exception)
    {
        bool hasCache = _hasCatalogData;
        CatalogResultStatus status = IsMissingConfigurationException(exception)
            ? CatalogResultStatus.MissingConfiguration
            : CatalogResultStatus.RefreshFailed;
        return new CatalogResult(
            status,
            hasCache ? _catalogMovies : Array.Empty<Movie>(),
            isStale: hasCache,
            usedCache: hasCache,
            error: exception
        );
    }

    private void ApplyRefreshFailure(CatalogResult result)
    {
        bool hasCache = _hasCatalogData;
        // Never replace a displayed cache with a failure payload. The service
        // normally returns that cache as a fallback, but keeping our own
        // snapshot also protects against a malformed implementation.
        if (hasCache)
        {
            ReplaceCatalog(_catalogMovies, hasCatalogData: true, preserveFeedPosition: true);
        }
        else
        {
            ReplaceCatalog(Array.Empty<Movie>(), hasCatalogData: false, preserveFeedPosition: true);
        }

        IsStale = hasCache;
        _isMissingToken = result.Status == CatalogResultStatus.MissingConfiguration;
        _isError = !hasCache;
        IsWarning = hasCache;
        WarningKey = hasCache
            ? _isMissingToken
                ? CatalogMessageKey.MissingToken
                : IsOffline
                    ? CatalogMessageKey.OfflineWarning
                    : CatalogMessageKey.RefreshWarning
            : CatalogMessageKey.None;
        ErrorKey =
            _isMissingToken ? CatalogMessageKey.MissingToken
            : IsOffline ? CatalogMessageKey.OfflineError
            : CatalogMessageKey.RefreshError;
    }

    private async Task LoadFavoritesAsync(long favoriteVersion, CancellationToken cancellationToken)
    {
        if (_favoritesStore is null)
        {
            return;
        }

        FavoritesResult? result = null;
        Exception? failure = null;
        try
        {
            result = await _favoritesStore.GetAsync(_clock.Today, cancellationToken);
            if (!result.Succeeded)
            {
                failure =
                    result.Error ?? new InvalidOperationException("Favorites could not be loaded.");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        if (favoriteVersion != Volatile.Read(ref _favoriteVersion))
        {
            return;
        }

        if (result?.Succeeded == true)
        {
            ApplyFavoriteEntries(result.Entries);
        }

        if (failure is null)
        {
            ClearFavoriteFailure();
            return;
        }
        WarningKey = CatalogMessageKey.FavoritesError;
        IsWarning = true;
    }

    private async Task ReconcileFavoritesAsync(
        long operationVersion,
        CancellationToken cancellationToken
    )
    {
        if (_favoritesStore is null || !IsCurrentVersion(operationVersion))
        {
            return;
        }

        long favoriteVersion = Interlocked.Increment(ref _favoriteVersion);
        try
        {
            FavoritesResult? reconciled = await _favoritesStore.ReconcileAsync(
                _catalogMovies,
                _clock.Today,
                cancellationToken
            );
            if (
                reconciled?.Succeeded == true
                && favoriteVersion == Volatile.Read(ref _favoriteVersion)
            )
            {
                ApplyFavoriteEntries(reconciled.Entries);
                ClearFavoriteFailure();
            }
            else if (
                reconciled is not null
                && !reconciled.Succeeded
                && IsCurrentVersion(operationVersion)
            )
            {
                WarningKey = CatalogMessageKey.FavoritesError;
                IsWarning = true;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (IsCurrentVersion(operationVersion))
            {
                WarningKey = CatalogMessageKey.FavoritesError;
                IsWarning = true;
            }
        }
    }

    public async Task<FavoriteToggleResult> ToggleFavoriteAsync(
        MovieCardViewModel? card,
        CancellationToken cancellationToken = default
    )
    {
        if (card is null)
        {
            return new FavoriteToggleResult(
                FavoriteToggleStatus.Rejected,
                error: new ArgumentNullException(nameof(card))
            );
        }

        FavoriteEntry? optionalEntry = card.FavoriteEntry;
        if (optionalEntry is not FavoriteEntry entry)
        {
            FavoriteToggleResult rejected = new(
                FavoriteToggleStatus.Rejected,
                error: new InvalidOperationException("The movie has no eligible release date.")
            );
            SetFavoriteFailure(CatalogMessageKey.FavoriteNotAllowed);
            return rejected;
        }

        if (_favoritesStore is null)
        {
            FavoriteToggleResult failed = new(
                FavoriteToggleStatus.Failed,
                error: new InvalidOperationException("Favorites are not configured.")
            );
            SetFavoriteFailure(CatalogMessageKey.FavoriteSaveFailed);
            return failed;
        }

        await _favoriteGate.WaitAsync(cancellationToken);
        try
        {
            FavoriteToggleResult? result;
            try
            {
                result = await _favoritesStore.ToggleAsync(entry, _clock.Today, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                result = new FavoriteToggleResult(FavoriteToggleStatus.Failed, error: exception);
            }

            if (result is null)
            {
                result = new FavoriteToggleResult(
                    FavoriteToggleStatus.Failed,
                    error: new InvalidOperationException("The favorites store returned no result.")
                );
            }

            if (result.Status is FavoriteToggleStatus.Added or FavoriteToggleStatus.Removed)
            {
                bool isFavorite = result.Status == FavoriteToggleStatus.Added;
                SetFavoriteForMovie(entry, isFavorite);
                ClearFavoriteFailure();
            }
            else
            {
                CatalogMessageKey failureKey =
                    result.Status == FavoriteToggleStatus.Rejected
                        ? CatalogMessageKey.FavoriteNotAllowed
                        : CatalogMessageKey.FavoriteSaveFailed;
                SetFavoriteFailure(failureKey);
            }

            return result;
        }
        finally
        {
            _favoriteGate.Release();
        }
    }

    private void SetFavoriteForMovie(FavoriteEntry entry, bool isFavorite)
    {
        Interlocked.Increment(ref _favoriteVersion);
        if (isFavorite)
        {
            _favoriteEntries[entry.MovieId] = entry;
        }
        else
        {
            _favoriteEntries.Remove(entry.MovieId);
        }

        if (_cardsByMovieId.TryGetValue(entry.MovieId, out MovieCardViewModel? card))
        {
            card.SetFavorite(isFavorite);
        }

        if (SelectedMovieDetail?.MovieId == entry.MovieId)
        {
            SelectedMovieDetail.SetFavoriteState(isFavorite);
        }

        BuildCurrentView();
    }

    private void SetFavoriteFailure(CatalogMessageKey key)
    {
        WarningKey = key;
        IsWarning = true;
        UpdatePresentationState();
    }

    private void ClearFavoriteFailure()
    {
        if (
            WarningKey
            is CatalogMessageKey.FavoriteSaveFailed
                or CatalogMessageKey.FavoriteNotAllowed
                or CatalogMessageKey.FavoritesError
        )
        {
            WarningKey = CatalogMessageKey.None;
            IsWarning = false;
            UpdatePresentationState();
        }
    }

    private void ApplyFavoriteEntries(IEnumerable<FavoriteEntry>? entries)
    {
        Dictionary<int, FavoriteEntry> next = new();
        foreach (FavoriteEntry entry in entries ?? Array.Empty<FavoriteEntry>())
        {
            if (ReleaseWindowPolicy.IsVisible(entry, _clock.Today))
            {
                next[entry.MovieId] = entry;
            }
        }

        _favoriteEntries = next;
        foreach (MovieCardViewModel card in _cardsByMovieId.Values)
        {
            card.SetFavorite(_favoriteEntries.ContainsKey(card.MovieId));
        }

        if (SelectedMovieDetail is not null)
        {
            SelectedMovieDetail.SetFavoriteState(
                _favoriteEntries.ContainsKey(SelectedMovieDetail.MovieId)
            );
        }

        BuildCurrentView();
        Interlocked.Increment(ref _favoriteVersion);
    }

    private void OnDetailFavoriteChanged(object? sender, FavoriteChangedEventArgs args)
    {
        SetFavoriteForMovie(args.Entry, args.IsFavorite);
    }

    private void ReplaceCatalog(
        IEnumerable<Movie>? movies,
        bool hasCatalogData,
        bool preserveFeedPosition = false
    )
    {
        IReadOnlyList<Movie> safeMovies = new MovieCatalogSnapshot(
            movies ?? Array.Empty<Movie>(),
            _clock.Today
        ).Movies;
        Dictionary<int, MovieCardViewModel> nextCards = new();
        foreach (Movie movie in safeMovies)
        {
            MovieCardViewModel card = new(
                movie,
                _clock,
                _favoriteEntries.ContainsKey(movie.Id),
                ToggleFavoriteAsync
            );
            nextCards[movie.Id] = card;
        }

        _catalogMovies = safeMovies;
        _cardsByMovieId = nextCards;
        _hasCatalogData = hasCatalogData;
        if (preserveFeedPosition)
        {
            FeedReplacementStarting?.Invoke(this, EventArgs.Empty);
        }

        try
        {
            BuildCurrentView();
        }
        finally
        {
            if (preserveFeedPosition)
            {
                FeedReplacementCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void BuildCurrentView()
    {
        string normalizedQuery = NormalizeQuery(Query);
        IEnumerable<MovieCardViewModel> selected = SelectedSection switch
        {
            CatalogSection.MyFavorites => _cardsByMovieId.Values.Where(card =>
                _favoriteEntries.ContainsKey(card.MovieId)
            ),
            CatalogSection.FindAMovie when normalizedQuery.Length > 0 =>
                _cardsByMovieId.Values.Where(card => Matches(card, normalizedQuery)),
            CatalogSection.FindAMovie => Array.Empty<MovieCardViewModel>(),
            _ => _cardsByMovieId.Values.Where(MatchesRatingFilter),
        };

        MovieCardViewModel[] selectedCards = selected
            .OrderBy(static card => card.ReleaseDate ?? DateOnly.MaxValue)
            .ThenBy(static card => card.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static card => card.Title, StringComparer.Ordinal)
            .ThenBy(static card => card.MovieId)
            .ToArray();

        // Replacing the collections reloads the grouped CollectionView, which
        // resets the scroll position. Skip it when the visible cards are
        // unchanged so that toggling a favorite leaves the feed where it is.
        if (!IsCurrentView(selectedCards))
        {
            MovieGroups = new ObservableCollection<MovieGroupViewModel>(
                MovieGroupViewModel.CreateGroups(selectedCards)
            );
            MovieCards = new ObservableCollection<MovieCardViewModel>(selectedCards);
        }

        UpdateCounts();
        UpdatePresentationState();
    }

    /// <summary>
    /// True when the feed already shows exactly these card instances in this
    /// order, so the grouped collections can be left alone.
    /// </summary>
    private bool IsCurrentView(MovieCardViewModel[] selectedCards)
    {
        if (MovieCards.Count != selectedCards.Length)
        {
            return false;
        }

        for (int index = 0; index < selectedCards.Length; index++)
        {
            if (!ReferenceEquals(MovieCards[index], selectedCards[index]))
            {
                return false;
            }
        }

        // Groups are a pure function of the ordered cards, but an earlier build
        // may have been skipped before the groups were ever populated.
        return MovieGroups.Sum(static group => group.Count) == selectedCards.Length;
    }

    private void UpdateCounts()
    {
        ComingSoonCount = _catalogMovies.Count;
        FavoriteCount = _catalogMovies.Count(movie => _favoriteEntries.ContainsKey(movie.Id));
        CurrentCount = MovieCards.Count;
    }

    private void UpdatePresentationState()
    {
        CatalogViewState nextState;
        CatalogMessageKey nextMessage;

        if (IsLoading && !_hasCatalogData)
        {
            nextState = CatalogViewState.Loading;
            nextMessage = CatalogMessageKey.Loading;
        }
        else if (_isError)
        {
            nextState = _isMissingToken ? CatalogViewState.MissingToken : CatalogViewState.Error;
            nextMessage = ErrorKey;
        }
        else if (IsRefreshing)
        {
            nextState = CatalogViewState.Refreshing;
            nextMessage = CatalogMessageKey.Loading;
        }
        else if (_isMissingToken && !_hasCatalogData)
        {
            nextState = CatalogViewState.MissingToken;
            nextMessage =
                WarningKey == CatalogMessageKey.None ? CatalogMessageKey.MissingToken : WarningKey;
        }
        else if (SelectedSection == CatalogSection.FindAMovie && string.IsNullOrWhiteSpace(Query))
        {
            nextState = CatalogViewState.SearchPrompt;
            nextMessage = CatalogMessageKey.SearchPrompt;
        }
        else if (SelectedSection == CatalogSection.FindAMovie && MovieCards.Count == 0)
        {
            nextState = CatalogViewState.NoResults;
            nextMessage = CatalogMessageKey.NoSearchResults;
        }
        else if (MovieCards.Count == 0)
        {
            nextState = CatalogViewState.Empty;
            nextMessage =
                SelectedSection == CatalogSection.MyFavorites
                    ? CatalogMessageKey.NoFavorites
                    : CatalogMessageKey.NoMovies;
        }
        else if (IsWarning)
        {
            nextState = CatalogViewState.Warning;
            nextMessage = WarningKey;
        }
        else if (IsStale)
        {
            nextState = CatalogViewState.Stale;
            nextMessage = CatalogMessageKey.RefreshWarning;
        }
        else
        {
            nextState = CatalogViewState.Ready;
            nextMessage = CatalogMessageKey.None;
        }

        State = nextState;
        MessageKey = nextMessage;
    }

    private void ScheduleSearch()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        CancellationTokenSource cts = new();
        _searchDebounceCts = cts;
        long version = Interlocked.Increment(ref _searchVersion);
        string query = Query;
        _searchTask = DebounceSearchAsync(query, version, cts.Token);
    }

    private async Task DebounceSearchAsync(
        string query,
        long version,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Task.Delay(_searchDebounce, cancellationToken);
            if (
                cancellationToken.IsCancellationRequested
                || version != Volatile.Read(ref _searchVersion)
                || SelectedSection != CatalogSection.FindAMovie
                || !string.Equals(query, Query, StringComparison.Ordinal)
            )
            {
                return;
            }

            BuildCurrentView();
        }
        catch (OperationCanceledException)
        {
            // A newer query owns the next debounce task.
        }
    }

    private void CancelSearch()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;
    }

    private async Task ClearInitializeTaskAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // The caller observes initialization failures.
        }
        finally
        {
            lock (_operationSync)
            {
                if (ReferenceEquals(_initializeTask, task))
                {
                    _initializeTask = null;
                }
            }
        }
    }

    private async Task ClearRefreshTaskAsync(Task<CatalogResult> task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // The caller observes cancellation; this continuation only clears
            // the duplicate-operation guard.
        }
        finally
        {
            lock (_operationSync)
            {
                if (ReferenceEquals(_refreshTask, task))
                {
                    _refreshTask = null;
                }
            }
        }
    }

    private async Task ClearCheckForUpdatesTaskAsync(Task<CatalogResult> task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // The caller observes operation failures; this continuation only
            // clears the duplicate-operation guard.
        }
        finally
        {
            lock (_operationSync)
            {
                if (ReferenceEquals(_checkForUpdatesTask, task))
                {
                    _checkForUpdatesTask = null;
                }
            }
        }
    }

    private bool IsCurrentVersion(long version) => version == Volatile.Read(ref _operationVersion);

    private static bool Matches(MovieCardViewModel card, string normalizedQuery) =>
        NormalizeQuery(card.Title).Contains(normalizedQuery, StringComparison.Ordinal)
        || NormalizeQuery(card.Kind).Contains(normalizedQuery, StringComparison.Ordinal)
        || card.Movie.Genres.Any(genre =>
            NormalizeQuery(genre.Name).Contains(normalizedQuery, StringComparison.Ordinal)
        );

    private bool MatchesRatingFilter(MovieCardViewModel card) =>
        SelectedRatingFilter switch
        {
            MovieRatingFilter.All => true,
            MovieRatingFilter.G => card.Movie.Certification?.IsG == true,
            MovieRatingFilter.PG => card.Movie.Certification?.IsPg == true,
            MovieRatingFilter.RatingSoon => card.Movie.IsNotYetRated,
            _ => false,
        };

    internal static string NormalizeQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        return string.Join(' ', query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private static bool IsMissingConfigurationException(Exception exception) =>
        exception.GetType().Name.Contains("Configuration", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("token", StringComparison.OrdinalIgnoreCase);

    private CatalogMessageKey GetRefreshWarningKey() =>
        IsOffline ? CatalogMessageKey.OfflineWarning : CatalogMessageKey.RefreshWarning;

    private void OnNetworkStatusChanged(object? sender, EventArgs e)
    {
        if (IsOffline && _hasCatalogData)
        {
            IsWarning = true;
            IsStale = true;
            WarningKey = CatalogMessageKey.OfflineWarning;
        }
        else if (!IsOffline && WarningKey == CatalogMessageKey.OfflineWarning)
        {
            WarningKey = IsStale ? CatalogMessageKey.RefreshWarning : CatalogMessageKey.None;
            IsWarning = IsStale;
        }

        UpdatePresentationState();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelSearch();
        CloseDetail();
        if (_networkStatusService is not null)
        {
            _networkStatusService.NetworkStatusChanged -= OnNetworkStatusChanged;
        }

        GC.SuppressFinalize(this);
    }

    partial void OnSelectedSectionChanged(CatalogSection value)
    {
        CancelSearch();
        if (value != CatalogSection.FindAMovie && !string.IsNullOrWhiteSpace(Query))
        {
            Query = string.Empty;
        }

        if (value == CatalogSection.FindAMovie && !string.IsNullOrWhiteSpace(Query))
        {
            ScheduleSearch();
        }
        else
        {
            BuildCurrentView();
        }
    }

    partial void OnSelectedRatingFilterChanged(MovieRatingFilter value)
    {
        if (SelectedSection == CatalogSection.ComingSoon)
        {
            BuildCurrentView();
        }
    }

    partial void OnQueryChanged(string value)
    {
        if (SelectedSection == CatalogSection.FindAMovie)
        {
            ScheduleSearch();
        }
    }
}
