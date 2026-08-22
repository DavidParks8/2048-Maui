namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Abstraction for localized string resources.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the localized restart confirmation title.
    /// </summary>
    string RestartConfirmTitle { get; }

    /// <summary>
    /// Gets the localized restart confirmation message.
    /// </summary>
    string RestartConfirmMessage { get; }

    /// <summary>
    /// Gets the localized "Start New" button text.
    /// </summary>
    string StartNew { get; }

    /// <summary>
    /// Gets the localized "Cancel" button text.
    /// </summary>
    string Cancel { get; }

    /// <summary>
    /// Gets the localized "Game Over!" title.
    /// </summary>
    string GameOver { get; }

    /// <summary>
    /// Gets the localized "Your Score" text.
    /// </summary>
    string YourScore { get; }

    /// <summary>
    /// Gets the localized "Best: {0}" format string.
    /// </summary>
    string BestFormat { get; }

    /// <summary>
    /// Gets the localized "Try Again" button text.
    /// </summary>
    string TryAgain { get; }

    /// <summary>
    /// Gets the localized "How to Play" title.
    /// </summary>
    string HowToPlayTitle { get; }

    /// <summary>
    /// Gets the localized how to play content.
    /// </summary>
    string HowToPlayContent { get; }

    /// <summary>
    /// Gets the localized "Got It!" button text.
    /// </summary>
    string GotIt { get; }

    /// <summary>
    /// Gets the localized "You Win!" status text.
    /// </summary>
    string YouWin { get; }

    /// <summary>
    /// Gets the localized reset statistics title.
    /// </summary>
    string ResetStatisticsTitle { get; }

    /// <summary>
    /// Gets the localized reset statistics message.
    /// </summary>
    string ResetStatisticsMessage { get; }

    /// <summary>
    /// Gets the localized "Reset" button text.
    /// </summary>
    string Reset { get; }

    /// <summary>
    /// Gets the localized screen reader announcement for the current score.
    /// </summary>
    /// <param name="score">The current score.</param>
    /// <returns>A formatted announcement string.</returns>
    string ScreenReaderScoreAnnouncement(int score);

    /// <summary>
    /// Gets the localized screen reader announcement for game over with final score.
    /// </summary>
    /// <param name="finalScore">The final score achieved.</param>
    /// <returns>A formatted announcement string.</returns>
    string ScreenReaderGameOverFinalScore(int finalScore);

    /// <summary>
    /// Gets the localized score display string.
    /// </summary>
    /// <param name="score">The score value.</param>
    /// <returns>A formatted score string (e.g., "Score: 2048").</returns>
    string FormatScore(int score);

    /// <summary>
    /// Gets the localized undo count display string.
    /// </summary>
    /// <param name="undoCount">The number of undos used.</param>
    /// <returns>A formatted undo count string (e.g., "Undos used: 5").</returns>
    string FormatUndoCount(int undoCount);

    /// <summary>
    /// Gets the localized screen reader announcement for the Coach nudge.
    /// </summary>
    string CoachNudgeAnnouncement { get; }

    /// <summary>
    /// Gets the localized victory subtitle based on game mode.
    /// </summary>
    /// <param name="isAdversarialMode">True if the victory was in Adversarial mode.</param>
    /// <returns>"You blocked 2048!" for Adversarial, "You reached 2048!" otherwise.</returns>
    string GetVictorySubtitle(bool isAdversarialMode);

    /// <summary>
    /// Gets the localized adversarial mode tap hint message.
    /// </summary>
    string AdversarialModeTapHint { get; }
}
