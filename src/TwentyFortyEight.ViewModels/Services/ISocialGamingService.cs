namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Platform-agnostic service for social gaming features.
/// Supports leaderboards and achievements across different platforms:
/// - iOS/macOS: Game Center
/// - Windows: Xbox Live
/// - Android: Google Play Games
/// - Other: Steam, etc.
/// </summary>
public interface ISocialGamingService
{
    /// <summary>
    /// Authenticates the player with the platform's social gaming service.
    /// Should be called once at app startup.
    /// </summary>
    Task AuthenticateAsync();

    /// <summary>
    /// Gets whether the social gaming service is available and the player is authenticated.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Submits a score to the high scores leaderboard.
    /// </summary>
    Task SubmitScoreAsync(long score);

    /// <summary>
    /// Reports progress for an achievement.
    /// </summary>
    Task ReportAchievementAsync(string achievementId, double percentComplete);

    /// <summary>
    /// Shows the platform's leaderboard UI.
    /// </summary>
    Task ShowLeaderboardAsync();

    /// <summary>
    /// Shows the platform's achievements UI.
    /// </summary>
    Task ShowAchievementsAsync();
}
