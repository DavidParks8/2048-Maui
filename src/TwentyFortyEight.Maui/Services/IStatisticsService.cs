using TwentyFortyEight.Maui.Models;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service interface for tracking and retrieving game statistics.
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Gets the current statistics.
    /// </summary>
    GameStatistics GetStatistics();

    /// <summary>
    /// Increments the games played counter.
    /// </summary>
    void IncrementGamesPlayed();

    /// <summary>
    /// Increments the games won counter and updates streaks.
    /// </summary>
    void IncrementGamesWon();

    /// <summary>
    /// Records a game loss (resets current streak).
    /// </summary>
    void RecordGameLoss();

    /// <summary>
    /// Updates the best score if the provided score is higher.
    /// </summary>
    void UpdateBestScore(int score);

    /// <summary>
    /// Adds a score to the total for calculating average.
    /// </summary>
    void AddScore(int score);

    /// <summary>
    /// Updates the highest tile if the provided tile is higher.
    /// </summary>
    void UpdateHighestTile(int tile);

    /// <summary>
    /// Adds moves to the total move count.
    /// </summary>
    void AddMoves(int moves);

    /// <summary>
    /// Adds time played in seconds.
    /// </summary>
    void AddTimePlayed(long seconds);

    /// <summary>
    /// Starts tracking time for the current game session.
    /// </summary>
    void StartTimeTracking();

    /// <summary>
    /// Stops tracking time for the current game session and persists the accumulated time.
    /// </summary>
    void StopTimeTracking();

    /// <summary>
    /// Resets all statistics to zero.
    /// </summary>
    void ResetStatistics();
}
