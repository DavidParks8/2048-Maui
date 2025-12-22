namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for Game Center integration on iOS.
/// Provides leaderboard and achievement functionality.
/// </summary>
public interface IGameCenterService
{
    /// <summary>
    /// Authenticates the player with Game Center.
    /// Should be called once at app startup.
    /// </summary>
    Task AuthenticateAsync();

    /// <summary>
    /// Gets whether Game Center is available and the player is authenticated.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Submits a score to the high scores leaderboard.
    /// </summary>
    /// <param name="score">The score to submit.</param>
    Task SubmitScoreAsync(long score);

    /// <summary>
    /// Reports progress for an achievement.
    /// </summary>
    /// <param name="achievementId">The achievement identifier.</param>
    /// <param name="percentComplete">Progress percentage (0-100).</param>
    Task ReportAchievementAsync(string achievementId, double percentComplete);

    /// <summary>
    /// Shows the Game Center leaderboard UI.
    /// </summary>
    Task ShowLeaderboardAsync();

    /// <summary>
    /// Shows the Game Center achievements UI.
    /// </summary>
    Task ShowAchievementsAsync();
}
