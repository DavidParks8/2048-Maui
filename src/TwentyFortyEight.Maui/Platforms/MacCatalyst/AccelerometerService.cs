using Microsoft.Extensions.Logging;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Mac Catalyst implementation of accelerometer-based orientation monitoring (disabled).
/// Accelerometer-based reflow is not supported on Mac Catalyst.
/// </summary>
public partial class AccelerometerService : IAccelerometerService
{
    public AccelerometerService(ILogger<AccelerometerService> logger)
    {
        // Logger not used on Mac Catalyst, but required for DI consistency
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
        // No-op on Mac Catalyst
    }

    /// <inheritdoc/>
    public void Stop()
    {
        // No-op on Mac Catalyst
    }
}
