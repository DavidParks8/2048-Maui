using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Controls;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.Maui.Victory;
using TwentyFortyEight.Maui.Victory.Phases;
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
            handlers.AddHandler(typeof(BottomBar), typeof(BottomBarHandler));
#if ANDROID
            handlers.AddHandler<Switch, CustomSwitchHandler>();
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

        // Accessibility and feedback services
        builder.Services.AddSingleton<IReduceMotionService, ReduceMotionService>();

        // Input and gesture services
        builder.Services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();
        builder.Services.AddSingleton<IInputCoordinationService, InputCoordinationService>();
        builder.Services.AddSingleton<IGestureRecognizerService, GestureRecognizerService>();

        // Victory animation components
        builder.Services.AddSingleton<WarpLineRenderer>();
        builder.Services.AddTransient<ImpactPhaseDrawer>();
        builder.Services.AddTransient<WarpTransitionPhaseDrawer>();
        builder.Services.AddTransient<WarpSustainPhaseDrawer>();
        builder.Services.AddTransient<CinematicOverlayView>();
        builder.Services.AddTransient<VictoryModalOverlay>();

        builder.Services.AddSingleton<TileAnimationService>();
        builder.Services.AddSingleton<BoardRippleService>();

        // Victory animation orchestrator
        builder.Services.AddTransient<TwentyFortyEight.Maui.Helpers.VictoryAnimationOrchestrator>();

        // Register achievement tracker
        builder.Services.AddSingleton<IAchievementTracker, AchievementTracker>();

        // Register achievement ID mapper - uses partial class pattern for platform-specific IDs
        builder.Services.AddSingleton<IAchievementIdMapper, AchievementIdMapper>();

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
