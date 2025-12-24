namespace TwentyFortyEight.Core;

/// <summary>
/// Interface for tracking gameplay statistics.
/// </summary>
public interface IStatisticsTracker
{
    /// <summary>
    /// Gets the current statistics. Returns a snapshot copy.
    /// </summary>
    GameStatistics GetStatistics();

    /// <summary>
    /// Called when a new game is started. Increments games played.
    /// </summary>
    void OnGameStarted();

    /// <summary>
    /// Called when a move is made. Updates move count.
    /// </summary>
    void OnMoveMade();

    /// <summary>
    /// Called when the win tile (2048) is reached. Increments games won (only once per game).
    /// </summary>
    void OnGameWon();

    /// <summary>
    /// Called when a game ends (game over or new game started while in progress).
    /// Finalizes statistics for that session.
    /// </summary>
    /// <param name="finalScore">The final score for the game.</param>
    /// <param name="wasWon">Whether the game was won.</param>
    void OnGameEnded(int finalScore, bool wasWon);

    /// <summary>
    /// Updates the best score if the new score is higher.
    /// </summary>
    /// <param name="score">The score to check.</param>
    void UpdateBestScore(int score);

    /// <summary>
    /// Updates the highest tile if the new tile value is higher.
    /// </summary>
    /// <param name="tileValue">The tile value to check.</param>
    void UpdateHighestTile(int tileValue);

    /// <summary>
    /// Resets all statistics to their default values.
    /// </summary>
    void Reset();
}
