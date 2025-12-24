using Microsoft.Extensions.Logging;
using TwentyFortyEight.Maui.Services;

namespace TwentyFortyEight.Maui;

public partial class App : Application
{
    private readonly IGameCenterService _gameCenterService;
    private readonly ILogger<App> _logger;

    public App(IGameCenterService gameCenterService, ILogger<App> logger)
    {
        InitializeComponent();
        _gameCenterService = gameCenterService;
        _logger = logger;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Authenticate with Game Center on app startup (fire and forget)
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await _gameCenterService.AuthenticateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Game Center authentication failed");
            }
        });

        return window;
    }
}
