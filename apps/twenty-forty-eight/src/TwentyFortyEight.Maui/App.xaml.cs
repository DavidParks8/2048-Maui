using Microsoft.Extensions.Logging;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui;

public partial class App : Application
{
    public IServiceProvider Services { get; }

    private readonly ISocialGamingService _socialGamingService;
    private readonly ILogger<App> _logger;

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Social gaming authentication failed"
    )]
    private static partial void SocialGamingAuthenticationFailed(
        ILogger logger,
        Exception exception
    );

    public App(
        IServiceProvider services,
        ISocialGamingService socialGamingService,
        ILogger<App> logger
    )
    {
        InitializeComponent();

        Services = services;
        _socialGamingService = socialGamingService;
        _logger = logger;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = new(new AppShell())
        {
            MinimumWidth = 360, // Min board (280) + padding (40) + margins (40)
            MinimumHeight = 700, // Ensures full UI visibility with adequate margins
        };

        window.Stopped += OnWindowStopped;
        window.Destroying += OnWindowDestroying;

        // Authenticate with social gaming service on app startup (fire and forget)
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await _socialGamingService.AuthenticateAsync();
            }
            catch (Exception ex)
            {
                SocialGamingAuthenticationFailed(_logger, ex);
            }
        });

        return window;
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            MainThread.BeginInvokeOnMainThread(() => _ = FlushGameAsync(window));
        }
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            MainThread.BeginInvokeOnMainThread(() => _ = FlushGameAsync(window));
        }
    }

    private async Task FlushGameAsync(Window window)
    {
        try
        {
            if (window.Page is Shell shell && shell.CurrentPage?.BindingContext is GameViewModel vm)
            {
                await vm.FlushPendingSavesAsync();
            }
        }
        catch (Exception ex)
        {
            LogFlushGameFailed(_logger, ex);
        }
    }

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Failed to flush game state")]
    private static partial void LogFlushGameFailed(ILogger logger, Exception ex);
}
