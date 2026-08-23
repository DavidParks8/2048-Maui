using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodMovies.Core;
using Microsoft.Extensions.DependencyInjection;

namespace GoodMovies.ViewModels;

/// <summary>
/// Coordinates the cache-first catalog, local search, favorites, and detail
/// navigation for the Design E sections.
/// </summary>
public partial class CatalogViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan DefaultSearchDebounce = TimeSpan.FromMilliseconds(250);

    private readonly IMovieCatalogService _catalogService;
    private readonly IFavoritesStore? _favoritesStore;
    private readonly IClock _clock;
    private readonly ReleaseWindowPolicy _releaseWindowPolicy;
    private readonly MovieSafetyPolicy _movieSafetyPolicy;
    private readonly INavigationService? _navigationService;
    private readonly IWordLevelSpeechService? _speechService;
    private readonly IMovieTrailerLookup? _trailerLookup;
    private readonly IExternalTrailerLauncher? _trailerLauncher;
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

    [ActivatorUtilitiesConstructor]
    public CatalogViewModel(
        IMovieCatalogService catalogService,
        IFavoritesStore? favoritesStore = null,
        IClock? clock = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        INavigationService? navigationService = null,
        IWordLevelSpeechService? speechService = null,
        IMovieTrailerLookup? trailerLookup = null,
        IExternalTrailerLauncher? trailerLauncher = null,
        TimeSpan? searchDebounce = null,
        INetworkStatusService? networkStatusService = null
    )
    {
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _favoritesStore = favoritesStore;
        _clock = clock ?? new SystemClock();
        _releaseWindowPolicy = releaseWindowPolicy ?? GoodMovies.Core.ReleaseWindowPolicy.Default;
        _movieSafetyPolicy = movieSafetyPolicy ?? new MovieSafetyPolicy();
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

    public CatalogViewModel(
        IMovieCatalogService catalogService,
        IClock clock,
        IFavoritesStore? favoritesStore = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        INavigationService? navigationService = null,
        IWordLevelSpeechService? speechService = null,
        IMovieTrailerLookup? trailerLookup = null,
        IExternalTrailerLauncher? trailerLauncher = null,
        TimeSpan? searchDebounce = null
    )
        : this(
            catalogService,
            favoritesStore,
            clock,
            releaseWindowPolicy,
            movieSafetyPolicy,
            navigationService,
            speechService,
            trailerLookup,
            trailerLauncher,
            searchDebounce
        ) { }

    public CatalogViewModel(
        IMovieCatalogService catalogService,
        IFavoritesStore? favoritesStore,
        IClock clock,
        INavigationService? navigation,
        IWordLevelSpeechService? speechService = null,
        IMovieTrailerLookup? trailerLookup = null,
        IExternalTrailerLauncher? trailerLauncher = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        TimeSpan? searchDebounce = null
    )
        : this(
            catalogService,
            favoritesStore,
            clock,
            releaseWindowPolicy,
            movieSafetyPolicy,
            navigation,
            speechService,
            trailerLookup,
            trailerLauncher,
            searchDebounce
        ) { }

    public CatalogViewModel(
        IMovieCatalogService catalogService,
        IClock clock,
        IFavoritesStore? favoritesStore,
        INavigationService? navigation,
        IWordLevelSpeechService? speechService = null,
        IMovieTrailerLookup? trailerLookup = null,
        IExternalTrailerLauncher? trailerLauncher = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        TimeSpan? searchDebounce = null
    )
        : this(
            catalogService,
            favoritesStore,
            clock,
            releaseWindowPolicy,
            movieSafetyPolicy,
            navigation,
            speechService,
            trailerLookup,
            trailerLauncher,
            searchDebounce
        ) { }

    [ObservableProperty]
    private ObservableCollection<MovieGroupViewModel> _movieGroups = new();

    public ObservableCollection<MovieGroupViewModel> GroupsCollection => MovieGroups;

    public IReadOnlyList<MovieGroupViewModel> Groups => MovieGroups;

    public IReadOnlyList<MovieGroupViewModel> GroupViewModels => MovieGroups;

    [ObservableProperty]
    private ObservableCollection<MovieCardViewModel> _movieCards = new();

    public ObservableCollection<MovieCardViewModel> CardsCollection => MovieCards;

    public IReadOnlyList<MovieCardViewModel> Cards => MovieCards;

    public IReadOnlyList<MovieCardViewModel> CurrentMovies => MovieCards;

    public IReadOnlyList<MovieCardViewModel> CurrentCards => MovieCards;

    public IReadOnlyList<MovieCardViewModel> VisibleMovies => MovieCards;

    public IReadOnlyList<MovieCardViewModel> Movies => MovieCards;

    public IReadOnlyList<MovieCardViewModel> FavoriteMovies =>
        _cardsByMovieId
            .Values.Where(card => _favoriteEntries.ContainsKey(card.MovieId))
            .OrderBy(static card => card.ReleaseDate ?? DateOnly.MaxValue)
            .ThenBy(static card => card.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static card => card.Title, StringComparer.Ordinal)
            .ThenBy(static card => card.MovieId)
            .ToArray();

    public IReadOnlyList<Movie> CatalogMovies => _catalogMovies;

    public bool HasCatalogData => _hasCatalogData;

    public bool HasData => HasCatalogData;

    public bool IsOffline => _networkStatusService is { IsInternetAvailable: false };

    public bool IsBusy => IsLoading || IsRefreshing;

    public bool Loading => IsLoading;

    public bool Refreshing => IsRefreshing;

    public bool Stale => IsStale;

    public CatalogSection CurrentSection => SelectedSection;

    public CatalogSection Section => SelectedSection;

    public CatalogResultStatus ResultStatus => Status;

    public CatalogResultStatus CatalogStatus => Status;

    public CatalogViewState ViewState => State;

    public CatalogViewState PresentationState => State;

    public CatalogMessageKey CurrentMessageKey => MessageKey;

    public CatalogMessageKey WarningMessageKey => WarningKey;

    public CatalogMessageKey ErrorMessageKey => ErrorKey;

    public CatalogMessageKey EmptyStateKey =>
        SelectedSection == CatalogSection.FindAMovie
            ? string.IsNullOrWhiteSpace(Query)
                ? CatalogMessageKey.SearchPrompt
                : CatalogMessageKey.NoSearchResults
            : SelectedSection == CatalogSection.MyFavorites
                ? CatalogMessageKey.NoFavorites
                : CatalogMessageKey.NoMovies;

    private MovieDetailViewModel? _selectedMovieDetail;

    public MovieDetailViewModel? SelectedMovieDetail
    {
        get => _selectedMovieDetail;
        private set
        {
            if (SetProperty(ref _selectedMovieDetail, value))
            {
                OnPropertyChanged(nameof(SelectedMovie));
                OnPropertyChanged(nameof(Detail));
            }
        }
    }

    public MovieDetailViewModel? Detail => SelectedMovieDetail;

    public Movie? SelectedMovie => SelectedMovieDetail?.Movie;

    [ObservableProperty]
    private CatalogSection _selectedSection = CatalogSection.ComingSoon;

    [ObservableProperty]
    private MovieRatingFilter _selectedRatingFilter = MovieRatingFilter.All;

    [ObservableProperty]
    private string _query = string.Empty;

    public string NormalizedQuery => NormalizeQuery(Query);

    public string SearchQuery
    {
        get => Query;
        set => Query = value;
    }

    [ObservableProperty]
    private int _comingSoonCount;

    [ObservableProperty]
    private int _favoriteCount;

    public int FavoritesCount => FavoriteCount;

    public int FavoriteMoviesCount => FavoriteCount;

    [ObservableProperty]
    private int _findMovieCount;

    public int SearchResultCount => FindMovieCount;

    [ObservableProperty]
    private int _currentCount;

    public int MovieCount => CurrentCount;

    [ObservableProperty]
    private CatalogResultStatus _status = CatalogResultStatus.NoCache;

    [ObservableProperty]
    private CatalogViewState _state = CatalogViewState.Idle;

    [ObservableProperty]
    private CatalogMessageKey _messageKey;

    [ObservableProperty]
    private CatalogMessageKey _warningKey;

    [ObservableProperty]
    private CatalogMessageKey _errorKey;

    [ObservableProperty]
    private CatalogMessageKey _favoriteMessageKey;

    public CatalogMessageKey FavoriteErrorKey => FavoriteMessageKey;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isStale;

    [ObservableProperty]
    private bool _isWarning;

    public bool HasWarning => IsWarning;

    public bool IsWarningVisible => IsWarning;

    public bool IsWarningState => IsWarning;

    public bool Warning => IsWarning;

    public bool HasStaleData => IsStale;

    public bool IsStaleState => IsStale;

    public bool IsStaleData => IsStale;

    [ObservableProperty]
    private bool _isError;

    public bool HasError => IsError;

    public bool IsErrorVisible => IsError;

    public bool IsErrorState => IsError;

    public bool IsBlockingError => IsError;

    [ObservableProperty]
    private bool _isMissingToken;

    public bool MissingToken => IsMissingToken;

    public bool IsMissingTokenVisible => IsMissingToken;

    public bool IsMissingTokenState => IsMissingToken;

    public bool IsMissingConfiguration => IsMissingToken;

    [ObservableProperty]
    private bool _isEmpty;

    public bool IsEmptyState => IsEmpty;

    public bool IsEmptyVisible => IsEmpty;

    public bool Empty => IsEmpty;

    public bool IsFavoriteError => FavoriteMessageKey != CatalogMessageKey.None;

    [ObservableProperty]
    private bool _isSearchPrompt;

    [ObservableProperty]
    private bool _hasNoResults;

    [ObservableProperty]
    private Exception? _lastError;

    public Exception? Error => LastError;

    [ObservableProperty]
    private CatalogResult? _lastResult;

    public CatalogResult? Result => LastResult;

    public DateTimeOffset? LastSuccessfulRefresh => LastResult?.LastSuccessfulRefresh;

    public TimeSpan? CacheAge => LastResult?.CacheAge;

    public Task SearchDebounceTask => _searchTask;

    public TimeSpan SearchDebounce => _searchDebounce;

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

    public Task LoadAsync(CancellationToken cancellationToken = default) =>
        InitializeAsync(cancellationToken);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteInitializeAsync()
    {
        await InitializeAsync();
    }

    public IAsyncRelayCommand InitializeCommand => ExecuteInitializeCommand;

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

    public Task<CatalogResult> RefreshResultAsync(CancellationToken cancellationToken = default)
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
    private async Task ExecuteRefreshAsync()
    {
        await RefreshAsync();
    }

    public IAsyncRelayCommand RefreshCommand => ExecuteRefreshCommand;

    public Task<CatalogResult> RetryAsync(CancellationToken cancellationToken = default) =>
        RefreshAsync(cancellationToken);

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

    public Task<CatalogResult> ResumeAsync(CancellationToken cancellationToken = default) =>
        CheckForUpdatesAndReapplyDateAsync(cancellationToken);

    public Task<CatalogResult> OnResumeAsync(CancellationToken cancellationToken = default) =>
        CheckForUpdatesAndReapplyDateAsync(cancellationToken);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteRetryAsync()
    {
        await RetryAsync();
    }

    public IAsyncRelayCommand RetryCommand => ExecuteRetryCommand;

    public void SwitchSection(CatalogSection section)
    {
        if (section != SelectedSection)
        {
            CloseDetail();
            Query = string.Empty;
        }

        SelectedSection = section;
    }

    public Task SwitchSectionAsync(CatalogSection section)
    {
        SwitchSection(section);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExecuteSwitchSectionAsync(CatalogSection section)
    {
        await SwitchSectionAsync(section);
    }

    public IAsyncRelayCommand<CatalogSection> SwitchSectionCommand => ExecuteSwitchSectionCommand;

    [RelayCommand]
    public void SelectRatingFilter(MovieRatingFilter filter)
    {
        if (!Enum.IsDefined(filter))
        {
            throw new ArgumentOutOfRangeException(nameof(filter));
        }

        SelectedRatingFilter = filter;
    }

    public Task<FavoriteToggleResult> ToggleFavoriteAsync(
        MovieCardViewModel? card,
        CancellationToken cancellationToken = default
    )
    {
        if (card is null)
        {
            return Task.FromResult(
                new FavoriteToggleResult(
                    FavoriteToggleStatus.Rejected,
                    default,
                    error: new ArgumentNullException(nameof(card))
                )
            );
        }

        return ToggleFavoriteCoreAsync(card, cancellationToken);
    }

    public Task<FavoriteToggleResult> ToggleFavoriteAsync(
        int movieId,
        CancellationToken cancellationToken = default
    ) =>
        _cardsByMovieId.TryGetValue(movieId, out MovieCardViewModel? card)
            ? ToggleFavoriteAsync(card, cancellationToken)
            : Task.FromResult(
                new FavoriteToggleResult(
                    FavoriteToggleStatus.Rejected,
                    new FavoriteEntry(movieId, default),
                    error: new KeyNotFoundException($"Movie {movieId} is not in the catalog.")
                )
            );

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteToggleFavoriteAsync(MovieCardViewModel card)
    {
        await ToggleFavoriteAsync(card);
    }

    public IAsyncRelayCommand<MovieCardViewModel> ToggleFavoriteCommand =>
        ExecuteToggleFavoriteCommand;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteToggleMovieFavoriteAsync(Movie movie)
    {
        if (_cardsByMovieId.TryGetValue(movie.Id, out MovieCardViewModel? card))
        {
            await ToggleFavoriteAsync(card);
        }
    }

    public IAsyncRelayCommand<Movie> ToggleMovieFavoriteCommand =>
        ExecuteToggleMovieFavoriteCommand;

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
            _releaseWindowPolicy,
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

    public Task OpenDetailAsync(Movie? movie, CancellationToken cancellationToken = default) =>
        movie is null
            ? Task.CompletedTask
            : OpenDetailAsync(
                _cardsByMovieId.TryGetValue(movie.Id, out MovieCardViewModel? card)
                    ? card
                    : new MovieCardViewModel(
                        movie,
                        _clock,
                        _releaseWindowPolicy,
                        _navigationService
                    ),
                cancellationToken
            );

    [RelayCommand]
    private async Task ExecuteOpenDetailAsync(MovieCardViewModel card)
    {
        await OpenDetailAsync(card);
    }

    public IAsyncRelayCommand<MovieCardViewModel> OpenDetailCommand => ExecuteOpenDetailCommand;

    public Task OpenMovieDetailAsync(Movie? movie, CancellationToken cancellationToken = default) =>
        OpenDetailAsync(movie, cancellationToken);

    [RelayCommand]
    private async Task ExecuteOpenMovieDetailAsync(Movie movie)
    {
        await OpenDetailAsync(movie);
    }

    public IAsyncRelayCommand<Movie> OpenMovieDetailCommand => ExecuteOpenMovieDetailCommand;

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
        IsError = false;
        IsMissingToken = false;
        IsWarning = false;
        WarningKey = CatalogMessageKey.None;
        ErrorKey = CatalogMessageKey.None;
        LastError = null;
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
            loaded = new CatalogResult(
                CatalogResultStatus.CacheReadFailed,
                error: exception,
                cacheStatus: CatalogCacheStatus.ReadFailed
            );
        }

        loaded ??= new CatalogResult(
            CatalogResultStatus.CacheReadFailed,
            error: new InvalidOperationException("The catalog service returned no result."),
            cacheStatus: CatalogCacheStatus.ReadFailed
        );

        if (!IsCurrentVersion(initialVersion))
        {
            return;
        }

        LastResult = loaded;
        Status = loaded.Status;
        LastError = loaded.Error;
        bool hasCache =
            loaded.UsedCache
            || loaded.Status is CatalogResultStatus.FreshCache or CatalogResultStatus.StaleCache;
        if (hasCache || loaded.Status is CatalogResultStatus.Refreshed)
        {
            ReplaceCatalog(GetResultMovies(loaded), hasCatalogData: true);
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
            IsError = false;
            IsMissingToken = false;
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
            LastResult = result;
            Status = result.Status;
            LastError = result.Error;

            bool successful =
                result.Status
                is CatalogResultStatus.Refreshed
                    or CatalogResultStatus.RefreshSucceededCacheWriteFailed;
            if (successful)
            {
                ReplaceCatalog(
                    GetResultMovies(result),
                    hasCatalogData: true,
                    preserveFeedPosition: true
                );
                IsStale = false;
                IsError = false;
                IsMissingToken = false;
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
            IsError = false;
            IsMissingToken = false;
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
            LastResult = result;
            Status = result.Status;
            LastError = result.Error;

            bool remoteRefreshSucceeded =
                result.Status
                is CatalogResultStatus.Refreshed
                    or CatalogResultStatus.RefreshSucceededCacheWriteFailed;
            bool cacheReadSucceeded =
                result.Status is CatalogResultStatus.FreshCache or CatalogResultStatus.StaleCache;

            if (remoteRefreshSucceeded)
            {
                ReplaceCatalog(
                    GetResultMovies(result),
                    hasCatalogData: true,
                    preserveFeedPosition: true
                );
                IsStale = false;
                IsError = false;
                IsMissingToken = false;
                IsWarning = result.Status == CatalogResultStatus.RefreshSucceededCacheWriteFailed;
                WarningKey = IsWarning ? CatalogMessageKey.RefreshWarning : CatalogMessageKey.None;
                ErrorKey = CatalogMessageKey.None;
                await ReconcileFavoritesAsync(version, cancellationToken);
            }
            else if (cacheReadSucceeded)
            {
                ReplaceCatalog(
                    GetResultMovies(result),
                    hasCatalogData: result.Status
                        is CatalogResultStatus.FreshCache
                            or CatalogResultStatus.StaleCache
                        || result.UsedCache
                        || result.HasUsableData,
                    preserveFeedPosition: true
                );
                IsStale = result.IsStale || result.Status == CatalogResultStatus.StaleCache;
                IsError = false;
                IsMissingToken = false;
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
        return LastResult
            ?? new CatalogResult(
                CatalogResultStatus.NoCache,
                cacheStatus: CatalogCacheStatus.NoCache
            );
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
            cacheAge: null,
            isStale: hasCache,
            usedCache: hasCache,
            error: exception,
            snapshot: hasCache
                ? MovieCatalogSnapshot.Create(
                    _catalogMovies,
                    _clock.Today,
                    _releaseWindowPolicy,
                    _movieSafetyPolicy
                )
                : null,
            cacheStatus: hasCache ? CatalogCacheStatus.Available : CatalogCacheStatus.NoCache
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
        IsMissingToken = result.Status == CatalogResultStatus.MissingConfiguration;
        IsError = !hasCache;
        IsWarning = hasCache;
        WarningKey = hasCache
            ? IsMissingToken
                ? CatalogMessageKey.MissingToken
                : IsOffline
                    ? CatalogMessageKey.OfflineWarning
                    : CatalogMessageKey.RefreshWarning
            : CatalogMessageKey.None;
        ErrorKey =
            IsMissingToken ? CatalogMessageKey.MissingToken
            : IsOffline ? CatalogMessageKey.OfflineError
            : CatalogMessageKey.RefreshError;
    }

    private async Task LoadFavoritesAsync(long favoriteVersion, CancellationToken cancellationToken)
    {
        if (_favoritesStore is null)
        {
            return;
        }

        FavoritesResult? loaded = null;
        FavoritesResult? pruned = null;
        Exception? failure = null;
        try
        {
            loaded = await _favoritesStore.GetAsync(_clock.Today, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        if (loaded is not null && !loaded.Succeeded)
        {
            failure ??=
                loaded.Error ?? new InvalidOperationException("Favorites could not be loaded.");
        }

        try
        {
            pruned = await _favoritesStore.PruneAsync(_clock.Today, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            failure ??= exception;
        }
        if (pruned is not null && !pruned.Succeeded)
        {
            failure ??=
                pruned.Error ?? new InvalidOperationException("Favorites could not be pruned.");
        }

        if (favoriteVersion != Volatile.Read(ref _favoriteVersion))
        {
            return;
        }

        FavoritesResult? selected =
            pruned?.Succeeded == true ? pruned
            : loaded?.Succeeded == true ? loaded
            : null;
        if (selected is not null)
        {
            ApplyFavoriteEntries(selected.Entries);
            if (failure is null)
            {
                FavoriteMessageKey = CatalogMessageKey.None;
            }
            else
            {
                FavoriteMessageKey = CatalogMessageKey.FavoritesError;
                LastError ??= failure;
                WarningKey = CatalogMessageKey.FavoritesError;
                IsWarning = true;
            }
        }
        else if (failure is not null)
        {
            FavoriteMessageKey = CatalogMessageKey.FavoritesError;
            LastError ??= failure;
            WarningKey = CatalogMessageKey.FavoritesError;
            IsWarning = true;
        }
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
                FavoriteMessageKey = CatalogMessageKey.None;
            }
            else if (
                reconciled is not null
                && !reconciled.Succeeded
                && IsCurrentVersion(operationVersion)
            )
            {
                FavoriteMessageKey = CatalogMessageKey.FavoritesError;
                LastError ??= reconciled.Error;
                WarningKey = CatalogMessageKey.FavoritesError;
                IsWarning = true;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (IsCurrentVersion(operationVersion))
            {
                FavoriteMessageKey = CatalogMessageKey.FavoritesError;
                LastError ??= exception;
                WarningKey = CatalogMessageKey.FavoritesError;
                IsWarning = true;
            }
        }
    }

    private async Task<FavoriteToggleResult> ToggleFavoriteCoreAsync(
        MovieCardViewModel card,
        CancellationToken cancellationToken
    )
    {
        FavoriteEntry? optionalEntry = card.FavoriteEntry;
        if (optionalEntry is not FavoriteEntry entry)
        {
            FavoriteToggleResult rejected = new(
                FavoriteToggleStatus.Rejected,
                default,
                error: new InvalidOperationException("The movie has no eligible release date.")
            );
            SetFavoriteFailure(CatalogMessageKey.FavoriteNotAllowed, rejected.Error);
            return rejected;
        }

        if (_favoritesStore is null)
        {
            FavoriteToggleResult failed = new(
                FavoriteToggleStatus.Failed,
                entry,
                error: new InvalidOperationException("Favorites are not configured.")
            );
            SetFavoriteFailure(CatalogMessageKey.FavoriteSaveFailed, failed.Error);
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
                result = new FavoriteToggleResult(
                    FavoriteToggleStatus.Failed,
                    entry,
                    error: exception
                );
            }

            if (result is null)
            {
                result = new FavoriteToggleResult(
                    FavoriteToggleStatus.Failed,
                    entry,
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
                SetFavoriteFailure(failureKey, result.Error);
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

        foreach (MovieCardViewModel card in _cardsByMovieId.Values)
        {
            if (card.MovieId == entry.MovieId)
            {
                card.SetFavorite(isFavorite);
            }
        }

        if (SelectedMovieDetail?.MovieId == entry.MovieId)
        {
            SelectedMovieDetail.SetFavoriteState(isFavorite);
        }

        UpdateCounts();
        BuildCurrentView();
    }

    private void SetFavoriteFailure(CatalogMessageKey key, Exception? error)
    {
        FavoriteMessageKey = key;
        WarningKey = key;
        IsWarning = true;
        LastError ??= error;
        UpdatePresentationState();
    }

    private void ClearFavoriteFailure()
    {
        FavoriteMessageKey = CatalogMessageKey.None;
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
            if (_releaseWindowPolicy.IsVisible(entry, _clock.Today))
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

        UpdateCounts();
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
        IReadOnlyList<Movie> safeMovies = MovieCatalogSnapshot
            .Create(
                movies ?? Array.Empty<Movie>(),
                _clock.Today,
                _releaseWindowPolicy,
                _movieSafetyPolicy
            )
            .Movies;
        Dictionary<int, MovieCardViewModel> nextCards = new();
        foreach (Movie movie in safeMovies)
        {
            MovieCardViewModel card = new(
                movie,
                _favoriteEntries.ContainsKey(movie.Id),
                _clock,
                _releaseWindowPolicy,
                _navigationService,
                ToggleFavoriteCoreAsync
            );
            nextCards[movie.Id] = card;
        }

        _catalogMovies = safeMovies;
        _cardsByMovieId = nextCards;
        _hasCatalogData = hasCatalogData;
        OnPropertyChanged(nameof(CatalogMovies));
        OnPropertyChanged(nameof(HasCatalogData));
        OnPropertyChanged(nameof(HasData));
        UpdateCounts();
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

    private static IReadOnlyList<Movie> GetResultMovies(CatalogResult result) =>
        result.Movies.Count > 0 || result.Snapshot.Movies.Count == 0
            ? result.Movies
            : result.Snapshot.Movies;

    private void BuildCurrentView()
    {
        IEnumerable<MovieCardViewModel> selected = SelectedSection switch
        {
            CatalogSection.MyFavorites => _cardsByMovieId.Values.Where(card =>
                _favoriteEntries.ContainsKey(card.MovieId)
            ),
            CatalogSection.FindAMovie when !string.IsNullOrWhiteSpace(Query) =>
                _cardsByMovieId.Values.Where(card => Matches(card, NormalizedQuery)),
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
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(GroupViewModels));
        OnPropertyChanged(nameof(Cards));
        OnPropertyChanged(nameof(CurrentMovies));
        OnPropertyChanged(nameof(CurrentCards));
        OnPropertyChanged(nameof(VisibleMovies));
        OnPropertyChanged(nameof(Movies));
        OnPropertyChanged(nameof(FavoriteMovies));
        OnPropertyChanged(nameof(EmptyStateKey));
        UpdatePresentationState();
    }

    /// <summary>
    /// True when the feed already shows exactly these card instances in this
    /// order, so the grouped collections can be left alone.
    /// </summary>
    private bool IsCurrentView(IReadOnlyList<MovieCardViewModel> selectedCards)
    {
        if (MovieCards.Count != selectedCards.Count)
        {
            return false;
        }

        for (int index = 0; index < selectedCards.Count; index++)
        {
            if (!ReferenceEquals(MovieCards[index], selectedCards[index]))
            {
                return false;
            }
        }

        // Groups are a pure function of the ordered cards, but an earlier build
        // may have been skipped before the groups were ever populated.
        return MovieGroups.Sum(static group => group.Count) == selectedCards.Count;
    }

    private void UpdateCounts()
    {
        ComingSoonCount = _catalogMovies.Count;
        FavoriteCount = _catalogMovies.Count(movie => _favoriteEntries.ContainsKey(movie.Id));
        FindMovieCount =
            SelectedSection == CatalogSection.FindAMovie && !string.IsNullOrWhiteSpace(Query)
                ? _cardsByMovieId.Values.Count(card => Matches(card, NormalizedQuery))
                : 0;
        CurrentCount = MovieCards.Count;
        OnPropertyChanged(nameof(FavoritesCount));
        OnPropertyChanged(nameof(FavoriteMoviesCount));
        OnPropertyChanged(nameof(SearchResultCount));
        OnPropertyChanged(nameof(MovieCount));
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
        else if (IsError)
        {
            nextState = IsMissingToken ? CatalogViewState.MissingToken : CatalogViewState.Error;
            nextMessage = ErrorKey;
        }
        else if (IsRefreshing)
        {
            nextState = CatalogViewState.Refreshing;
            nextMessage = CatalogMessageKey.Loading;
        }
        else if (IsMissingToken && !_hasCatalogData)
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
        bool canShowEmptyState = !IsLoading && !IsRefreshing && !IsError && !IsMissingToken;
        IsEmpty = canShowEmptyState && MovieCards.Count == 0;
        IsSearchPrompt =
            canShowEmptyState
            && SelectedSection == CatalogSection.FindAMovie
            && string.IsNullOrWhiteSpace(Query);
        HasNoResults =
            canShowEmptyState
            && SelectedSection == CatalogSection.FindAMovie
            && !string.IsNullOrWhiteSpace(Query)
            && MovieCards.Count == 0;
        OnPropertyChanged(nameof(ViewState));
        OnPropertyChanged(nameof(PresentationState));
        OnPropertyChanged(nameof(CatalogStatus));
        OnPropertyChanged(nameof(CurrentMessageKey));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(HasWarning));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsBlockingError));
        OnPropertyChanged(nameof(MissingToken));
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(EmptyStateKey));
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
        || card.Genres.Any(genre =>
            NormalizeQuery(genre.Name).Contains(normalizedQuery, StringComparison.Ordinal)
        );

    private bool MatchesRatingFilter(MovieCardViewModel card) =>
        SelectedRatingFilter switch
        {
            MovieRatingFilter.All => true,
            MovieRatingFilter.G => card.MovieCertification?.IsG == true,
            MovieRatingFilter.PG => card.MovieCertification?.IsPg == true,
            MovieRatingFilter.RatingSoon => card.IsNotYetRated,
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
        OnPropertyChanged(nameof(IsOffline));
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
    }

    partial void OnSelectedSectionChanged(CatalogSection value)
    {
        OnPropertyChanged(nameof(CurrentSection));
        OnPropertyChanged(nameof(Section));
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
        OnPropertyChanged(nameof(NormalizedQuery));
        OnPropertyChanged(nameof(SearchQuery));
        OnPropertyChanged(nameof(EmptyStateKey));
        if (SelectedSection == CatalogSection.FindAMovie)
        {
            ScheduleSearch();
        }
    }

    partial void OnStatusChanged(CatalogResultStatus value)
    {
        OnPropertyChanged(nameof(ResultStatus));
        OnPropertyChanged(nameof(CatalogStatus));
    }

    partial void OnStateChanged(CatalogViewState value)
    {
        OnPropertyChanged(nameof(ViewState));
        OnPropertyChanged(nameof(PresentationState));
    }

    partial void OnLastResultChanged(CatalogResult? value)
    {
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(LastSuccessfulRefresh));
        OnPropertyChanged(nameof(CacheAge));
    }

    partial void OnMessageKeyChanged(CatalogMessageKey value)
    {
        OnPropertyChanged(nameof(CurrentMessageKey));
    }

    partial void OnWarningKeyChanged(CatalogMessageKey value)
    {
        OnPropertyChanged(nameof(WarningMessageKey));
    }

    partial void OnErrorKeyChanged(CatalogMessageKey value)
    {
        OnPropertyChanged(nameof(ErrorMessageKey));
    }

    partial void OnFavoriteMessageKeyChanged(CatalogMessageKey value)
    {
        OnPropertyChanged(nameof(FavoriteErrorKey));
        OnPropertyChanged(nameof(IsFavoriteError));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(Loading));
    }

    partial void OnIsRefreshingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(Refreshing));
    }

    partial void OnIsWarningChanged(bool value)
    {
        OnPropertyChanged(nameof(HasWarning));
        OnPropertyChanged(nameof(IsWarningVisible));
        OnPropertyChanged(nameof(IsWarningState));
        OnPropertyChanged(nameof(Warning));
    }

    partial void OnIsStaleChanged(bool value)
    {
        OnPropertyChanged(nameof(HasStaleData));
        OnPropertyChanged(nameof(IsStaleState));
        OnPropertyChanged(nameof(IsStaleData));
        OnPropertyChanged(nameof(Stale));
    }

    partial void OnIsErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsBlockingError));
        OnPropertyChanged(nameof(IsErrorVisible));
        OnPropertyChanged(nameof(IsErrorState));
    }

    partial void OnIsMissingTokenChanged(bool value)
    {
        OnPropertyChanged(nameof(MissingToken));
        OnPropertyChanged(nameof(IsMissingTokenVisible));
        OnPropertyChanged(nameof(IsMissingTokenState));
        OnPropertyChanged(nameof(IsMissingConfiguration));
    }

    partial void OnIsEmptyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(Empty));
    }
}
