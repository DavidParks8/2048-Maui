using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// The three sections in the Design E catalog.
/// </summary>
public enum CatalogSection
{
    ComingSoon,
    MyFavorites,
    FindAMovie,

    Coming = ComingSoon,
    Favorites = MyFavorites,
    Search = FindAMovie,
}

public enum MovieRatingFilter
{
    All,
    G,
    PG,
    RatingSoon,
}

/// <summary>
/// A presentation state. Text for these states belongs to the MAUI
/// localization layer; the ViewModels expose only semantic keys.
/// </summary>
public enum CatalogViewState
{
    Idle,
    Loading,
    Ready,
    Refreshing,
    Stale,
    Warning,
    Empty,
    SearchPrompt,
    NoResults,
    Error,
    MissingToken,

    Failed = Error,
    MissingConfiguration = MissingToken,
    NoSearchResults = NoResults,
}

public enum CatalogMessageKey
{
    None,
    Loading,
    SearchPrompt,
    NoSearchResults,
    NoFavorites,
    NoMovies,
    RefreshWarning,
    RefreshError,
    OfflineWarning,
    OfflineError,
    MissingToken,
    FavoritesError,
    FavoriteSaveFailed,
    FavoriteNotAllowed,
    SpeechFailed,

    NoResults = NoSearchResults,
    NoFavorite = NoFavorites,
    MissingConfiguration = MissingToken,
}

/// <summary>
/// Identifies the semantic heading for a group. The UI supplies the localized
/// heading and formats <see cref="MovieGroupViewModel.ReleaseDate"/>.
/// </summary>
public enum MovieGroupKind
{
    InTheatersNow,
    ReleaseDate,

    InTheaters = InTheatersNow,
    FutureDate = ReleaseDate,
    Date = ReleaseDate,
}

public enum MovieGroupHeaderKey
{
    InTheatersNow,
    ReleaseDate,

    Date = ReleaseDate,
}

public enum ReleaseStatusKey
{
    None,
    FutureSleeps,
    Today,
    InTheatersNow,

    InTheaters = InTheatersNow,
}

public interface INavigationService
{
    Task NavigateToMovieDetailAsync(int movieId, CancellationToken cancellationToken = default);

    Task NavigateBackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    Task NavigateToMovieDetailAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(movie);
        return NavigateToMovieDetailAsync(movie.Id, cancellationToken);
    }

    Task OpenMovieDetailAsync(int movieId, CancellationToken cancellationToken = default) =>
        NavigateToMovieDetailAsync(movieId, cancellationToken);

    Task OpenMovieDetailAsync(Movie movie, CancellationToken cancellationToken = default) =>
        NavigateToMovieDetailAsync(movie, cancellationToken);

    Task NavigateAsync(int movieId, CancellationToken cancellationToken = default) =>
        NavigateToMovieDetailAsync(movieId, cancellationToken);

    Task NavigateToMovieAsync(int movieId, CancellationToken cancellationToken = default) =>
        NavigateToMovieDetailAsync(movieId, cancellationToken);

    Task OpenDetailAsync(int movieId, CancellationToken cancellationToken = default) =>
        NavigateToMovieDetailAsync(movieId, cancellationToken);
}

public interface IMovieNavigationService : INavigationService { }

public interface INetworkStatusService
{
    bool IsInternetAvailable { get; }

    event EventHandler? NetworkStatusChanged;
}

/// <summary>
/// Reports character ranges as speech progresses. Ranges use zero-based
/// character offsets and a non-inclusive length.
/// </summary>
public sealed class SpeechRangeEventArgs : EventArgs
{
    public SpeechRangeEventArgs(int start, int length)
    {
        Start = Math.Max(0, start);
        Length = Math.Max(0, length);
    }

    public SpeechRangeEventArgs(SpokenCharacterRange range)
        : this(range.Start, range.Length) { }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;

    public int CharacterStart => Start;

