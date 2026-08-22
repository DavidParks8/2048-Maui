namespace GoodMovies.ViewModels;

public interface INativeUriLauncher
{
    Task<bool> CanOpenAsync(Uri uri, CancellationToken cancellationToken = default);

    Task<bool> OpenAsync(Uri uri, CancellationToken cancellationToken = default);
}

public sealed class NativeYouTubeTrailerLauncher : IExternalTrailerLauncher
{
    private readonly INativeUriLauncher _uriLauncher;

    public NativeYouTubeTrailerLauncher(INativeUriLauncher uriLauncher)
    {
        _uriLauncher = uriLauncher ?? throw new ArgumentNullException(nameof(uriLauncher));
    }

    public async Task<bool> LaunchAsync(
        string youtubeKey,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!YouTubeTrailerUri.TryCreate(youtubeKey, out Uri uri))
        {
            return false;
        }

        if (!await _uriLauncher.CanOpenAsync(uri, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        return await _uriLauncher.OpenAsync(uri, cancellationToken).ConfigureAwait(false);
    }
}
