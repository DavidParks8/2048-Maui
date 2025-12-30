using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

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
