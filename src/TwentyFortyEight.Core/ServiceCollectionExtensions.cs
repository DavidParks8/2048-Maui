using Microsoft.Extensions.DependencyInjection;

namespace TwentyFortyEight.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTwentyFortyEightCore(this IServiceCollection services)
    {
        services.AddSingleton<IRandomSource, SystemRandomSource>();
        services.AddSingleton<IMoveAnalyzer, MoveAnalyzer>();
        services.AddSingleton<IBoardSimulator, BoardMoveSimulator>();
        services.AddSingleton<IMoveAdvisor, HeuristicMoveAdvisor>();
        services.AddSingleton<IGame2048EngineFactory, Game2048EngineFactory>();
        services.AddSingleton<IAchievementTracker, AchievementTracker>();

        return services;
    }
}
