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
    /// <returns>The saved game state, or null if no save exists or loading failed.</returns>
    GameState? LoadGameState();

    /// <summary>
    /// Saves the current game state.
    /// </summary>
    /// <param name="state">The state to save.</param>
    void SaveGameState(GameState state);

    /// <summary>
    /// Gets the all-time best score.
    /// </summary>
    int GetBestScore();

    /// <summary>
    /// Updates the best score if the new score is higher.
    /// Implements debouncing internally to avoid storage thrashing.
    /// </summary>
    /// <param name="score">The new score to potentially save.</param>
    void UpdateBestScoreIfHigher(int score);

    /// <summary>
    /// Waits for any pending save operations to complete.
    /// Useful for testing.
    /// </summary>
    Task FlushAsync();
}
