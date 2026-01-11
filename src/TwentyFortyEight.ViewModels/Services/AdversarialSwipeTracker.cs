namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Tracks swipe attempts in adversarial mode and determines when to show hints.
/// </summary>
internal sealed class AdversarialSwipeTracker : IAdversarialSwipeTracker
{
    private const int SwipeAttemptsBeforeHint = 2;
    private int _consecutiveSwipeAttempts = 0;

    /// <inheritdoc />
    public bool RecordSwipeAttempt()
    {
        _consecutiveSwipeAttempts++;

        if (_consecutiveSwipeAttempts >= SwipeAttemptsBeforeHint)
        {
            _consecutiveSwipeAttempts = 0; // Reset counter after showing hint
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _consecutiveSwipeAttempts = 0;
    }
}
