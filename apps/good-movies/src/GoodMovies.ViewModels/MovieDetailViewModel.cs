using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// Detail state for one safe catalog movie with injected speech and trailer services.
/// </summary>
public sealed partial class MovieDetailViewModel : ObservableObject, IDisposable
{
    private readonly IFavoritesStore? _favoritesStore;
    private readonly IWordLevelSpeechService? _speechService;
    private readonly IMovieTrailerLookup? _trailerLookup;
    private readonly ITrailerLauncher? _trailerLauncher;
    private readonly SemaphoreSlim _favoriteGate = new(1, 1);
    private readonly object _trailerSync = new();
    private readonly IClock _clock;
    private DateOnly? _releaseDate;

    private Task<TrailerPlaybackResult>? _trailerTask;
    private Task<TrailerPlaybackResult>? _trailerPreparationTask;
    private bool _isDeactivated;
    private bool _disposed;

    public MovieDetailViewModel(
        Movie movie,
        IClock? clock = null,
        IFavoritesStore? favoritesStore = null,
        IWordLevelSpeechService? speechService = null,
        IMovieTrailerLookup? trailerLookup = null,
        ITrailerLauncher? trailerLauncher = null,
        bool isFavorite = false
    )
    {
        Movie = movie ?? throw new ArgumentNullException(nameof(movie));
        MovieGenre? primaryGenre = Movie.Genres.Count == 0 ? null : Movie.Genres[0];
        Kind = primaryGenre?.Name ?? string.Empty;
        KindIcon = MovieKindIconMapper.GetIcon(primaryGenre);
        _clock = clock ?? new SystemClock();
        _favoritesStore = favoritesStore;
        _speechService = speechService;
        _trailerLookup = trailerLookup;
        _trailerLauncher = trailerLauncher;
        IsFavorite = isFavorite;

        _releaseDate = GetDisplayReleaseDate();
        StatusInfo = _releaseDate is DateOnly releaseDate
            ? ReleaseWindowPolicy.GetStatusInfo(releaseDate, _clock.Today)
            : default;
        WordTokens = Tokenize(Overview).ToArray();

        if (_speechService is not null)
        {
            _speechService.SpokenRange += OnSpokenRange;
        }
    }

    public Movie Movie { get; }

    public int MovieId => Movie.Id;

    public string Title => Movie.Title;

    public string Rating => Movie.Certification?.Code ?? string.Empty;

    public string Kind { get; }

    public string KindIcon { get; }

    public string? PosterSource => Movie.PosterUri?.AbsoluteUri ?? Movie.PosterPath;

    public string? Overview => Movie.Overview;

    public DateOnly? ReleaseDate => _releaseDate;

    private FavoriteEntry? FavoriteEntry =>
        ReleaseDate is DateOnly date ? new FavoriteEntry(Movie.Id, date) : null;

    private ReleaseStatusInfo StatusInfo { get; set; }

    public ReleaseStatus Status => StatusInfo.Status;

    public int Sleeps => StatusInfo.Sleeps;

    private string ReadAloudText => Overview ?? string.Empty;

    public IReadOnlyList<WordTokenViewModel> WordTokens { get; }

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isReading;

    [ObservableProperty]
    private CatalogMessageKey _favoriteMessageKey;

    [ObservableProperty]
    private CatalogMessageKey _speechMessageKey;

    [ObservableProperty]
    private TrailerPlaybackState _trailerState;

    [ObservableProperty]
    private bool _isTrailerPlaying;

    [ObservableProperty]
    private bool _isAnotherTrailerPlaying;

    public bool CanPlayTrailer => TrailerState != TrailerPlaybackState.Loading && !IsTrailerPlaying;

    public bool IsTrailerMessageVisible =>
        !IsAnotherTrailerPlaying
        && TrailerState
            is TrailerPlaybackState.NotFound
                or TrailerPlaybackState.MissingConfiguration
                or TrailerPlaybackState.Failed
                or TrailerPlaybackState.LaunchFailed;

    private TrailerPlaybackResult? LastTrailerResult
    {
        get => _lastTrailerResult;
        set
        {
            if (SetProperty(ref _lastTrailerResult, value))
            {
                OnPropertyChanged(nameof(SelectedTrailerKey));
            }
        }
    }

    private TrailerPlaybackResult? _lastTrailerResult;

    public string? SelectedTrailerKey => LastTrailerResult?.Trailer?.Key;

    public event EventHandler<FavoriteChangedEventArgs>? FavoriteChanged;

    public void SetFavoriteState(bool isFavorite)
    {
        IsFavorite = isFavorite;
    }

    public void SetTrailerPlaybackContext(bool isCurrentTrailer, bool isAnotherTrailerPlaying)
    {
        IsTrailerPlaying = isCurrentTrailer;
        IsAnotherTrailerPlaying = isAnotherTrailerPlaying;
    }

