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
        Window window = new(new AppShell())
        {
            MinimumWidth = 360, // Min board (280) + padding (40) + margins (40)
            MinimumHeight = 700, // Ensures full UI visibility with adequate margins
        };

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
