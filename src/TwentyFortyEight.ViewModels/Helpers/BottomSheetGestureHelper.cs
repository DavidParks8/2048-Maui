namespace TwentyFortyEight.ViewModels.Helpers;

/// <summary>
/// Helper class for analyzing pan gestures on bottom sheets.
/// </summary>
public static class BottomSheetGestureHelper
{
    /// <summary>
    /// Ratio threshold for determining if swipe is mostly vertical (not diagonal).
    /// A swipe is considered vertical if abs(verticalDistance) / abs(horizontalDistance) > this ratio.
    /// </summary>
    public const double VerticalSwipeRatioThreshold = 1.5;

    /// <summary>
    /// Determines if a swipe gesture is mostly vertical rather than diagonal.
    /// </summary>
    /// <param name="deltaX">Horizontal distance of swipe</param>
    /// <param name="deltaY">Vertical distance of swipe</param>
    /// <returns>True if the swipe is mostly vertical, false if it's too diagonal</returns>
    public static bool IsSwipeMostlyVertical(double deltaX, double deltaY)
    {
        var absDeltaX = Math.Abs(deltaX);
        var absDeltaY = Math.Abs(deltaY);

        // If horizontal movement is negligible, it's definitely vertical
        if (absDeltaX < 0.001)
        {
            return true;
        }

        // Check if vertical distance is significantly greater than horizontal
        // ratio = vertical / horizontal, should be > threshold for vertical swipe
        var ratio = absDeltaY / absDeltaX;
        return ratio > VerticalSwipeRatioThreshold;
    }

    /// <summary>
    /// Calculates the velocity of a downward swipe gesture.
    /// Returns 0 if the swipe is not mostly vertical (too diagonal).
    /// </summary>
    /// <param name="deltaX">Horizontal distance since last update</param>
    /// <param name="deltaY">Vertical distance since last update</param>
    /// <param name="timeDeltaSeconds">Time elapsed since last update in seconds</param>
    /// <param name="minTimeDelta">Minimum time delta to avoid extreme velocity values</param>
    /// <param name="maxTimeDelta">Maximum time delta for velocity calculation</param>
    /// <returns>Velocity in pixels per second, or 0 if conditions are not met</returns>
    public static double CalculateSwipeVelocity(
        double deltaX,
        double deltaY,
        double timeDeltaSeconds,
        double minTimeDelta = 0.001,
        double maxTimeDelta = 0.5)
    {
        // Only calculate velocity if we have a reasonable time delta
        // Avoid very small time deltas that could cause unrealistic velocity values
        if (timeDeltaSeconds < minTimeDelta || timeDeltaSeconds >= maxTimeDelta)
        {
            return 0.0;
        }

        // Check if swipe is mostly vertical (not diagonal)
        if (!IsSwipeMostlyVertical(deltaX, deltaY))
        {
            return 0.0;
        }

        return deltaY / timeDeltaSeconds;
    }
}
