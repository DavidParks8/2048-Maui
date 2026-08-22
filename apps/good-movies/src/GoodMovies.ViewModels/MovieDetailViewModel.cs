using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// Detail state for one safe catalog movie. Platform speech, navigation, and
/// link launching are injected so this type remains usable on net10.0.
/// </summary>
public partial class MovieDetailViewModel : ObservableObject, IDisposable
{
    private readonly IFavoritesStore? _favoritesStore;
    private readonly IWordLevelSpeechService? _speechService;
    private readonly IMovieTrailerLookup? _trailerLookup;
    private readonly IExternalTrailerLauncher? _trailerLauncher;
    private readonly ReleaseWindowPolicy _releaseWindowPolicy;
    private readonly SemaphoreSlim _favoriteGate = new(1, 1);
    private readonly object _trailerSync = new();
    private DateOnly? _releaseDate;

    private Task<TrailerPlaybackResult>? _trailerTask;
    private Task<TrailerPlaybackResult>? _trailerPreparationTask;
    private bool _isDeactivated;
    private bool _acceptSpeechRanges;
    private bool _disposed;

    public MovieDetailViewModel(
        Movie movie,
        IClock? clock = null,
        IFavoritesStore? favoritesStore = null,
        IWordLevelSpeechService? speechService = null,
        IMovieTrailerLookup? trailerLookup = null,
        IExternalTrailerLauncher? trailerLauncher = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        bool isFavorite = false
    )
    {
        Movie = movie ?? throw new ArgumentNullException(nameof(movie));
        Clock = clock ?? new SystemClock();
        _favoritesStore = favoritesStore;
        _speechService = speechService;
        _trailerLookup = trailerLookup;
        _trailerLauncher = trailerLauncher;
        _releaseWindowPolicy = releaseWindowPolicy ?? GoodMovies.Core.ReleaseWindowPolicy.Default;
        IsFavorite = isFavorite;

        _releaseDate = GetDisplayReleaseDate();
        StatusInfo = _releaseDate is DateOnly releaseDate
            ? _releaseWindowPolicy.GetStatusInfo(releaseDate, Clock.Today)
            : default;
        WordTokens = new ReadOnlyObservableCollection<WordTokenViewModel>(
            new ObservableCollection<WordTokenViewModel>(Tokenize(Overview).ToArray())
        );

        if (_speechService is not null)
        {
            _speechService.SpokenRange += OnSpokenRange;
            _speechService.CharacterRangeSpoken += OnSpokenRange;
            _speechService.RangeSpoken += OnSpokenRange;
        }
    }

    public MovieDetailViewModel(
        Movie movie,
        IFavoritesStore favoritesStore,
        IClock clock,
        IWordLevelSpeechService? speechService = null,
        IMovieTrailerLookup? trailerLookup = null,
        IExternalTrailerLauncher? trailerLauncher = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        bool isFavorite = false
    )
        : this(
            movie,
            clock,
            favoritesStore,
            speechService,
            trailerLookup,
            trailerLauncher,
            releaseWindowPolicy,
            isFavorite
        ) { }

    public MovieDetailViewModel(
        Movie movie,
        IFavoritesStore favoritesStore,
        IWordLevelSpeechService? speech,
        IMovieTrailerLookup? lookup,
        IExternalTrailerLauncher? launcher,
        IClock? clock = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        bool isFavorite = false
    )
        : this(
            movie,
            clock,
            favoritesStore,
            speech,
            lookup,
            launcher,
            releaseWindowPolicy,
            isFavorite
        ) { }

    public MovieDetailViewModel(
        Movie movie,
        IClock clock,
        IFavoritesStore? favoritesStore,
        IMovieTrailerLookup? lookup,
        IExternalTrailerLauncher? launcher,
        IWordLevelSpeechService? speech = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        bool isFavorite = false
    )
        : this(
            movie,
            clock,
            favoritesStore,
            speech,
            lookup,
            launcher,
            releaseWindowPolicy,
            isFavorite
        ) { }

    public MovieDetailViewModel(
        Movie movie,
        IFavoritesStore favoritesStore,
        IClock clock,
        IMovieTrailerLookup? lookup,
        IExternalTrailerLauncher? launcher,
        IWordLevelSpeechService? speech = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        bool isFavorite = false
    )
        : this(
            movie,
            clock,
            favoritesStore,
            speech,
            lookup,
            launcher,
            releaseWindowPolicy,
            isFavorite
        ) { }

