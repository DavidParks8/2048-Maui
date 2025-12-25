using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Coordinates meta-game features like achievements and social gaming.
/// </summary>
public sealed partial class GameSessionCoordinator(
    ISocialGamingService socialGamingService,
    IAchievementTracker achievementTracker,
    IAchievementIdMapper achievementIdMapper,
    ILogger<GameSessionCoordinator> logger
) : IGameSessionCoordinator
{
    public bool IsSocialGamingAvailable => socialGamingService.IsAvailable;

    public Task ShowLeaderboardAsync() => socialGamingService.ShowLeaderboardAsync();

    public Task ShowAchievementsAsync() => socialGamingService.ShowAchievementsAsync();

    public async Task OnMoveCompletedAsync(GameState state)
    {
        try
        {
            await CheckAndReportAchievementsAsync(state);
        }
        catch (Exception ex)
        {
            LogMoveCompletedProcessingFailed(logger, ex);
        }
    }

    public async Task OnScoreChangedAsync(int newScore, bool isNewBestScore)
    {
        if (!isNewBestScore)
        {
            return;
        }

        try
        {
            await socialGamingService.SubmitScoreAsync(newScore);
        }
        catch (Exception ex)
        {
            LogScoreSubmitFailed(logger, ex);
        }
    }

    private async Task CheckAndReportAchievementsAsync(GameState state)
    {
        // Check for tile achievements
        if (achievementTracker.CheckTileAchievement(state.MaxTileValue))
        {
            var tileValue = achievementTracker.LastUnlockedTileValue!.Value;
            var achievementId = achievementIdMapper.GetTileAchievementId(tileValue);
            if (achievementId != null)
            {
                await socialGamingService.ReportAchievementAsync(achievementId, 100.0);
            }
        }

        // Check for first win achievement
        if (achievementTracker.CheckFirstWinAchievement(state.IsWon))
        {
            var achievementId = achievementIdMapper.GetFirstWinAchievementId();
            if (achievementId != null)
            {
                await socialGamingService.ReportAchievementAsync(achievementId, 100.0);
            }
        }

        // Check for score achievements
        if (achievementTracker.CheckScoreAchievement(state.Score))
        {
            var scoreMilestone = achievementTracker.LastUnlockedScoreMilestone!.Value;
            var achievementId = achievementIdMapper.GetScoreAchievementId(scoreMilestone);
            if (achievementId != null)
            {
                await socialGamingService.ReportAchievementAsync(achievementId, 100.0);
            }
        }

        // Reset flags after reporting
        achievementTracker.ResetJustUnlocked();
    }

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Error,
        Message = "Failed to process move completion"
    )]
    private static partial void LogMoveCompletedProcessingFailed(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Error,
        Message = "Failed to submit score to social gaming service"
    )]
    private static partial void LogScoreSubmitFailed(ILogger logger, Exception ex);
}
