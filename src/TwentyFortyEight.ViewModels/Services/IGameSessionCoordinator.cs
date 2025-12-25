using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Coordinates meta-game features: achievements, social gaming, statistics.
/// Reacts to game events without affecting core gameplay.
/// </summary>
public interface IGameSessionCoordinator
{
    /// <summary>
    /// Gets whether social gaming features (leaderboards, achievements) are available.
    /// </summary>
    bool IsSocialGamingAvailable { get; }

    /// <summary>
    /// Shows the platform leaderboard UI.
    /// </summary>
    Task ShowLeaderboardAsync();

    /// <summary>
    /// Shows the platform achievements UI.
    /// </summary>
    Task ShowAchievementsAsync();

    /// <summary>
    /// Called after each successful move to update achievements and statistics.
    /// </summary>
    /// <param name="state">The current game state after the move.</param>
    Task OnMoveCompletedAsync(GameState state);

    /// <summary>
    /// Called when the score changes to potentially submit to leaderboards.
    /// </summary>
    /// <param name="newScore">The new score.</param>
    /// <param name="isNewBestScore">Whether this is a new personal best.</param>
    Task OnScoreChangedAsync(int newScore, bool isNewBestScore);
}
