namespace GoodMovies.Maui.Services;

internal interface ITrailerPlaybackHost
{
    event EventHandler? PlaybackEnded;

    Task<bool> PlayAsync(string youtubeKey, CancellationToken cancellationToken);

    void Stop();
}