    public MovieDetailViewModel(Movie movie)
        : this(movie, new SystemClock()) { }

    public Movie Movie { get; }

    public Movie SelectedMovie => Movie;

    public Movie Item => Movie;

    public IClock Clock { get; }

    public int MovieId => Movie.Id;

    public string Title => Movie.Title;

    public string Name => Movie.Title;

    public string Rating => Movie.CertificationCode ?? string.Empty;

    public string Certification => Rating;

    /// <summary>
    /// True when the MPAA has not rated this release yet. The movie is still
    /// safe to show because only animation and family titles reach the catalog
    /// without a rating.
    /// </summary>
    public bool IsNotYetRated => Movie.IsNotYetRated;

    public string Kind => Movie.Genres.FirstOrDefault()?.Name ?? string.Empty;

    public string Genre => Kind;

    public IReadOnlyList<MovieGenre> Genres => Movie.Genres;

    public string KindIcon => MovieKindIconMapper.GetIcon(Movie.Genres.FirstOrDefault());

    public string? Poster => Movie.PosterUri ?? Movie.PosterPath;

    public string? PosterSource => Poster;

    public string? PosterUri => Movie.PosterUri;

    public string? PosterPath => Movie.PosterPath;

    public string? Overview => Movie.Overview;

    public string? Synopsis => Overview;

    public DateOnly? ReleaseDate => _releaseDate;

    public DateOnly? DisplayReleaseDate => ReleaseDate;

    public FavoriteEntry? FavoriteEntry =>
        ReleaseDate is DateOnly date ? new FavoriteEntry(Movie.Id, date) : null;

    public ReleaseStatusInfo StatusInfo { get; private set; }

    public ReleaseStatusInfo ReleaseStatusInfo => StatusInfo;

    public ReleaseStatusInfo StatusData => StatusInfo;

    public ReleaseStatusInfo ReleaseStatusData => StatusInfo;

    public ReleaseStatus Status => StatusInfo.Status;

    public ReleaseStatus ReleaseStatus => Status;

    public ReleaseStatusKey StatusKey =>
        Status switch
        {
            ReleaseStatus.Future => ReleaseStatusKey.FutureSleeps,
            ReleaseStatus.Today => ReleaseStatusKey.Today,
            ReleaseStatus.InTheatersNow => ReleaseStatusKey.InTheatersNow,
            _ => ReleaseStatusKey.None,
        };

    public int Sleeps => StatusInfo.Sleeps;

    public int DaysUntilRelease => StatusInfo.DaysUntilRelease;

    /// <summary>
    /// The text passed to the injected speech service. Tokens use offsets into
    /// this exact string.
    /// </summary>
    public string ReadAloudText => Overview ?? string.Empty;

    public string TextToRead => ReadAloudText;

    public string SpeechText => ReadAloudText;

    public ReadOnlyObservableCollection<WordTokenViewModel> WordTokens { get; }

    public IReadOnlyList<WordTokenViewModel> Tokens => WordTokens;

    public IReadOnlyList<WordTokenViewModel> Words => WordTokens;

    public IReadOnlyList<WordTokenViewModel> OverviewTokens => WordTokens;

    [ObservableProperty]
    private bool _isFavorite;

    public bool IsSaved => IsFavorite;

    [ObservableProperty]
    private bool _isReading;

    public bool IsReadAloudPlaying => IsReading;

    public bool IsSpeaking => IsReading;

    public bool IsReadingAloud => IsReading;

    [ObservableProperty]
    private SpokenCharacterRange? _spokenRange;

    public SpokenCharacterRange? SpokenCharacterRange => SpokenRange;

    public SpokenCharacterRange? CurrentSpokenRange => SpokenRange;

    public IReadOnlyList<WordTokenViewModel> HighlightedTokens =>
        WordTokens.Where(static token => token.IsHighlighted).ToArray();

    [ObservableProperty]
    private CatalogMessageKey _favoriteMessageKey;

    public CatalogMessageKey FavoriteErrorKey => FavoriteMessageKey;

    public bool IsFavoriteError => FavoriteMessageKey != CatalogMessageKey.None;

    [ObservableProperty]
    private CatalogMessageKey _speechMessageKey;

    public CatalogMessageKey SpeechErrorKey => SpeechMessageKey;

    [ObservableProperty]
    private bool _isFavoriteSaving;

