namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Windows implementation of accelerometer-based orientation monitoring (disabled).
/// Accelerometer-based reflow is not supported on Windows.
/// </summary>
public partial class AccelerometerService : IAccelerometerService
{
    /// <inheritdoc/>
    public event EventHandler<OrientationChangedEventArgs>? OrientationChanged;

    /// <inheritdoc/>
    public bool IsSupported => false;

    /// <inheritdoc/>
    public DeviceOrientation CurrentOrientation => DeviceOrientation.Portrait;

    /// <inheritdoc/>
    public void Start()
    {
        // No-op on Windows
    }

    /// <inheritdoc/>
    public void Stop()
    {
        // No-op on Windows
    }
}
