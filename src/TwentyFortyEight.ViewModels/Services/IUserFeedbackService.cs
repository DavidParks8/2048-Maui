namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Provides unified user feedback through accessibility, haptics, and dialogs.
/// Abstracts platform-specific feedback mechanisms from the ViewModel.
/// </summary>
public interface IUserFeedbackService
{
    /// <summary>
    /// Announces the current score to screen readers.
    /// Implementation should debounce to avoid overwhelming users.
    /// </summary>
    /// <param name="score">The current score.</param>
    /// <param name="previousScore">The previous score (for delta calculation).</param>
    void AnnounceScoreIfSignificant(int score, int previousScore);

    /// <summary>
    /// Announces game over state to screen readers.
    /// </summary>
    /// <param name="finalScore">The final score achieved.</param>
    void AnnounceGameOver(int finalScore);

    /// <summary>
    /// Announces a win to screen readers.
    /// </summary>
    void AnnounceWin();

    /// <summary>
    /// Announces arbitrary status text to screen readers.
    /// </summary>
    /// <param name="message">The message to announce.</param>
    void AnnounceStatus(string message);

    /// <summary>
    /// Triggers haptic feedback for a successful move.
    /// Respects user's haptic settings internally.
    /// </summary>
    void PerformMoveHaptic();

    /// <summary>
    /// Triggers a distinct haptic feedback for a victory.
    /// Respects user's haptic settings internally.
    /// </summary>
    void PerformVictoryHaptic();

    /// <summary>
    /// Shows a confirmation dialog for starting a new game.
    /// </summary>
    /// <returns>True if user confirmed, false if cancelled.</returns>
    Task<bool> ConfirmNewGameAsync();

    /// <summary>
    /// Shows the game over dialog with final score.
    /// </summary>
    /// <param name="score">The final score achieved.</param>
    /// <param name="bestScore">The best score.</param>
    /// <returns>True if user wants to try again, false otherwise.</returns>
    Task<bool> ShowGameOverAsync(int score, int bestScore);

    /// <summary>
    /// Shows the how to play instructions.
    /// </summary>
    Task ShowHowToPlayAsync();
}
