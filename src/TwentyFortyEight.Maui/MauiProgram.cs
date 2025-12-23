using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
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
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register services for dependency injection
        builder.Services.AddSingleton<IMoveAnalyzer, MoveAnalyzer>();
        builder.Services.AddSingleton<TileAnimationService>();
        builder.Services.AddSingleton<IStatisticsTracker, StatisticsService>();
        builder.Services.AddSingleton<GameViewModel>();
        builder.Services.AddTransient<StatsViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<StatsPage>();

        return builder.Build();
    }
}
