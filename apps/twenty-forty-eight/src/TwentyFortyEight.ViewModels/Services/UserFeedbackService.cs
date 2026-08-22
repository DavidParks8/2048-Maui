namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Consolidates user feedback mechanisms: accessibility, haptics, and dialogs.
/// </summary>
internal sealed class UserFeedbackService(
    IScreenReaderService screenReaderService,
    IHapticService hapticService,
    IAlertService alertService,
    ILocalizationService localizationService,
    ISettingsService settingsService,
    IToastService toastService
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

    public void AnnounceCoachNudge()
    {
        AnnounceStatus(localizationService.CoachNudgeAnnouncement);
    }

    public void PerformMoveHaptic()
    {
        if (settingsService.HapticsEnabled && hapticService.IsSupported)
        {
            // Backwards-compatible default (move) haptic.
            hapticService.PerformHaptic();
        }
    }

    public void PerformSwipePreviewHaptic()
    {
        if (settingsService.HapticsEnabled && hapticService.IsSupported)
        {
            // Keep preview feedback subtle and consistent.
            hapticService.PerformHaptic(HapticPattern.Move);
        }
    }

    public void PerformVictoryHaptic()
    {
        if (settingsService.HapticsEnabled && hapticService.IsSupported)
        {
            // Distinct, stronger victory haptic.
            hapticService.PerformHaptic(HapticPattern.Victory);
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

    public Task<bool> ShowGameOverAsync(int score, int bestScore, int undoCount = 0)
    {
        string message = $"{localizationService.YourScore}\n{score}";
        if (score >= bestScore && bestScore > 0)
        {
            message += $"\n\n{localizationService.BestFormat.Replace("{0}", bestScore.ToString())}";
        }
        if (undoCount > 0)
        {
            message += $"\n\n{localizationService.FormatUndoCount(undoCount)}";
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

    public Task ShowAdversarialModeTapHintAsync()
    {
        // Use the glass toast service for native liquid glass styling
        return toastService.ShowAsync(localizationService.AdversarialModeTapHint);
    }
}
