namespace TwentyFortyEight.Core;

/// <summary>
/// Interface for tracking game statistics. Can be implemented by external systems
/// to receive notifications about game events.
/// </summary>
public interface IGameStatisticsTracker
{
    /// <summary>
    /// Called when a new game starts.
    /// </summary>
    void OnGameStarted();

    /// <summary>
    /// Called when a move is successfully made.
    /// </summary>
    /// <param name="currentScore">The current score after the move.</param>
    /// <param name="highestTile">The highest tile value on the board.</param>
    void OnMoveMade(int currentScore, int highestTile);

    /// <summary>
    /// Called when the player reaches the 2048 tile for the first time.
    /// </summary>
    void OnGameWon();

    /// <summary>
    /// Called when the game is over (no more valid moves).
    /// </summary>
    /// <param name="finalScore">The final score of the game.</param>
    /// <param name="wasWon">Whether the game was won (reached 2048).</param>
    void OnGameOver(int finalScore, bool wasWon);
}
