using TwentyFortyEight.ViewModels.Services;
using MauiSocialGamingService = TwentyFortyEight.Maui.Services.ISocialGamingService;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Adapter that wraps the MAUI social gaming service to implement the ViewModels interface.
/// </summary>
public class SocialGamingServiceAdapter : ISocialGamingService
{
    private readonly MauiSocialGamingService _mauiService;

    public SocialGamingServiceAdapter(MauiSocialGamingService mauiService)
    {
        _mauiService = mauiService;
    }

    public bool IsAvailable => _mauiService.IsAvailable;

    public Task SubmitScoreAsync(long score) => _mauiService.SubmitScoreAsync(score);

    public Task ReportAchievementAsync(string achievementId, double percentComplete) =>
        _mauiService.ReportAchievementAsync(achievementId, percentComplete);

    public Task ShowLeaderboardAsync() => _mauiService.ShowLeaderboardAsync();

    public Task ShowAchievementsAsync() => _mauiService.ShowAchievementsAsync();
}
