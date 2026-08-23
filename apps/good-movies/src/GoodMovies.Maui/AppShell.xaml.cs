using CoreGraphics;
using GoodMovies.Maui.Controls;
using GoodMovies.Maui.Services;
using GoodMovies.ViewModels;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace GoodMovies.Maui;

public partial class AppShell : Shell, ITrailerPlaybackHost
{
    private readonly MauiTrailerLauncher _trailerLauncher;
    private TrailerPlayerView? _trailerPlayer;
    private IElementHandler? _trailerHandler;
    private TaskCompletionSource<bool>? _trailerLoadCompletion;
    private bool _isTrailerActive;
    private bool _isTornDown;

    public AppShell(
        MainPage mainPage,
        MovieDetailPage detailPage,
        MauiTrailerLauncher trailerLauncher
    )
    {
        _trailerLauncher =
            trailerLauncher ?? throw new ArgumentNullException(nameof(trailerLauncher));
        InitializeComponent();
        MainShellContent.Content = mainPage ?? throw new ArgumentNullException(nameof(mainPage));
        Routing.RegisterRoute(
            GoodMoviesRoutes.MovieDetail,
            new MauiMovieDetailRouteFactory(detailPage)
        );
        _trailerLauncher.AttachHost(this);
    }

    public event EventHandler? PlaybackEnded;

    public async Task<bool> PlayAsync(string youtubeKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_isTornDown)
        {
            return false;
        }

        if (!YouTubeTrailerUri.TryCreate(youtubeKey, out Uri? source))
        {
            return false;
        }

        if (_isTrailerActive)
        {
            StopPlayback(notifyPlaybackEnded: false);
        }

        TrailerPlayerView player = EnsureTrailerPlayer();
        TaskCompletionSource<bool> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _trailerLoadCompletion?.TrySetResult(false);
        _trailerLoadCompletion = completion;

        if (Equals(player.Source, source))
        {
            player.Reload();
        }
        else
        {
            player.Source = source;
        }

        try
        {
            return await completion.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (ReferenceEquals(_trailerLoadCompletion, completion))
            {
                _trailerLoadCompletion = null;
                Stop();
            }

            throw;
        }
    }

    public void Stop() => StopPlayback(notifyPlaybackEnded: true);

    internal void TearDown()
    {
        if (_isTornDown)
        {
            return;
        }

        _isTornDown = true;
        _trailerLauncher.DetachHost(this);
        StopPlayback(notifyPlaybackEnded: false);

        TrailerPlayerView? player = _trailerPlayer;
        IElementHandler? handler = _trailerHandler;
        if (handler?.PlatformView is UIView platformView)
        {
            platformView.RemoveFromSuperview();
        }

        if (player is not null)
        {
            player.LoadFailed -= OnTrailerLoadFailed;
            player.LoadSucceeded -= OnTrailerLoadSucceeded;
            player.PresentationEnded -= OnTrailerPresentationEnded;
        }

        try
        {
            if (handler?.PlatformView is not null)
            {
                handler.DisconnectHandler();
            }
        }
        finally
        {
            _trailerHandler = null;
            _trailerPlayer = null;
        }
    }

    private void StopPlayback(bool notifyPlaybackEnded)
    {
        bool wasActive = _isTrailerActive;
        _isTrailerActive = false;
        _trailerLoadCompletion?.TrySetResult(false);
        _trailerLoadCompletion = null;
        _trailerPlayer?.StopPlayback();
        if (wasActive && notifyPlaybackEnded)
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    private TrailerPlayerView EnsureTrailerPlayer()
    {
        ObjectDisposedException.ThrowIf(_isTornDown, this);
        if (_trailerPlayer is not null)
        {
            return _trailerPlayer;
        }

        if (
            Handler?.MauiContext is not { } mauiContext
            || Handler is not IPlatformViewHandler { ViewController.View: UIView shellView }
        )
        {
            throw new InvalidOperationException("The Good Movies window is not ready for video.");
        }

        TrailerPlayerView player = new()
        {
            AutomationId = "GoodMoviesTrailerPlayer",
            HeightRequest = 270,
            InputTransparent = true,
            Opacity = 0.001,
            WidthRequest = 480,
        };
        player.LoadFailed += OnTrailerLoadFailed;
        player.LoadSucceeded += OnTrailerLoadSucceeded;
        player.PresentationEnded += OnTrailerPresentationEnded;

        IElementHandler trailerHandler = player.ToHandler(mauiContext);
        if (trailerHandler.PlatformView is not UIView platformView)
        {
            trailerHandler.DisconnectHandler();
            throw new InvalidOperationException("The Good Movies video view is unavailable.");
        }

        platformView.Frame = new CGRect(0, 0, 480, 270);
        platformView.AccessibilityElementsHidden = true;
        platformView.Alpha = (nfloat)0.001;
        platformView.UserInteractionEnabled = false;
        shellView.AddSubview(platformView);
        _trailerHandler = trailerHandler;
        _trailerPlayer = player;
        return player;
    }

    private void OnTrailerLoadSucceeded(object? sender, EventArgs e)
    {
        _isTrailerActive = true;
        _trailerLoadCompletion?.TrySetResult(true);
        _trailerLoadCompletion = null;
    }

    private void OnTrailerLoadFailed(object? sender, EventArgs e)
    {
        _isTrailerActive = false;
        _trailerLoadCompletion?.TrySetResult(false);
        _trailerLoadCompletion = null;
        _trailerPlayer?.StopPlayback();
    }

    private void OnTrailerPresentationEnded(object? sender, EventArgs e) => Stop();
}
