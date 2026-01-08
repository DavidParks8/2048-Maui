using Microsoft.Extensions.DependencyInjection;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTwentyFortyEightViewModels(this IServiceCollection services)
    {
        // ViewModels
        services.AddSingleton<GameViewModel>();
        services.AddSingleton<VictoryViewModel>();
        services.AddTransient<StatsViewModel>();
        services.AddTransient<SettingsViewModel>();

        // ViewModel-layer services
        services.AddSingleton<IUserFeedbackService, UserFeedbackService>();
        services.AddSingleton<IGameStateRepository, GameStateRepository>();
        services.AddSingleton<IGameSessionCoordinator, GameSessionCoordinator>();
        services.AddSingleton<IBoardSizeChangeService, BoardSizeChangeService>();
        services.AddSingleton<ICoachNudgeService, CoachNudgeService>();
        services.AddSingleton<ICoachSuggestionService, CoachSuggestionService>();

        return services;
    }
}
