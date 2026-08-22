using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Helpers;

/// <summary>
/// Provides pure logic for detecting swipe direction from gesture delta values.
/// All methods are static and testable without MAUI dependencies.
/// </summary>
public static class SwipeDirectionDetector
{
    /// <summary>
    /// Default minimum swipe distance in pixels to trigger a completed swipe.
    /// </summary>
    public const double DefaultMinSwipeDistance = 30;

    /// <summary>
    /// Default minimum distance for preview direction detection.
    /// </summary>
    public const double DefaultMinPreviewDistance = 8;

    /// <summary>
    /// Default speed threshold in pixels per millisecond for fast swipe detection.
    /// Values above this are considered "fast" swipes (e.g., 0.8 => ~800 px/s).
    /// </summary>
    public const double DefaultFastSwipeSpeedThreshold = 0.8;

    /// <summary>
    /// Determines the swipe direction based on delta values and a distance threshold.
    /// </summary>
    /// <param name="deltaX">Horizontal delta from the gesture start point.</param>
    /// <param name="deltaY">Vertical delta from the gesture start point.</param>
    /// <param name="threshold">Minimum distance required to register as a swipe.</param>
    /// <returns>The detected direction, or null if the delta is below threshold.</returns>
    public static Direction? GetDirection(double deltaX, double deltaY, double threshold)
    {
        // Determine which axis has more movement
        if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            // Horizontal movement dominant
            if (Math.Abs(deltaX) > threshold)
            {
                return deltaX > 0 ? Direction.Right : Direction.Left;
            }
        }
        else
        {
            // Vertical movement dominant
            if (Math.Abs(deltaY) > threshold)
            {
                return deltaY > 0 ? Direction.Down : Direction.Up;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the direction for swipe preview (using a smaller threshold).
    /// </summary>
    /// <param name="deltaX">Horizontal delta from the gesture start point.</param>
    /// <param name="deltaY">Vertical delta from the gesture start point.</param>
    /// <returns>The detected direction, or null if below preview threshold.</returns>
    public static Direction? GetPreviewDirection(double deltaX, double deltaY)
    {
        return GetDirection(deltaX, deltaY, DefaultMinPreviewDistance);
    }

    /// <summary>
    /// Gets the direction for a completed swipe (using the full swipe threshold).
    /// </summary>
    /// <param name="deltaX">Horizontal delta from the gesture start point.</param>
    /// <param name="deltaY">Vertical delta from the gesture start point.</param>
    /// <returns>The detected direction, or null if below swipe threshold.</returns>
    public static Direction? GetSwipeDirection(double deltaX, double deltaY)
    {
        return GetDirection(deltaX, deltaY, DefaultMinSwipeDistance);
    }

    /// <summary>
    /// Calculates the gesture speed from distance and elapsed time.
    /// </summary>
    /// <param name="deltaX">Horizontal delta.</param>
    /// <param name="deltaY">Vertical delta.</param>
    /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
    /// <returns>Speed in pixels per millisecond.</returns>
    public static double CalculateSpeed(double deltaX, double deltaY, double elapsedMs)
    {
        double distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        double safeElapsed = Math.Max(1, elapsedMs);
        return distance / safeElapsed;
    }

    /// <summary>
    /// Determines if a gesture is considered a "fast" swipe based on speed.
    /// </summary>
    /// <param name="deltaX">Horizontal delta.</param>
    /// <param name="deltaY">Vertical delta.</param>
    /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
    /// <param name="speedThreshold">Optional custom speed threshold (default: 0.8 px/ms).</param>
    /// <returns>True if the gesture speed exceeds the threshold.</returns>
    public static bool IsFastSwipe(
        double deltaX,
        double deltaY,
        double elapsedMs,
        double speedThreshold = DefaultFastSwipeSpeedThreshold
    )
    {
        return CalculateSpeed(deltaX, deltaY, elapsedMs) >= speedThreshold;
    }
}
