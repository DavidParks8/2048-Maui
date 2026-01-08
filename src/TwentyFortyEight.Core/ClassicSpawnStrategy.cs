namespace TwentyFortyEight.Core;

/// <summary>
/// Original 2048 spawn rules: 2 (90%) or 4 (10%).
/// </summary>
public sealed class ClassicSpawnStrategy(IRandomSource random) : ISpawnStrategy
{
    private const double CommonSpawnProbability = 0.9;

    public int GetSpawnValue(GameState state, GameConfig config)
    {
        return random.NextDouble() < CommonSpawnProbability ? 2 : 4;
    }
}
