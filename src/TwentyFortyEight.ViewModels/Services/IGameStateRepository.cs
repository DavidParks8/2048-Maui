using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Handles persistence of game state and scores.
/// Abstracts storage mechanism from the ViewModel.
/// </summary>
public interface IGameStateRepository
{
    /// <summary>
    /// Loads the saved game state, if one exists.
    /// </summary>
    /// <param name="config">The game configuration.</param>
    /// <returns>The saved game state, or null if no save exists or loading failed.</returns>
    GameState? LoadGameState(GameConfig config);

    /// <summary>
    /// Saves the current game state.
    /// </summary>
    /// <param name="config">The game configuration.</param>
    /// <param name="state">The state to save.</param>
    void SaveGameState(GameConfig config, GameState state);

    /// <summary>
    /// Clears the saved game state for the specified ruleset.
    /// </summary>
    /// <param name="config">The game configuration.</param>
    void ClearSavedGame(GameConfig config);

    /// <summary>
    /// Gets the all-time best score.
    /// </summary>
    /// <param name="config">The game configuration.</param>
    int GetBestScore(GameConfig config);

    /// <summary>
    /// Updates the best score if the new score is higher.
    /// Implements debouncing internally to avoid storage thrashing.
    /// </summary>
    /// <param name="config">The game configuration.</param>
    /// <param name="score">The new score to potentially save.</param>
    void UpdateBestScoreIfHigher(GameConfig config, int score);

    /// <summary>
    /// Waits for any pending save operations to complete.
    /// Useful for testing.
    /// </summary>
    /// <param name="config">The game configuration.</param>
    Task FlushAsync(GameConfig config);
}
