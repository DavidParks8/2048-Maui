using TwentyFortyEight.Core;
// Alias to avoid conflict with Apple's GameKit.GameSave namespace on iOS/Mac Catalyst.
using CoreGameSave = TwentyFortyEight.Core.GameSave;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Handles persistence of game state and scores.
/// Abstracts storage mechanism from the ViewModel.
/// </summary>
public interface IGameStateRepository
{
    /// <summary>
    /// Loads the saved game session, if one exists.
    /// </summary>
    /// <param name="config">The game configuration.</param>
    /// <returns>The saved game session, or null if no save exists or loading failed.</returns>
    CoreGameSave? LoadGame(GameConfig config);

    /// <summary>
    /// Saves the current game session.
    /// </summary>
    /// <param name="config">The game configuration.</param>
    /// <param name="save">The game session to save.</param>
    void SaveGame(GameConfig config, CoreGameSave save);

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
    ///
    /// Note: In Adversarial mode, lower score is better, so this will update when the
    /// new score is lower than the current best.
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
