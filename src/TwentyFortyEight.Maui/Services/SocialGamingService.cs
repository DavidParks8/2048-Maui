namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Default implementation of ISocialGamingService for platforms without social gaming support.
/// Platform-specific implementations should be placed in their respective platform folders.
/// </summary>
public partial class SocialGamingService : ISocialGamingService
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
