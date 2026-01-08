namespace TwentyFortyEight.Core;

/// <summary>
/// Strategy for selecting the value of a newly spawned tile.
/// </summary>
public interface ISpawnStrategy
{
    /// <summary>
    /// Gets the value for the next spawned tile.
    /// </summary>
    int GetSpawnValue(GameState state, GameConfig config);
}
