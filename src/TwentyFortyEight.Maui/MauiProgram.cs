using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Controls;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;
#if IOS
using TwentyFortyEight.Maui.Platforms.iOS.Handlers;
#endif
#if ANDROID
using TwentyFortyEight.Maui.Platforms.Android.Handlers;
#endif
#if WINDOWS
using TwentyFortyEight.Maui.Platforms.Windows.Handlers;
#endif

namespace TwentyFortyEight.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseMauiCommunityToolkit().UseSkiaSharp().ConfigureFonts(_ => { });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register custom handlers
        builder.ConfigureMauiHandlers(handlers =>
        {
#if IOS
            handlers.AddHandler(typeof(BottomBar), typeof(BottomBarHandler));
#endif
#if ANDROID
            handlers.AddHandler(typeof(BottomBar), typeof(BottomBarHandler));
#endif
#if WINDOWS
            handlers.AddHandler(typeof(BottomBar), typeof(BottomBarHandler));
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register services for dependency injection
        builder.Services.AddSingleton<IRandomSource, SystemRandomSource>();
        builder.Services.AddSingleton<IMoveAnalyzer, MoveAnalyzer>();
        builder.Services.AddSingleton<ISettingsService, MauiSettingsService>();
        builder.Services.AddSingleton<IStatisticsTracker, StatisticsService>();
        builder.Services.AddSingleton<IToolbarIconService, ToolbarIconService>();

        // Register consolidated services (from refactoring)
        builder.Services.AddSingleton<IUserFeedbackService, UserFeedbackService>();
        builder.Services.AddSingleton<IGameStateRepository, GameStateRepository>();
        builder.Services.AddSingleton<IGameSessionCoordinator, GameSessionCoordinator>();

        // Register low-level services (used by consolidated services internally)
        builder.Services.AddSingleton<IHapticService, MauiHapticService>();
        builder.Services.AddSingleton<IPreferencesService, MauiPreferencesService>();
        builder.Services.AddSingleton<IAlertService, MauiAlertService>();
        builder.Services.AddSingleton<ILocalizationService, MauiLocalizationService>();
        builder.Services.AddSingleton<IScreenReaderService, MauiScreenReaderService>();
        builder.Services.AddSingleton<TileAnimationService>();
        builder.Services.AddSingleton<BoardRippleService>();

        // Register achievement tracker
        builder.Services.AddSingleton<IAchievementTracker, AchievementTracker>();

        // Register achievement ID mapper - uses partial class pattern for platform-specific IDs
        builder.Services.AddSingleton<
            TwentyFortyEight.ViewModels.Services.IAchievementIdMapper,
            AchievementIdMapper
        >();

        // Register social gaming service - uses partial class pattern
        // Platform-specific implementations are in Platforms/iOS, Platforms/Windows, etc.
        builder.Services.AddSingleton<ISocialGamingService, SocialGamingService>();

        builder.Services.AddSingleton<GameViewModel>();
        builder.Services.AddTransient<StatsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<StatsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AboutPage>();

        return builder.Build();
    }
}
