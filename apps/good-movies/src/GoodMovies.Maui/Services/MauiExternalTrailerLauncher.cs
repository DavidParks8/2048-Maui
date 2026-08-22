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
    private readonly NativeYouTubeTrailerLauncher _launcher;

    public MauiExternalTrailerLauncher()
        : this(new MauiNativeUriLauncher()) { }

    public MauiExternalTrailerLauncher(INativeUriLauncher uriLauncher)
    {
        _launcher = new NativeYouTubeTrailerLauncher(uriLauncher);
    }

    public Task<bool> LaunchAsync(
        string youtubeKey,
        CancellationToken cancellationToken = default
    ) => _launcher.LaunchAsync(youtubeKey, cancellationToken);
}

internal sealed class MauiNativeUriLauncher : INativeUriLauncher
{
    public async Task<bool> CanOpenAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Task<bool> availability = MainThread.InvokeOnMainThreadAsync(() =>
            Launcher.Default.CanOpenAsync(uri)
        );
        return await availability.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> OpenAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
