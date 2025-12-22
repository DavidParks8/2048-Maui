using TwentyFortyEight.Maui.Services;

namespace TwentyFortyEight.Maui;

public partial class App : Application
{
    private readonly IGameCenterService _gameCenterService;

    public App(IGameCenterService gameCenterService)
    {
        InitializeComponent();
        _gameCenterService = gameCenterService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        
        // Authenticate with Game Center on app startup (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _gameCenterService.AuthenticateAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Game Center authentication failed: {ex.Message}");
            }
        });
        
        return window;
    }
}
