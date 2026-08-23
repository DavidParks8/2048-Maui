using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui.Services;

public sealed class MauiTrailerLauncher : ITrailerLauncher
{
    private readonly object _sync = new();
    private readonly TrailerPlaybackStateCoordinator _playbackState = new();
    private ITrailerPlaybackHost? _activeHost;

    public MauiTrailerLauncher()
    {
        _playbackState.ActiveYoutubeKeyChanged += PublishPlaybackChanged;
    }

    internal event EventHandler<TrailerPlaybackChangedEventArgs>? PlaybackChanged;

    internal string? ActiveYoutubeKey => _playbackState.ActiveYoutubeKey;

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

        TrailerPlaybackStateCoordinator.LaunchOperation operation = _playbackState.BeginLaunch(
            youtubeKey
        );
        if (operation.WasAlreadyPlaying)
        {
            return true;
        }

        lock (_sync)
        {
            if (!ReferenceEquals(host, _activeHost))
            {
                _playbackState.CancelLaunch(operation);
                return false;
            }
        }

        try
        {
            Task<bool> presentation = MainThread.InvokeOnMainThreadAsync(() =>
                host.PlayAsync(youtubeKey, cancellationToken)
            );
            bool launched = await presentation.WaitAsync(cancellationToken).ConfigureAwait(false);
            return _playbackState.CompleteLaunch(operation, launched);
        }
        catch
        {
            if (_playbackState.CancelLaunch(operation))
            {
                StopHost(host);
            }

            throw;
        }
    }

    public void Stop()
    {
        ITrailerPlaybackHost? host;
        lock (_sync)
        {
            host = _activeHost;
        }

        _playbackState.Reset();
        if (host is not null)
        {
            StopHost(host);
        }
    }

    internal void AttachHost(ITrailerPlaybackHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        ITrailerPlaybackHost? previous;
        lock (_sync)
        {
            previous = _activeHost;
            if (ReferenceEquals(previous, host))
            {
                return;
            }

            _activeHost = host;
        }

        _playbackState.Reset();
        if (previous is not null)
        {
            previous.PlaybackEnded -= OnHostPlaybackEnded;
            StopHost(previous);
        }

        host.PlaybackEnded += OnHostPlaybackEnded;
    }

    internal void DetachHost(ITrailerPlaybackHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        lock (_sync)
        {
            if (!ReferenceEquals(_activeHost, host))
            {
                return;
            }

            _activeHost = null;
            host.PlaybackEnded -= OnHostPlaybackEnded;
        }

        _playbackState.Reset();
    }

    private void OnHostPlaybackEnded(object? sender, EventArgs e)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(sender, _activeHost))
            {
                return;
            }

            _playbackState.Reset();
        }
    }

    private static void StopHost(ITrailerPlaybackHost host)
    {
        if (MainThread.IsMainThread)
        {
            host.Stop();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(host.Stop);
        }
    }

    private void PublishPlaybackChanged(string? youtubeKey)
    {
        void PublishCurrentState()
        {
            if (
                string.Equals(_playbackState.ActiveYoutubeKey, youtubeKey, StringComparison.Ordinal)
            )
            {
                PlaybackChanged?.Invoke(this, new TrailerPlaybackChangedEventArgs(youtubeKey));
            }
        }

        if (MainThread.IsMainThread)
        {
            PublishCurrentState();
        }
        else
        {
            MainThread.InvokeOnMainThreadAsync(PublishCurrentState).GetAwaiter().GetResult();
        }
    }
}
