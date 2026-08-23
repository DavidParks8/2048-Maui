namespace GoodMovies.Maui.Services;

internal sealed class TrailerPlaybackChangedEventArgs(string? youtubeKey) : EventArgs
{
    public string? YouTubeKey { get; } = youtubeKey;
}
