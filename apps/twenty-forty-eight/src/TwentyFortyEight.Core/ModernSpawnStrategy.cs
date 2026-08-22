namespace TwentyFortyEight.Core;

/// <summary>
/// Modern/adaptive spawn rules that scale up spawn values as the game progresses.
/// </summary>
public sealed class ModernSpawnStrategy : ISpawnStrategy
{
    /// <summary>
    /// Probability of spawning the common (lower) tile value vs the rare (higher) value.
    /// </summary>
    private const double CommonSpawnProbability = 0.9;

    /// <summary>
    /// Threshold for tier 5 spawning: when max tile >= 2^17 (131072), spawn 32/64.
    /// </summary>
    private const int SpawnTier5Threshold = 131072;

    /// <summary>
    /// Threshold for tier 4 spawning: when max tile >= 2^15 (32768), spawn 16/32.
    /// </summary>
    private const int SpawnTier4Threshold = 32768;

    /// <summary>
    /// Threshold for tier 3 spawning: when max tile >= 2^13 (8192), spawn 8/16.
    /// </summary>
    private const int SpawnTier3Threshold = 8192;

    /// <summary>
    /// Threshold for tier 2 spawning: when max tile >= 2^11 (2048), spawn 4/8.
    /// </summary>
    private const int SpawnTier2Threshold = 2048;

    private readonly IRandomSource _random;

    public ModernSpawnStrategy(IRandomSource random)
    {
        _random = random;
    }

    public int GetSpawnValue(GameState state, GameConfig config)
    {
        _ = config;

        var (commonValue, rareValue) = GetSpawnValues(state.MaxTileValue);
        return _random.NextDouble() < CommonSpawnProbability ? commonValue : rareValue;
    }

    /// <summary>
    /// Gets the spawn values based on the current maximum tile on the board.
    /// Spawn values scale up as the game progresses to keep the game playable at high scores.
    /// </summary>
    private static (int commonValue, int rareValue) GetSpawnValues(int maxTileOnBoard) =>
        maxTileOnBoard switch
        {
            >= SpawnTier5Threshold => (32, 64), // 2^17: spawn 32 (90%) or 64 (10%)
            >= SpawnTier4Threshold => (16, 32), // 2^15: spawn 16 (90%) or 32 (10%)
            >= SpawnTier3Threshold => (8, 16), // 2^13: spawn 8 (90%) or 16 (10%)
            >= SpawnTier2Threshold => (4, 8), // 2^11: spawn 4 (90%) or 8 (10%)
            _ => (2, 4), // Default: spawn 2 (90%) or 4 (10%)
        };
}
