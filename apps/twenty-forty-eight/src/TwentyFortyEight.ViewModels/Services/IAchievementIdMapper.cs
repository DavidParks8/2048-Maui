namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Service to map achievement milestones to platform-specific achievement IDs.
/// </summary>
public interface IAchievementIdMapper
{
    /// <summary>
    /// Gets the achievement ID for a tile milestone.
    /// </summary>
    string? GetTileAchievementId(int tileValue);

    /// <summary>
    /// Gets the achievement ID for the first win.
    /// </summary>
    string? GetFirstWinAchievementId();

    /// <summary>
    /// Gets the achievement ID for a score milestone.
    /// </summary>
    string? GetScoreAchievementId(int score);
}
