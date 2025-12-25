namespace TwentyFortyEight.Core;

/// <summary>
/// Tracks achievements and milestones during gameplay.
/// Platform-agnostic achievement tracking that can be used with
/// Game Center (iOS), Xbox (Windows), Google Play (Android), Steam, etc.
/// </summary>
public interface IAchievementTracker
{
    /// <summary>
    /// Checks if a tile achievement should be unlocked based on the max tile value.
    /// </summary>
    /// <param name="maxTileValue">The highest tile value on the board.</param>
    /// <returns>True if a new tile achievement was unlocked.</returns>
    bool CheckTileAchievement(int maxTileValue);

    /// <summary>
    /// Checks if a score achievement should be unlocked.
    /// </summary>
    /// <param name="score">The current score.</param>
    /// <returns>True if a new score achievement was unlocked.</returns>
    bool CheckScoreAchievement(int score);

    /// <summary>
    /// Checks if the first win achievement should be unlocked.
    /// </summary>
    /// <param name="isWon">Whether the game is in a won state.</param>
    /// <returns>True if the first win achievement was just unlocked.</returns>
    bool CheckFirstWinAchievement(bool isWon);

    /// <summary>
    /// Gets the tile value for the last unlocked tile achievement, or null if none.
    /// </summary>
    int? LastUnlockedTileValue { get; }

    /// <summary>
    /// Gets the score for the last unlocked score achievement, or null if none.
    /// </summary>
    int? LastUnlockedScoreMilestone { get; }

    /// <summary>
    /// Gets whether the first win was just unlocked.
    /// </summary>
    bool FirstWinJustUnlocked { get; }

    /// <summary>
    /// Resets the tracker state for tracking what was "just unlocked".
    /// Should be called after achievements are reported to the platform.
    /// </summary>
    void ResetJustUnlocked();
}
