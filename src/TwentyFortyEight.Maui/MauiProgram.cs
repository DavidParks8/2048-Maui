using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Components;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;
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
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(_ => { })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<BottomBar, BottomBarHandler>();
                handlers.AddHandler<Switch, CustomSwitchHandler>();
#endif
#if WINDOWS
                handlers.AddHandler<BottomBar, BottomBarHandler>();
#endif
            });

        // Register services for dependency injection
        builder.Services.AddSingleton<IRandomSource, SystemRandomSource>();
        builder.Services.AddSingleton<IMoveAnalyzer, MoveAnalyzer>();
        builder.Services.AddSingleton<IBoardSimulator, BoardMoveSimulator>();
        builder.Services.AddSingleton<IMoveAdvisor, HeuristicMoveAdvisor>();
        builder.Services.AddSingleton<ISettingsService, MauiSettingsService>();
        builder.Services.AddSingleton<IStatisticsTracker, StatisticsService>();
        builder.Services.AddSingleton<IGame2048EngineFactory, Game2048EngineFactory>();
        builder.Services.AddSingleton<IToolbarIconService, ToolbarIconService>();
        builder.Services.AddSingleton<IWindowOverlayService, WindowOverlayService>();
        builder.Services.AddSingleton<IWallOverlayRenderer, WallOverlayRenderer>();

        // Register consolidated services (from refactoring)
        builder.Services.AddSingleton<IUserFeedbackService, UserFeedbackService>();
        builder.Services.AddSingleton<IGameStateRepository, GameStateRepository>();
        builder.Services.AddSingleton<IGameSessionCoordinator, GameSessionCoordinator>();
        builder.Services.AddSingleton<IBoardSizeChangeService, BoardSizeChangeService>();
        builder.Services.AddSingleton<ICoachNudgeService, CoachNudgeService>();
        builder.Services.AddSingleton<ICoachSuggestionService, CoachSuggestionService>();

        // Register low-level services (used by consolidated services internally)
        builder.Services.AddSingleton<IHapticService, MauiHapticService>();
        builder.Services.AddSingleton<IPreferencesService, MauiPreferencesService>();
        builder.Services.AddSingleton<IAlertService, MauiAlertService>();
        builder.Services.AddSingleton<ILocalizationService, MauiLocalizationService>();
        builder.Services.AddSingleton<IScreenReaderService, MauiScreenReaderService>();

        // Accessibility and feedback services
        builder.Services.AddSingleton<IReduceMotionService, ReduceMotionService>();

        // Input and gesture services
        builder.Services.AddSingleton<IInputCoordinationService, InputCoordinationService>();
        builder.Services.AddSingleton<IGestureRecognizerService, GestureRecognizerService>();

        // Swipe preview interaction orchestration (page-level)
        builder.Services.AddTransient<
            ISwipePreviewInteractionService,
            SwipePreviewInteractionService
        >();

        // Victory ViewModel and animation service
        builder.Services.AddSingleton<VictoryViewModel>();

        builder.Services.AddSingleton<TileAnimationService>();
        builder.Services.AddSingleton<BoardRippleService>();

        // Register achievement tracker
        builder.Services.AddSingleton<IAchievementTracker, AchievementTracker>();

        // Register achievement ID mapper - uses partial class pattern for platform-specific IDs
        builder.Services.AddSingleton<IAchievementIdMapper, AchievementIdMapper>();

        // Register social gaming service - uses partial class pattern
        // Platform-specific implementations are in Platforms/iOS, Platforms/Windows, etc.
        builder.Services.AddSingleton<ISocialGamingService, SocialGamingService>();

#if IOS || MACCATALYST
        // Visual features (handler mapper extensions)
        builder.Services.AddSingleton<ILiquidGlassApplier, LiquidGlassApplier>();
        builder.Services.AddSingleton<IMauiInitializeService, LiquidGlassInitializer>();
#endif

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
