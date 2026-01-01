using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for handling swipe gesture recognition.
/// Abstracts pointer and pan gesture tracking for cross-platform compatibility.
/// </summary>
public interface IGestureRecognizerService
{
    /// <summary>
    /// Raised when a swipe gesture is detected.
    /// </summary>
    event EventHandler<Direction>? SwipeDetected;

    /// <summary>
    /// Attaches swipe gesture recognizers to a view.
    /// </summary>
    /// <param name="view">The view to attach recognizers to.</param>
    void AttachSwipeRecognizers(View view);

    /// <summary>
    /// Detaches swipe gesture recognizers from a view.
    /// </summary>
    /// <param name="view">The view to detach recognizers from.</param>
    void DetachSwipeRecognizers(View view);
}
