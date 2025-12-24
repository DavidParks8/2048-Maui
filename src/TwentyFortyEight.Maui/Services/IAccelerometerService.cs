namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for monitoring device orientation changes via accelerometer.
/// </summary>
public interface IAccelerometerService
{
    /// <summary>
    /// Event raised when device orientation changes between portrait and landscape.
    /// </summary>
    event EventHandler<OrientationChangedEventArgs>? OrientationChanged;

    /// <summary>
    /// Gets whether the accelerometer is available and supported on this platform.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Gets the current orientation.
    /// </summary>
    DeviceOrientation CurrentOrientation { get; }

    /// <summary>
    /// Starts monitoring accelerometer for orientation changes.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops monitoring accelerometer.
    /// </summary>
    void Stop();
}

/// <summary>
/// Device orientation types.
/// </summary>
public enum DeviceOrientation
{
    Portrait,
    Landscape,
}

/// <summary>
/// Event args for orientation change events.
/// </summary>
public class OrientationChangedEventArgs : EventArgs
{
    public DeviceOrientation Orientation { get; }

    public OrientationChangedEventArgs(DeviceOrientation orientation)
    {
        Orientation = orientation;
    }
}
