using Microsoft.Extensions.Logging;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Windows implementation of accelerometer-based orientation monitoring (disabled).
/// Accelerometer-based reflow is not supported on Windows.
/// </summary>
public partial class AccelerometerService : IAccelerometerService
{
    public AccelerometerService(ILogger<AccelerometerService> logger)
    {
        // Logger not used on Windows, but required for DI consistency
    }

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