    [ObservableProperty]
    private TrailerPlaybackState _trailerState;

    public TrailerPlaybackState TrailerStatus => TrailerState;

    public bool IsTrailerLoading => TrailerState == TrailerPlaybackState.Loading;

    public bool IsTrailerLaunched => TrailerState == TrailerPlaybackState.Launched;

    public bool IsTrailerFound => IsTrailerLaunched;

    public bool IsTrailerNotFound => TrailerState == TrailerPlaybackState.NotFound;

    public bool IsTrailerMissingConfiguration =>
        TrailerState == TrailerPlaybackState.MissingConfiguration;

    public bool IsTrailerMissingConfig => IsTrailerMissingConfiguration;

    public bool IsTrailerMissingToken => IsTrailerMissingConfiguration;

    public bool IsTrailerFailed =>
        TrailerState is TrailerPlaybackState.Failed or TrailerPlaybackState.LaunchFailed;

    public bool IsTrailerFailure => IsTrailerFailed;

    public bool IsTrailerLaunchFailed => TrailerState == TrailerPlaybackState.LaunchFailed;

    [ObservableProperty]
    private TrailerPlaybackResult? _lastTrailerResult;

    public TrailerLookupResult? TrailerLookupResult => LastTrailerResult?.Lookup;

    public MovieTrailer? SelectedTrailer => LastTrailerResult?.Lookup?.Trailer;

    public string? SelectedTrailerKey => LastTrailerResult?.YouTubeKey;

    public bool IsTrailerAvailable =>
        TrailerState is TrailerPlaybackState.Ready or TrailerPlaybackState.Launched;

    public event EventHandler<FavoriteChangedEventArgs>? FavoriteChanged;

    public void SetFavoriteState(bool isFavorite)
    {
        IsFavorite = isFavorite;
    }

