namespace TwentyFortyEight.Core;

internal sealed class SpawnStrategyFactory(
    ClassicSpawnStrategy classicSpawnStrategy,
    ModernSpawnStrategy modernSpawnStrategy
) : ISpawnStrategyFactory
{
    public ISpawnStrategy Create(GameConfig config)
    {
        return config.Mode switch
        {
            GameMode.Classic => classicSpawnStrategy,
            _ => modernSpawnStrategy,
        };
    }
}
