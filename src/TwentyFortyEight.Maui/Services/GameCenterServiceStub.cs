namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Stub implementation of IGameCenterService for non-iOS platforms.
/// All operations are no-ops.
/// </summary>
public class GameCenterServiceStub : IGameCenterService
{
    public bool IsAvailable => false;

    public Task AuthenticateAsync()
    {
        return Task.CompletedTask;
    }

    public Task SubmitScoreAsync(long score)
    {
        return Task.CompletedTask;
    }

    public Task ReportAchievementAsync(string achievementId, double percentComplete)
    {
        return Task.CompletedTask;
    }

    public Task ShowLeaderboardAsync()
    {
        return Task.CompletedTask;
    }

    public Task ShowAchievementsAsync()
    {
        return Task.CompletedTask;
    }
}
