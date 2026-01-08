using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core;

/// <summary>
/// Factory for creating configured <see cref="Game2048Engine"/> instances.
/// </summary>
public interface IGame2048EngineFactory
{
    /// <summary>
    /// Creates a new engine for the provided ruleset.
    /// </summary>
    Game2048Engine Create(GameConfig config);

    /// <summary>
    /// Creates an engine from a previously saved session (including undo history) for the provided ruleset.
    /// </summary>
    Game2048Engine Create(GameSave save, GameConfig config);
}
