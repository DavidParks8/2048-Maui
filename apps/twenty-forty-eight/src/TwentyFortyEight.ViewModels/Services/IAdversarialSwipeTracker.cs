namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Tracks swipe attempts in adversarial mode and triggers hints when needed.
/// </summary>
public interface IAdversarialSwipeTracker
{
    /// <summary>
    /// Records a swipe attempt in adversarial mode.
    /// Returns true if the hint should be shown (threshold reached).
    /// </summary>
    /// <returns>True if hint threshold is reached and should be shown.</returns>
    bool RecordSwipeAttempt();

    /// <summary>
    /// Resets the swipe attempt counter.
    /// Should be called when mode changes or when not in adversarial mode.
    /// </summary>
    void Reset();
}
