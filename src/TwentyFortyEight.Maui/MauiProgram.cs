using Microsoft.Extensions.Logging;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.Maui.ViewModels;

namespace TwentyFortyEight.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register services for dependency injection
#if IOS
        builder.Services.AddSingleton<IGameCenterService, Services.GameCenterService>();
#else
        builder.Services.AddSingleton<IGameCenterService, GameCenterServiceStub>();
#endif
        builder.Services.AddSingleton<GameViewModel>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
