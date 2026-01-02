using Microsoft.Extensions.Logging;
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
}
