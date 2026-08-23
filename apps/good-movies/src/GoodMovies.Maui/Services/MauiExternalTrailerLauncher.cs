using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui.Services;

public interface ITrailerPlaybackController
{
    void Stop();
}

public interface ITrailerPlaybackHost
{
    event EventHandler? PlaybackEnded;

    Task<bool> PlayAsync(string youtubeKey, CancellationToken cancellationToken);

    void Stop();
}

public sealed class TrailerPlaybackChangedEventArgs(string? youtubeKey, bool isPlaying) : EventArgs
{
    public string? YouTubeKey { get; } = youtubeKey;

    public bool IsPlaying { get; } = isPlaying;
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
    private string? _activeYoutubeKey;

    public event EventHandler<TrailerPlaybackChangedEventArgs>? PlaybackChanged;

    public string? ActiveYoutubeKey
    {
        get
        {
            lock (_sync)
            {
                return _activeYoutubeKey;
            }
        }
    }

    public bool IsPlaying(string? youtubeKey)
    {
        lock (_sync)
        {
            return !string.IsNullOrWhiteSpace(youtubeKey)
                && string.Equals(_activeYoutubeKey, youtubeKey, StringComparison.Ordinal);
        }
    }

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

        if (IsPlaying(youtubeKey))
        {
            return true;
        }

        Task<bool> presentation = MainThread.InvokeOnMainThreadAsync(() =>
            host.PlayAsync(youtubeKey, cancellationToken)
        );
        bool launched = await presentation.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (launched)
        {
            SetActiveYoutubeKey(youtubeKey);
        }

        return launched;
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
        SetActiveYoutubeKey(null);
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
            previous.PlaybackEnded -= OnHostPlaybackEnded;
            previous.Stop();
        }

        host.PlaybackEnded -= OnHostPlaybackEnded;
        host.PlaybackEnded += OnHostPlaybackEnded;
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
                host.PlaybackEnded -= OnHostPlaybackEnded;
                removed = true;
            }
        }

        if (removed)
        {
            host.Stop();
            SetActiveYoutubeKey(null);
        }
    }

    private void OnHostPlaybackEnded(object? sender, EventArgs e) => SetActiveYoutubeKey(null);

    private void SetActiveYoutubeKey(string? youtubeKey)
    {
        bool changed;
        lock (_sync)
        {
            changed = !string.Equals(_activeYoutubeKey, youtubeKey, StringComparison.Ordinal);
            _activeYoutubeKey = youtubeKey;
        }

        if (!changed)
        {
            return;
        }

        TrailerPlaybackChangedEventArgs args = new(youtubeKey, youtubeKey is not null);
        if (MainThread.IsMainThread)
        {
            PlaybackChanged?.Invoke(this, args);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => PlaybackChanged?.Invoke(this, args));
    }
}
