using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Provides continuous pan information for swipe/drag interactions.
/// This is raised for both touch (PanGestureRecognizer) and mouse/touch pointer drags.
/// </summary>
public sealed class SwipePanEventArgs : EventArgs
{
    public required GestureStatus Status { get; init; }

    /// <summary>
    /// Total translation since the gesture started, in device-independent pixels.
    /// Positive X is right; positive Y is down.
    /// </summary>
    public required double TotalX { get; init; }

    /// <summary>
    /// Total translation since the gesture started, in device-independent pixels.
    /// Positive X is right; positive Y is down.
    /// </summary>
    public required double TotalY { get; init; }

    /// <summary>
    /// A best-effort direction guess for preview purposes (uses a smaller threshold).
    /// Null until the gesture is sufficiently directional.
    /// </summary>
    public Direction? PreviewDirection { get; init; }

    /// <summary>
    /// The direction that qualifies as a completed swipe (uses the same threshold as the existing swipe logic).
    /// This is most useful on Completed/Canceled.
    /// </summary>
    public Direction? SwipeDirection { get; init; }

    /// <summary>
    /// Time since gesture started.
    /// </summary>
    public required TimeSpan Elapsed { get; init; }

    /// <summary>
    /// Indicates this gesture is moving quickly enough to be treated as a fast swipe.
    /// </summary>
    public bool IsFast { get; init; }
}