    public void ReapplyCurrentDatePolicies()
    {
        _releaseDate = GetDisplayReleaseDate();
        StatusInfo = _releaseDate is DateOnly releaseDate
            ? ReleaseWindowPolicy.GetStatusInfo(releaseDate, _clock.Today)
            : default;

        OnPropertyChanged(nameof(ReleaseDate));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Sleeps));
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task<FavoriteToggleResult> ToggleFavorite() => ToggleFavoriteAsync();

    public async Task<FavoriteToggleResult> ToggleFavoriteAsync(
        CancellationToken cancellationToken = default
    )
    {
        FavoriteEntry? entry = FavoriteEntry;
        if (entry is not FavoriteEntry favoriteEntry)
        {
            FavoriteToggleResult rejected = new(
                FavoriteToggleStatus.Rejected,
                error: new InvalidOperationException("The movie has no eligible release date.")
            );
            FavoriteMessageKey = CatalogMessageKey.FavoriteNotAllowed;
            return rejected;
        }

        if (_favoritesStore is null)
        {
            FavoriteToggleResult unavailable = new(
                FavoriteToggleStatus.Failed,
                error: new InvalidOperationException("Favorites are not configured.")
            );
            FavoriteMessageKey = CatalogMessageKey.FavoriteSaveFailed;
            return unavailable;
        }

        await _favoriteGate.WaitAsync(cancellationToken);
        try
        {
            FavoriteToggleResult? result = await _favoritesStore.ToggleAsync(
                favoriteEntry,
                _clock.Today,
                cancellationToken
            );
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
                IsFavorite = isFavorite;
                FavoriteMessageKey = CatalogMessageKey.None;
                FavoriteChanged?.Invoke(
                    this,
                    new FavoriteChangedEventArgs(favoriteEntry, isFavorite)
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
            return new FavoriteToggleResult(FavoriteToggleStatus.Failed, error: exception);
        }
        finally
        {
            _favoriteGate.Release();
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
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
            _speechService.StopSpeaking();
        }

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

    [RelayCommand]
    public void StopReadAloud()
    {
        _speechService?.StopSpeaking();
        IsReading = false;
        ClearSpokenRange();
    }

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
            _speechService?.StopSpeaking();
            IsReading = false;
        }

        HighlightRange(new SpokenCharacterRange(token.Start, token.Length));
        return SpeakWordCoreAsync(token.Text, cancellationToken);
    }

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

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task<TrailerPlaybackResult> PlayTrailer() => PlayTrailerAsync();

    private async Task<TrailerPlaybackResult> PlayTrailerCoreAsync(
        CancellationToken cancellationToken
    )
    {
        TrailerPlaybackResult prepared = await PrepareTrailerAsync(cancellationToken);
        if (
            prepared.State != TrailerPlaybackState.Ready
            || prepared.Trailer is not MovieTrailer trailer
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
                    trailer,
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
                new TrailerPlaybackResult(TrailerPlaybackState.LaunchFailed, trailer, exception)
            );
        }

        return SetTrailerResult(
            new TrailerPlaybackResult(
                launched ? TrailerPlaybackState.Launched : TrailerPlaybackState.LaunchFailed,
                trailer,
                launched
                    ? null
                    : new InvalidOperationException("The trailer player could not be presented.")
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
                return SetTrailerResult(new TrailerPlaybackResult(TrailerPlaybackState.NotFound));
            case TrailerLookupStatus.MissingConfiguration:
                return SetTrailerResult(
                    new TrailerPlaybackResult(
                        TrailerPlaybackState.MissingConfiguration,
                        error: lookup.Error
                    )
                );
            case TrailerLookupStatus.Failed:
                return SetTrailerResult(
                    new TrailerPlaybackResult(TrailerPlaybackState.Failed, error: lookup.Error)
                );
        }

        MovieTrailer? trailer = lookup.Trailer;
        if (trailer is null || !YouTubeVideoKey.IsValid(trailer.Key) || !trailer.IsYouTube)
        {
            return SetTrailerResult(new TrailerPlaybackResult(TrailerPlaybackState.NotFound));
        }

        return SetTrailerResult(new TrailerPlaybackResult(TrailerPlaybackState.Ready, trailer));
    }

    private TrailerPlaybackResult SetTrailerResult(TrailerPlaybackResult result)
    {
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
        }

        GC.SuppressFinalize(this);
    }

    private void OnSpokenRange(object? sender, SpeechRangeEventArgs args)
    {
        if (_isDeactivated || _disposed)
        {
            return;
        }

        if (!IsReading)
        {
            return;
        }

        HighlightRange(args.Range);
    }

    private void ClearSpokenRange() => HighlightRange(null);

    private void HighlightRange(SpokenCharacterRange? range)
    {
        WordTokenViewModel? highlightedToken = null;
        if (range is SpokenCharacterRange value)
        {
            long rangeEnd = (long)value.Start + value.Length;
            foreach (WordTokenViewModel token in WordTokens)
            {
                bool intersects =
                    value.Length > 0
                        ? token.Start < rangeEnd && value.Start < token.End
                        : token.Start <= value.Start && value.Start < token.End;
                if (intersects)
                {
                    highlightedToken = token;
                    break;
                }
            }
        }

        foreach (WordTokenViewModel token in WordTokens)
        {
            token.IsHighlighted = ReferenceEquals(token, highlightedToken);
        }
    }

    private DateOnly? GetDisplayReleaseDate() =>
        ReleaseWindowPolicy.GetVisibleRelease(Movie, _clock.Today)?.ReleaseDate
        ?? Movie.UsTheatricalReleaseDate;

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

    partial void OnTrailerStateChanged(TrailerPlaybackState value)
    {
        OnPropertyChanged(nameof(CanPlayTrailer));
        OnPropertyChanged(nameof(IsTrailerMessageVisible));
    }

    partial void OnIsTrailerPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanPlayTrailer));
    }

    partial void OnIsAnotherTrailerPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTrailerMessageVisible));
    }
}
