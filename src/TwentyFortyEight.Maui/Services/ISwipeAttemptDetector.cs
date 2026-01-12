namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Lightweight swipe attempt detector for adversarial mode.
/// Detects when users try to swipe (instead of tap) without interfering with tap gestures.
/// </summary>
public interface ISwipeAttemptDetector
{
    /// <summary>
    /// Raised when a swipe attempt is detected (completed swipe-like gesture).
    /// </summary>
    event EventHandler? SwipeAttempted;

    /// <summary>
    /// Attaches the lightweight swipe detector to a view.
    /// Uses gesture recognizers configured to not block taps.
    /// </summary>
    /// <param name="view">The view to attach the detector to.</param>
    void Attach(View view);

    /// <summary>
    /// Detaches the swipe detector from a view.
    /// </summary>
    /// <param name="view">The view to detach from.</param>
    void Detach(View view);
}
