using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui.Services;

public class MauiExternalTrailerLauncher
    : IExternalTrailerLauncher,
        ITrailerLauncher,
        IYouTubeTrailerLauncher,
        IExternalLinkLauncher,
        IExternalLauncher,
        IExternalTrailerService
{
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

        Task<bool> launch = MainThread.InvokeOnMainThreadAsync(() =>
            Launcher.Default.OpenAsync(uri)
        );
        return await launch.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Compatibility name for callers that identify the implementation by its
/// YouTube-specific behavior.
/// </summary>
public sealed class YouTubeTrailerLauncher : MauiExternalTrailerLauncher { }
