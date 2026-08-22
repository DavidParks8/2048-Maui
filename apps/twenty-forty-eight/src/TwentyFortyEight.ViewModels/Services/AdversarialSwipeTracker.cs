namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Tracks swipe attempts in adversarial mode and determines when to show hints.
/// Thread-safe with cooldown to prevent toast spam.
/// </summary>
internal sealed class AdversarialSwipeTracker : IAdversarialSwipeTracker
{
    private const int SwipeAttemptsBeforeHint = 2;
    private const int CooldownMilliseconds = 3000; // 3 seconds cooldown between toasts
    private static readonly long CooldownTicks = TimeSpan
        .FromMilliseconds(CooldownMilliseconds)
        .Ticks;

    private int _consecutiveSwipeAttempts = 0;
    private long _lastHintShownTicks = 0;

    /// <inheritdoc />
    public bool RecordSwipeAttempt()
    {
        // Use Interlocked.Increment for thread-safe increment
        int currentCount = Interlocked.Increment(ref _consecutiveSwipeAttempts);

        if (currentCount >= SwipeAttemptsBeforeHint)
        {
            // Check cooldown period
            long currentTicks = DateTime.UtcNow.Ticks;
            long lastShown = Interlocked.Read(ref _lastHintShownTicks);
            long ticksSinceLastHint = currentTicks - lastShown;

            if (ticksSinceLastHint >= CooldownTicks)
            {
                // Update last shown time atomically
                Interlocked.Exchange(ref _lastHintShownTicks, currentTicks);
                // Reset counter after showing hint
                Interlocked.Exchange(ref _consecutiveSwipeAttempts, 0);
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Interlocked.Exchange(ref _consecutiveSwipeAttempts, 0);
    }
}