    public void ReapplyCurrentDatePolicies()
    {
        _releaseDate = GetDisplayReleaseDate();
        StatusInfo = _releaseDate is DateOnly releaseDate
            ? _releaseWindowPolicy.GetStatusInfo(releaseDate, Clock.Today)
            : default;

        OnPropertyChanged(nameof(ReleaseDate));
        OnPropertyChanged(nameof(DisplayReleaseDate));
        OnPropertyChanged(nameof(FavoriteEntry));
        OnPropertyChanged(nameof(StatusInfo));
        OnPropertyChanged(nameof(ReleaseStatusInfo));
        OnPropertyChanged(nameof(StatusData));
        OnPropertyChanged(nameof(ReleaseStatusData));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(ReleaseStatus));
        OnPropertyChanged(nameof(StatusKey));
        OnPropertyChanged(nameof(Sleeps));
        OnPropertyChanged(nameof(DaysUntilRelease));
    }

    public async Task LoadFavoriteAsync(CancellationToken cancellationToken = default)
    {
        if (_favoritesStore is null)
        {
            return;
        }

        FavoritesResult? result;
        try
        {
            result = await _favoritesStore.GetAsync(Clock.Today, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            FavoriteMessageKey = CatalogMessageKey.FavoritesError;
            return;
        }

        if (result is not null && result.Succeeded)
        {
            IsFavorite = result.Entries.Any(entry => entry.MovieId == Movie.Id);
            FavoriteMessageKey = CatalogMessageKey.None;
        }
        else if (result is not null)
        {
            FavoriteMessageKey = CatalogMessageKey.FavoritesError;
        }
    }

    public Task<FavoriteToggleResult> ToggleFavoriteAsync(
        CancellationToken cancellationToken = default
    ) => ToggleFavoriteCoreAsync(cancellationToken);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteToggleFavoriteAsync()
    {
        await ToggleFavoriteAsync();
    }

    public IAsyncRelayCommand ToggleFavoriteCommand => ExecuteToggleFavoriteCommand;

    private async Task<FavoriteToggleResult> ToggleFavoriteCoreAsync(
        CancellationToken cancellationToken
    )
    {
        FavoriteEntry? entry = FavoriteEntry;
        if (entry is not FavoriteEntry favoriteEntry)
        {
            FavoriteToggleResult rejected = new(
                FavoriteToggleStatus.Rejected,
                default,
                error: new InvalidOperationException("The movie has no eligible release date.")
            );
            FavoriteMessageKey = CatalogMessageKey.FavoriteNotAllowed;
            return rejected;
        }

        if (_favoritesStore is null)
        {
            FavoriteToggleResult unavailable = new(
                FavoriteToggleStatus.Failed,
                favoriteEntry,
                error: new InvalidOperationException("Favorites are not configured.")
            );
            FavoriteMessageKey = CatalogMessageKey.FavoriteSaveFailed;
            return unavailable;
        }

        await _favoriteGate.WaitAsync(cancellationToken);
        IsFavoriteSaving = true;
        try
        {
            FavoriteToggleResult? result = await _favoritesStore.ToggleAsync(
                favoriteEntry,
                Clock.Today,
                cancellationToken
            );
            if (result is null)
            {
                result = new FavoriteToggleResult(
                    FavoriteToggleStatus.Failed,
                    favoriteEntry,
                    error: new InvalidOperationException("The favorites store returned no result.")
                );
            }

            if (result.Status is FavoriteToggleStatus.Added or FavoriteToggleStatus.Removed)
            {
                bool isFavorite = result.Status == FavoriteToggleStatus.Added;
                IsFavorite = isFavorite;
                FavoriteMessageKey = CatalogMessageKey.None;
                FavoriteChanged?.Invoke(
                    this,
                    new FavoriteChangedEventArgs(Movie.Id, isFavorite, favoriteEntry)
                );
            }
            else
            {
                FavoriteMessageKey =
                    result.Status == FavoriteToggleStatus.Rejected
                        ? CatalogMessageKey.FavoriteNotAllowed
                        : CatalogMessageKey.FavoriteSaveFailed;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            FavoriteMessageKey = CatalogMessageKey.FavoriteSaveFailed;
            return new FavoriteToggleResult(
                FavoriteToggleStatus.Failed,
                favoriteEntry,
                error: exception
            );
        }
        finally
        {
            IsFavoriteSaving = false;
            _favoriteGate.Release();
        }
    }

    public async Task PlayReadAloudAsync(CancellationToken cancellationToken = default)
    {
        if (_speechService is null || string.IsNullOrWhiteSpace(ReadAloudText))
        {
            IsReading = false;
            SpeechMessageKey = CatalogMessageKey.SpeechFailed;
            return;
        }

        _isDeactivated = false;
        if (IsReading)
        {
            _speechService.Stop();
        }

        _acceptSpeechRanges = true;
        SpeechMessageKey = CatalogMessageKey.None;
        ClearSpokenRange();
        IsReading = true;
        try
        {
            await _speechService.SpeakAsync(ReadAloudText, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }
        catch
        {
            SpeechMessageKey = CatalogMessageKey.SpeechFailed;
        }
        finally
        {
            IsReading = false;
            ClearSpokenRange();
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecutePlayReadAloudAsync()
    {
        await PlayReadAloudAsync();
    }

    public IAsyncRelayCommand PlayReadAloudCommand => ExecutePlayReadAloudCommand;

    public IAsyncRelayCommand ReadAloudCommand => PlayReadAloudCommand;

    public void StopReadAloud()
    {
        _acceptSpeechRanges = false;
        _speechService?.Stop();
        IsReading = false;
        ClearSpokenRange();
    }

    [RelayCommand]
    private void ExecuteStopReadAloud()
    {
        StopReadAloud();
    }

    public IRelayCommand StopReadAloudCommand => ExecuteStopReadAloudCommand;

    public IRelayCommand StopReadingCommand => StopReadAloudCommand;

    public IRelayCommand StopCommand => StopReadAloudCommand;

    public IRelayCommand StopReadingAloudCommand => StopReadAloudCommand;

    public Task SpeakWordAsync(
        WordTokenViewModel? token,
        CancellationToken cancellationToken = default
    )
    {
        if (token is null)
        {
            return Task.CompletedTask;
        }

        if (IsReading)
        {
            _speechService?.Stop();
            IsReading = false;
        }

        ClearSpokenRange();
        token.IsHighlighted = true;
        _acceptSpeechRanges = true;
        SpokenRange = new SpokenCharacterRange(token.Start, token.Length);
        return SpeakWordCoreAsync(token.Text, cancellationToken);
    }

    public Task SpeakWordAsync(string? word, CancellationToken cancellationToken = default)
    {
        if (IsReading)
        {
            _speechService?.Stop();
            IsReading = false;
        }

        _acceptSpeechRanges = true;
        return SpeakWordCoreAsync(word ?? string.Empty, cancellationToken);
    }

    public Task TapWordAsync(
        WordTokenViewModel? token,
        CancellationToken cancellationToken = default
    ) => SpeakWordAsync(token, cancellationToken);

    private async Task SpeakWordCoreAsync(string word, CancellationToken cancellationToken)
    {
        if (_speechService is null || string.IsNullOrWhiteSpace(word))
        {
            SpeechMessageKey = CatalogMessageKey.SpeechFailed;
            return;
        }

        SpeechMessageKey = CatalogMessageKey.None;
        try
        {
            await _speechService.SpeakWordAsync(word, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }
        catch
        {
            SpeechMessageKey = CatalogMessageKey.SpeechFailed;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteSpeakWordAsync(WordTokenViewModel token)
    {
        await SpeakWordAsync(token);
    }

    public IAsyncRelayCommand<WordTokenViewModel> SpeakWordCommand => ExecuteSpeakWordCommand;

    public IAsyncRelayCommand<WordTokenViewModel> TapWordCommand => SpeakWordCommand;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteSpeakWordTextAsync(string word)
    {
        await SpeakWordAsync(word);
    }

    public IAsyncRelayCommand<string> SpeakWordTextCommand => ExecuteSpeakWordTextCommand;

    public Task<TrailerPlaybackResult> PlayTrailerAsync(
        CancellationToken cancellationToken = default
    )
    {
        lock (_trailerSync)
        {
            if (_trailerTask is not null)
            {
                return _trailerTask;
            }

            Task<TrailerPlaybackResult> task = PlayTrailerCoreAsync(cancellationToken);
            _trailerTask = task;
            _ = ClearTrailerTaskAsync(task);
            return task;
        }
    }

    public Task<TrailerPlaybackResult> PrepareTrailerAsync(
        CancellationToken cancellationToken = default
    )
    {
        lock (_trailerSync)
        {
            if (
                LastTrailerResult is { } existing
                && TrailerState
                    is TrailerPlaybackState.Ready
                        or TrailerPlaybackState.NotFound
                        or TrailerPlaybackState.MissingConfiguration
                        or TrailerPlaybackState.Failed
            )
            {
                return Task.FromResult(existing);
            }

            if (_trailerPreparationTask is not null)
            {
                return _trailerPreparationTask;
            }

            Task<TrailerPlaybackResult> task = PrepareTrailerCoreAsync(cancellationToken);
            _trailerPreparationTask = task;
            _ = ClearTrailerPreparationTaskAsync(task);
            return task;
        }
    }

    public Task<TrailerPlaybackResult> PlayTrailerResultAsync(
        CancellationToken cancellationToken = default
    ) => PlayTrailerAsync(cancellationToken);

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecutePlayTrailerAsync()
    {
        await PlayTrailerAsync();
    }

    public IAsyncRelayCommand PlayTrailerCommand => ExecutePlayTrailerCommand;

    private async Task<TrailerPlaybackResult> PlayTrailerCoreAsync(
        CancellationToken cancellationToken
    )
    {
        TrailerPlaybackResult prepared = await PrepareTrailerAsync(cancellationToken);
        if (
            prepared.State != TrailerPlaybackState.Ready
            || prepared.Lookup?.Trailer is not MovieTrailer trailer
            || string.IsNullOrWhiteSpace(trailer.Key)
        )
        {
            return prepared;
        }

        if (_trailerLauncher is null)
        {
            return SetTrailerResult(
                new TrailerPlaybackResult(
                    TrailerPlaybackState.MissingConfiguration,
                    prepared.Lookup,
                    trailer.Key,
                    new InvalidOperationException("Trailer launcher is not configured.")
                )
            );
        }

        bool launched;
        try
        {
            launched = await _trailerLauncher.LaunchAsync(trailer.Key, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            SetTrailerState(TrailerPlaybackState.Ready, prepared);
            throw;
        }
        catch (Exception exception)
        {
            return SetTrailerResult(
                new TrailerPlaybackResult(
                    TrailerPlaybackState.LaunchFailed,
                    prepared.Lookup,
                    trailer.Key,
                    exception
                )
            );
        }

        return SetTrailerResult(
            new TrailerPlaybackResult(
                launched ? TrailerPlaybackState.Launched : TrailerPlaybackState.LaunchFailed,
                prepared.Lookup,
                trailer.Key,
                launched
                    ? null
                    : new InvalidOperationException("The trailer could not be launched.")
            )
        );
    }

    private async Task<TrailerPlaybackResult> PrepareTrailerCoreAsync(
        CancellationToken cancellationToken
    )
    {
        SetTrailerState(TrailerPlaybackState.Loading, null);
        LastTrailerResult = null;

        if (_trailerLookup is null)
        {
            return SetTrailerResult(
                new TrailerPlaybackResult(
                    TrailerPlaybackState.MissingConfiguration,
                    error: new InvalidOperationException("Trailer lookup is not configured.")
                )
            );
        }

        TrailerLookupResult? lookup;
        try
        {
            lookup = await _trailerLookup.GetTrailerAsync(Movie.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            SetTrailerState(TrailerPlaybackState.Idle, null);
            throw;
        }
        catch (Exception exception)
        {
            return SetTrailerResult(
                new TrailerPlaybackResult(
                    IsMissingConfigurationException(exception)
                        ? TrailerPlaybackState.MissingConfiguration
                        : TrailerPlaybackState.Failed,
                    error: exception
                )
            );
        }

        if (lookup is null)
        {
            return SetTrailerResult(
                new TrailerPlaybackResult(
                    TrailerPlaybackState.Failed,
                    error: new InvalidOperationException("Trailer lookup returned no result.")
                )
            );
        }

        switch (lookup.Status)
        {
            case TrailerLookupStatus.NotFound:
                return SetTrailerResult(
                    new TrailerPlaybackResult(TrailerPlaybackState.NotFound, lookup)
                );
            case TrailerLookupStatus.MissingConfiguration:
                return SetTrailerResult(
                    new TrailerPlaybackResult(
                        TrailerPlaybackState.MissingConfiguration,
                        lookup,
                        error: lookup.Error
                    )
                );
            case TrailerLookupStatus.Failed:
                return SetTrailerResult(
                    new TrailerPlaybackResult(
                        TrailerPlaybackState.Failed,
                        lookup,
                        error: lookup.Error
                    )
                );
        }

        MovieTrailer? trailer = lookup.Trailer;
        if (trailer is null || !YouTubeVideoKey.IsValid(trailer.Key) || !trailer.IsYouTube)
        {
            return SetTrailerResult(
                new TrailerPlaybackResult(TrailerPlaybackState.NotFound, lookup)
            );
        }

        return SetTrailerResult(
            new TrailerPlaybackResult(TrailerPlaybackState.Ready, lookup, trailer.Key)
        );
    }

    private TrailerPlaybackResult SetTrailerResult(TrailerPlaybackResult result)
    {
        LastTrailerResult = result;
        SetTrailerState(result.State, result);
        return result;
    }

    private void SetTrailerState(TrailerPlaybackState state, TrailerPlaybackResult? result)
    {
        TrailerState = state;
        if (result is not null)
        {
            LastTrailerResult = result;
        }
    }

    private async Task ClearTrailerTaskAsync(Task<TrailerPlaybackResult> task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // The task is observed by callers; this continuation only releases
            // the duplicate-operation guard.
        }
        finally
        {
            lock (_trailerSync)
            {
                if (ReferenceEquals(_trailerTask, task))
                {
                    _trailerTask = null;
                }
            }
        }
    }

    private async Task ClearTrailerPreparationTaskAsync(Task<TrailerPlaybackResult> task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // The caller observes operation failures; this continuation only
            // releases the duplicate-operation guard.
        }
        finally
        {
            lock (_trailerSync)
            {
                if (ReferenceEquals(_trailerPreparationTask, task))
                {
                    _trailerPreparationTask = null;
                }
            }
        }
    }

    public void Activate()
    {
        _isDeactivated = false;
    }

    public void Deactivate()
    {
        if (_isDeactivated)
        {
            return;
        }

        _isDeactivated = true;
        StopReadAloud();
    }

    public void OnDeactivated() => Deactivate();

    public void OnNavigatedFrom() => Deactivate();

    public Task OnNavigatedFromAsync()
    {
        Deactivate();
        return Task.CompletedTask;
    }

    public void OnDisappearing() => Deactivate();

    public void OnNavigatedTo() => Activate();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Deactivate();
        if (_speechService is not null)
        {
            _speechService.SpokenRange -= OnSpokenRange;
            _speechService.CharacterRangeSpoken -= OnSpokenRange;
            _speechService.RangeSpoken -= OnSpokenRange;
        }
    }

    private void OnSpokenRange(object? sender, SpeechRangeEventArgs args)
    {
        if (_isDeactivated || _disposed)
        {
            return;
        }

        if (!_acceptSpeechRanges)
        {
            return;
        }

        SpokenRange = args.Range;
    }

    public void ReportSpokenRange(int start, int length)
    {
        if (!_isDeactivated && !_disposed && _acceptSpeechRanges)
        {
            SpokenRange = new SpokenCharacterRange(start, length);
        }
    }

    public void ReportSpokenRange(SpokenCharacterRange range) =>
        ReportSpokenRange(range.Start, range.Length);

    private void ClearSpokenRange()
    {
        SpokenRange = null;
        foreach (WordTokenViewModel token in WordTokens)
        {
            token.IsHighlighted = false;
        }
    }

    private DateOnly? GetDisplayReleaseDate()
    {
        DateOnly? visibleDate = Movie
            .UsTheatricalReleases.Where(release =>
                _releaseWindowPolicy.IsVisible(release.ReleaseDate, Clock.Today)
            )
            .Select(static release => (DateOnly?)release.ReleaseDate)
            .OrderBy(static date => date)
            .FirstOrDefault();
        return visibleDate ?? Movie.UsTheatricalReleaseDate;
    }

    private static bool IsMissingConfigurationException(Exception exception) =>
        exception.GetType().Name.Contains("Configuration", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("token", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<WordTokenViewModel> Tokenize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            int start = index;
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index > start)
            {
                yield return new WordTokenViewModel(
                    text.Substring(start, index - start),
                    start,
                    index - start
                );
            }
        }
    }

    partial void OnIsFavoriteChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSaved));
        OnPropertyChanged(nameof(Favorite));
    }

    partial void OnIsReadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsReadAloudPlaying));
        OnPropertyChanged(nameof(IsSpeaking));
        OnPropertyChanged(nameof(IsReadingAloud));
    }

    partial void OnIsFavoriteSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSavingFavorite));
    }

    partial void OnSpokenRangeChanged(SpokenCharacterRange? value)
    {
        foreach (WordTokenViewModel token in WordTokens)
        {
            bool highlighted =
                value is SpokenCharacterRange range
                && (
                    range.Length > 0
                        ? token.Start < range.End && range.Start < token.End
                        : token.Start <= range.Start && range.Start < token.End
                );
            token.IsHighlighted = highlighted;
        }

        OnPropertyChanged(nameof(SpokenCharacterRange));
        OnPropertyChanged(nameof(CurrentSpokenRange));
        OnPropertyChanged(nameof(HighlightedTokens));
    }

    partial void OnFavoriteMessageKeyChanged(CatalogMessageKey value)
    {
        OnPropertyChanged(nameof(FavoriteErrorKey));
        OnPropertyChanged(nameof(IsFavoriteError));
    }

    partial void OnSpeechMessageKeyChanged(CatalogMessageKey value)
    {
        OnPropertyChanged(nameof(SpeechErrorKey));
    }

    partial void OnTrailerStateChanged(TrailerPlaybackState value)
    {
        OnPropertyChanged(nameof(TrailerStatus));
        OnPropertyChanged(nameof(IsTrailerLoading));
        OnPropertyChanged(nameof(IsTrailerLaunched));
        OnPropertyChanged(nameof(IsTrailerFound));
        OnPropertyChanged(nameof(IsTrailerNotFound));
        OnPropertyChanged(nameof(IsTrailerMissingConfiguration));
        OnPropertyChanged(nameof(IsTrailerMissingConfig));
        OnPropertyChanged(nameof(IsTrailerMissingToken));
        OnPropertyChanged(nameof(IsTrailerFailed));
        OnPropertyChanged(nameof(IsTrailerFailure));
        OnPropertyChanged(nameof(IsTrailerLaunchFailed));
        OnPropertyChanged(nameof(IsTrailerAvailable));
    }

    partial void OnLastTrailerResultChanged(TrailerPlaybackResult? value)
    {
        OnPropertyChanged(nameof(TrailerLookupResult));
        OnPropertyChanged(nameof(SelectedTrailer));
        OnPropertyChanged(nameof(SelectedTrailerKey));
    }

    public bool IsSavingFavorite => IsFavoriteSaving;

    public bool Favorite => IsFavorite;
}
