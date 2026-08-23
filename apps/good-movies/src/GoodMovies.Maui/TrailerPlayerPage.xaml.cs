using GoodMovies.Maui.Resources.Strings;
using GoodMovies.Maui.Services;
using GoodMovies.ViewModels;
using Microsoft.Extensions.Logging;

namespace GoodMovies.Maui;

public partial class TrailerPlayerPage : ContentPage, IQueryAttributable
{
    internal const string YouTubeKeyQueryParameter = "youtubeKey";

    private readonly ILogger _logger;
    private readonly IScreenReaderService _screenReaderService;
    private int _closeRequested;
    private bool _released;

    public TrailerPlayerPage(ILogger logger, IScreenReaderService screenReaderService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _screenReaderService =
            screenReaderService ?? throw new ArgumentNullException(nameof(screenReaderService));
        InitializeComponent();
        DoneButton.Command = new Command(() => _ = CloseAsync(animated: true));
    }

    public event EventHandler? Closed;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        string? youtubeKey = query.TryGetValue(YouTubeKeyQueryParameter, out object? value)
            ? value as string
            : null;
        TrailerWebView.Source = YouTubeTrailerUri.TryCreate(youtubeKey, out Uri embedUri)
            ? embedUri
            : null;
    }

    protected override void OnDisappearing()
    {
        ReleasePlayer();
        base.OnDisappearing();
    }

    internal void CloseForLifecycle()
    {
        TrailerWebView.StopPlayback();
        _ = CloseAsync(animated: false);
    }

    internal void ReleasePlayer()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        TrailerWebView.StopPlayback();
        TrailerWebView.Handler?.DisconnectHandler();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        LoadingOverlay.IsVisible = true;
        ErrorOverlay.IsVisible = false;
        TrailerWebView.Reload();
    }

    private void OnPlayerLoadStarted(object? sender, EventArgs e)
    {
        LoadingOverlay.IsVisible = true;
        ErrorOverlay.IsVisible = false;
    }

    private void OnPlayerLoadSucceeded(object? sender, EventArgs e)
    {
        LoadingOverlay.IsVisible = false;
        ErrorOverlay.IsVisible = false;
    }

    private void OnPlayerLoadFailed(object? sender, EventArgs e)
    {
        LoadingOverlay.IsVisible = false;
        ErrorOverlay.IsVisible = true;
        _screenReaderService.Announce(
            $"{AppStrings.TrailerPlayerErrorTitle}. {AppStrings.TrailerPlayerErrorMessage}"
        );
    }

    private async Task CloseAsync(bool animated)
    {
        if (Interlocked.Exchange(ref _closeRequested, 1) != 0)
        {
            return;
        }

        try
        {
            Shell shell =
                Shell.Current
                ?? throw new InvalidOperationException("The Good Movies Shell is not available.");
            if (!ReferenceEquals(shell.CurrentPage, this))
            {
                Interlocked.Exchange(ref _closeRequested, 0);
                return;
            }

            await shell.GoToAsync("..", animated);
        }
        catch (Exception exception)
        {
            Interlocked.Exchange(ref _closeRequested, 0);
            _logger.LogWarning(exception, "The trailer player could not be dismissed.");
        }
    }
}
