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
        if (
            !YouTubeKidsTrailerUri.TryCreate(youtubeKey, out Uri kidsUri)
            || !YouTubeTrailerUri.TryCreate(youtubeKey, out Uri youtubeUri)
        )
        {
            return false;
        }

        foreach (Uri uri in new[] { kidsUri, youtubeUri })
        {
            if (!await _uriLauncher.CanOpenAsync(uri, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            if (await _uriLauncher.OpenAsync(uri, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }
}
