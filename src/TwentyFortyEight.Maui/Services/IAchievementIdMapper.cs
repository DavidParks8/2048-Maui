namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service to map achievement milestones to platform-specific achievement IDs.
/// Platform-specific implementations should be in their respective platform folders.
/// </summary>
public interface IAchievementIdMapper
{
    /// <summary>
    /// Gets the achievement ID for a tile milestone.
    /// </summary>
    /// <param name="tileValue">The tile value (128, 256, 512, 1024, 2048, 4096).</param>
    /// <returns>The platform-specific achievement ID, or null if not supported.</returns>
    string? GetTileAchievementId(int tileValue);

    /// <summary>
    /// Gets the achievement ID for the first win.
    /// </summary>
    /// <returns>The platform-specific achievement ID, or null if not supported.</returns>
    string? GetFirstWinAchievementId();

    /// <summary>
    /// Gets the achievement ID for a score milestone.
    /// </summary>
    /// <param name="score">The score milestone (10000, 25000, 50000, 100000).</param>
    /// <returns>The platform-specific achievement ID, or null if not supported.</returns>
    string? GetScoreAchievementId(int score);
}
