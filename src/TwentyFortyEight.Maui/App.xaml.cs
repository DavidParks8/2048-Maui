using Microsoft.Extensions.Logging;
using TwentyFortyEight.Maui.Services;

namespace TwentyFortyEight.Maui;

public partial class App : Application
{
    private readonly ISocialGamingService _socialGamingService;
    private readonly ILogger<App> _logger;

    public App(ISocialGamingService socialGamingService, ILogger<App> logger)
    {
        InitializeComponent();
        _socialGamingService = socialGamingService;
        _logger = logger;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Authenticate with social gaming service on app startup (fire and forget)
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await _socialGamingService.AuthenticateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Social gaming authentication failed");
            }
        });

        return window;
    }
}
