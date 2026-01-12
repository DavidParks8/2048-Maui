namespace TwentyFortyEight.ViewModels.Helpers;

/// <summary>
/// Tracks velocity during pan gestures by recording recent position samples.
/// Velocity is computed from the most recent samples to capture momentum at gesture end.
/// </summary>
public class PanVelocityTracker
{
    private const double MinTimeDelta = 0.001;
    private const double MaxTimeDelta = 0.15;

    private DateTime _previousTime;
    private double _previousX;
    private double _previousY;
    private double _lastComputedVelocityY;
    private bool _hasValidSample;

    /// <summary>
    /// Resets the tracker for a new gesture.
    /// Call this when a pan gesture starts.
    /// </summary>
    public void Reset()
    {
        _hasValidSample = false;
        _lastComputedVelocityY = 0.0;
    }

    /// <summary>
    /// Records a position sample during the pan gesture.
    /// Call this on each pan update (Running status).
    /// </summary>
    /// <param name="totalX">Cumulative X position from gesture start</param>
    /// <param name="totalY">Cumulative Y position from gesture start</param>
    /// <param name="timestamp">Timestamp of this sample</param>
    public void RecordSample(double totalX, double totalY, DateTime timestamp)
    {
        if (_hasValidSample)
        {
            var timeDelta = (timestamp - _previousTime).TotalSeconds;
            var deltaX = totalX - _previousX;
            var deltaY = totalY - _previousY;

            // Only calculate velocity if time delta is within valid range
            // This ensures we don't lose velocity during pauses at gesture end
            if (timeDelta >= MinTimeDelta && timeDelta <= MaxTimeDelta)
            {
                // Check if swipe is mostly vertical
                if (BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY))
                {
                    _lastComputedVelocityY = deltaY / timeDelta;
                }
                else
                {
                    // Diagonal/horizontal movement resets velocity
                    _lastComputedVelocityY = 0.0;
                }
            }
            // If time delta is outside valid range, preserve the last computed velocity
        }

        _previousTime = timestamp;
        _previousX = totalX;
        _previousY = totalY;
        _hasValidSample = true;
    }

    /// <summary>
    /// Gets the last computed velocity in pixels per second.
    /// Positive values indicate downward movement, negative indicates upward.
    /// </summary>
    /// <returns>Velocity in pixels per second</returns>
    public double GetVelocity()
    {
        return _lastComputedVelocityY;
    }
}
