namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Consolidates user feedback mechanisms: accessibility, haptics, and dialogs.
/// </summary>
public sealed class UserFeedbackService(
    IScreenReaderService screenReaderService,
    IHapticService hapticService,
    IAlertService alertService,
    ILocalizationService localizationService,
    ISettingsService settingsService
) : IUserFeedbackService
{
    // Minimum score change before announcing (prevents spam)
    private const int ScoreAnnouncementThreshold = 10;

    public void AnnounceScoreIfSignificant(int score, int previousScore)
    {
        if (
            score > 0
            && score > previousScore
            && score - previousScore >= ScoreAnnouncementThreshold
        )
        {
            screenReaderService.Announce(localizationService.ScreenReaderScoreAnnouncement(score));
        }
    }

    public void AnnounceGameOver(int finalScore)
    {
        screenReaderService.Announce(
            localizationService.ScreenReaderGameOverFinalScore(finalScore)
        );
    }

    public void AnnounceWin()
    {
        screenReaderService.Announce(localizationService.YouWin);
    }

    public void AnnounceStatus(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            screenReaderService.Announce(message);
        }
    }

    public void PerformMoveHaptic()
    {
        if (settingsService.HapticsEnabled && hapticService.IsSupported)
        {
            hapticService.PerformHaptic();
        }
    }

    public Task<bool> ConfirmNewGameAsync()
    {
        return alertService.ShowConfirmationAsync(
            localizationService.RestartConfirmTitle,
            localizationService.RestartConfirmMessage,
            localizationService.StartNew,
            localizationService.Cancel
        );
    }

    public Task<bool> ShowGameOverAsync(int score, int bestScore)
    {
        string message = $"{localizationService.YourScore}\n{score}";
        if (score >= bestScore && bestScore > 0)
        {
            message += $"\n\n{localizationService.BestFormat.Replace("{0}", bestScore.ToString())}";
        }

        return alertService.ShowConfirmationAsync(
            localizationService.GameOver,
            message,
            localizationService.TryAgain,
            localizationService.Cancel
        );
    }

    public Task ShowHowToPlayAsync()
    {
        return alertService.ShowAlertAsync(
            localizationService.HowToPlayTitle,
            localizationService.HowToPlayContent,
            localizationService.GotIt
        );
    }
}