    public int CharacterLength => Length;

    public SpokenCharacterRange Range => new(Start, Length);
}

public readonly record struct SpokenCharacterRange(int Start, int Length)
{
    public int End => Start + Length;
}

/// <summary>
/// Platform-neutral word-aware speech abstraction. A MAUI adapter can forward
/// TextToSpeech progress callbacks to <see cref="SpokenRange"/>.
/// </summary>
public interface IWordLevelSpeechService
{
    event EventHandler<SpeechRangeEventArgs>? SpokenRange;

    event EventHandler<SpeechRangeEventArgs>? CharacterRangeSpoken
    {
        add => SpokenRange += value;
        remove => SpokenRange -= value;
    }

    event EventHandler<SpeechRangeEventArgs>? RangeSpoken
    {
        add => SpokenRange += value;
        remove => SpokenRange -= value;
    }

    Task SpeakAsync(string text, CancellationToken cancellationToken = default);

    Task SpeakWordAsync(string word, CancellationToken cancellationToken = default);

    void Stop();
}

public interface IWordSpeechService : IWordLevelSpeechService { }

public interface ISpeechService : IWordLevelSpeechService { }

public interface IReadAloudService : IWordLevelSpeechService { }

public interface ITextToSpeechService : IWordLevelSpeechService { }

public interface IWordLevelSpeech : IWordLevelSpeechService { }

public enum TrailerPlaybackState
{
    Idle,
    Loading,
    Ready,
    Launched,
    NotFound,
    MissingConfiguration,
    Failed,
    LaunchFailed,

    Failure = Failed,
    MissingConfig = MissingConfiguration,
    Success = Launched,
    Found = Launched,
}

public sealed class TrailerPlaybackResult
{
    public TrailerPlaybackResult(
        TrailerPlaybackState state,
        TrailerLookupResult? lookup = null,
        string? youtubeKey = null,
        Exception? error = null
    )
    {
        State = state;
        Lookup = lookup;
        YouTubeKey = youtubeKey;
        Error = error;
    }

    public TrailerPlaybackState State { get; }

    public TrailerPlaybackState Status => State;

    public TrailerLookupResult? Lookup { get; }

    public string? YouTubeKey { get; }

    public string? YoutubeKey => YouTubeKey;

    public Exception? Error { get; }

    public bool Succeeded => State == TrailerPlaybackState.Launched;

    public bool IsSuccess => Succeeded;
}

/// <summary>
/// Presents an already-selected YouTube trailer outside the ViewModels assembly.
/// The platform implementation decides how the in-app player is displayed.
/// Returning false means the player could not be presented.
/// </summary>
public interface IExternalTrailerLauncher
{
    Task<bool> LaunchAsync(string youtubeKey, CancellationToken cancellationToken = default);

    Task<bool> LaunchYouTubeAsync(
        string youtubeKey,
        CancellationToken cancellationToken = default
    ) => LaunchAsync(youtubeKey, cancellationToken);

    Task<bool> LaunchTrailerAsync(
        string youtubeKey,
        CancellationToken cancellationToken = default
    ) => LaunchAsync(youtubeKey, cancellationToken);
}

public interface ITrailerLauncher : IExternalTrailerLauncher { }

public interface IYouTubeTrailerLauncher : IExternalTrailerLauncher { }

public interface IExternalLinkLauncher : IExternalTrailerLauncher { }

public interface IExternalLauncher : IExternalTrailerLauncher { }

public interface IExternalTrailerService : IExternalTrailerLauncher { }

public sealed class FavoriteChangedEventArgs : EventArgs
{
    public FavoriteChangedEventArgs(int movieId, bool isFavorite, FavoriteEntry entry)
    {
        MovieId = movieId;
        IsFavorite = isFavorite;
        Entry = entry;
    }

    public int MovieId { get; }

    public bool IsFavorite { get; }

    public FavoriteEntry Entry { get; }
}
