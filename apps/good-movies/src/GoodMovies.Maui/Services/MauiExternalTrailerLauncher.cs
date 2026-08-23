using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui.Services;

public interface ITrailerPlaybackController
{
    void Stop();
}

public interface ITrailerPlaybackHost
{
    Task<bool> PlayAsync(string youtubeKey, CancellationToken cancellationToken);

    void Stop();
}

public sealed class MauiExternalTrailerLauncher
    : IExternalTrailerLauncher,
        ITrailerLauncher,
        IYouTubeTrailerLauncher,
        IExternalLinkLauncher,
        IExternalLauncher,
        IExternalTrailerService,
        ITrailerPlaybackController
{
    private readonly object _sync = new();
    private ITrailerPlaybackHost? _activeHost;

    public async Task<bool> LaunchAsync(
        string youtubeKey,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!YouTubeTrailerUri.TryCreate(youtubeKey, out _))
        {
            return false;
        }

        ITrailerPlaybackHost? host;
        lock (_sync)
        {
            host = _activeHost;
        }

        if (host is null)
        {
            return false;
        }

        Task<bool> presentation = MainThread.InvokeOnMainThreadAsync(() =>
            host.PlayAsync(youtubeKey, cancellationToken)
        );
        return await presentation.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Stop()
    {
        ITrailerPlaybackHost? host;
        lock (_sync)
        {
            host = _activeHost;
        }

        if (host is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(host.Stop);
    }

    public void AttachHost(ITrailerPlaybackHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        ITrailerPlaybackHost? previous;
        lock (_sync)
        {
            previous = _activeHost;
            _activeHost = host;
        }

        if (previous is not null && !ReferenceEquals(previous, host))
        {
            previous.Stop();
        }
    }

    public void DetachHost(ITrailerPlaybackHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        bool removed = false;
        lock (_sync)
        {
            if (ReferenceEquals(_activeHost, host))
            {
                _activeHost = null;
                removed = true;
            }
        }

        if (removed)
        {
            host.Stop();
        }
    }
}
