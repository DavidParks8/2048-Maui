namespace TwentyFortyEight.Core;

/// <summary>
/// Default factory for creating <see cref="Game2048Engine"/> instances.
/// </summary>
public sealed class Game2048EngineFactory(
    IRandomSource randomSource,
    IStatisticsTracker statisticsTracker,
    IBoardSimulator boardSimulator
) : IGame2048EngineFactory
{
    public Game2048Engine Create(GameConfig config)
    {
        return new Game2048Engine(config, randomSource, statisticsTracker, boardSimulator);
    }

    public Game2048Engine Create(GameSave save, GameConfig config)
    {
        return new Game2048Engine(save, config, randomSource, statisticsTracker, boardSimulator);
    }
}
