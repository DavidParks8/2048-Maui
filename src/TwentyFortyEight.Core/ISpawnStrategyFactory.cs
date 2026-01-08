namespace TwentyFortyEight.Core;

public interface ISpawnStrategyFactory
{
    ISpawnStrategy Create(GameConfig config);
}
