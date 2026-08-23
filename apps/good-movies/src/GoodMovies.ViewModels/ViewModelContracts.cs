using GoodMovies.Core;

namespace GoodMovies.ViewModels;

public enum CatalogSection
{
    ComingSoon,
    MyFavorites,
    FindAMovie,
}

public enum MovieRatingFilter
{
    All,
    G,
    PG,
    RatingSoon,
}

/// <summary>
/// A semantic presentation state. Display text belongs to the localized UI layer.
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
}

public enum MovieGroupKind
{
    InTheatersNow,
    ReleaseDate,
}

public interface INavigationService
{
    Task NavigateToMovieDetailAsync(int movieId, CancellationToken cancellationToken = default);

    Task NavigateBackAsync(CancellationToken cancellationToken = default);
}

public interface INetworkStatusService
{
    bool IsInternetAvailable { get; }

    event EventHandler? NetworkStatusChanged;
}

public sealed class SpeechRangeEventArgs(int start, int length) : EventArgs
{
    public SpokenCharacterRange Range { get; } = new(Math.Max(0, start), Math.Max(0, length));
}

public readonly record struct SpokenCharacterRange(int Start, int Length)
{
    public int End => Start + Length;
}

public interface IWordLevelSpeechService
{
    event EventHandler<SpeechRangeEventArgs>? SpokenRange;

    Task SpeakAsync(string text, CancellationToken cancellationToken = default);

    Task SpeakWordAsync(string word, CancellationToken cancellationToken = default);

    void StopSpeaking();
}

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
}

public sealed class TrailerPlaybackResult
{
    public TrailerPlaybackResult(
        TrailerPlaybackState state,
        MovieTrailer? trailer = null,
        Exception? error = null
    )
    {
        State = state;
        Trailer = trailer;
        Error = error;
    }

    public TrailerPlaybackState State { get; }

    public MovieTrailer? Trailer { get; }

    public Exception? Error { get; }
}

/// <summary>
/// Presents an already-selected YouTube trailer in the platform player.
/// Returning false means the player could not be presented.
/// </summary>
public interface ITrailerLauncher
{
    Task<bool> LaunchAsync(string youtubeKey, CancellationToken cancellationToken = default);
}

public sealed class FavoriteChangedEventArgs(FavoriteEntry entry, bool isFavorite) : EventArgs
{
    public FavoriteEntry Entry { get; } = entry;

    public bool IsFavorite { get; } = isFavorite;
}
