using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Base implementation of accelerometer-based orientation monitoring.
/// Shared between Android and iOS platforms.
/// </summary>
public partial class AccelerometerService : IAccelerometerService
{
    private readonly ILogger<AccelerometerService> _logger;
    private DeviceOrientation _currentOrientation = DeviceOrientation.Portrait;
    private int _isMonitoring;
    private const double OrientationThreshold = 1.15; // 15% threshold to prevent jittery changes

    public AccelerometerService(ILogger<AccelerometerService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public event EventHandler<OrientationChangedEventArgs>? OrientationChanged;

    /// <inheritdoc/>
    public bool IsSupported => Accelerometer.Default.IsSupported;

    /// <inheritdoc/>
    public DeviceOrientation CurrentOrientation => _currentOrientation;

    /// <inheritdoc/>
    public void Start()
    {
        if (
            Interlocked.CompareExchange(ref _isMonitoring, 1, 0) == 1
            || !Accelerometer.Default.IsSupported
        )
            return;

        try
        {
            Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
            Accelerometer.Default.Start(SensorSpeed.UI);
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _isMonitoring, 0);
            LogStartError(_logger, ex);
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _isMonitoring, 0, 1) == 0)
            return;

        try
        {
            Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
            Accelerometer.Default.Stop();
        }
        catch (Exception ex)
        {
            LogStopError(_logger, ex);
        }
    }

    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var data = e.Reading;

        // Determine orientation based on accelerometer data with threshold to prevent jitter
        // When device is in portrait, Y acceleration is dominant (gravity pulls down)
        // When device is in landscape, X acceleration is dominant
        var absX = Math.Abs(data.Acceleration.X);
        var absY = Math.Abs(data.Acceleration.Y);

        // Use threshold to prevent rapid switching at boundary angles
        var newOrientation =
            absX > absY * OrientationThreshold
                ? DeviceOrientation.Landscape
                : DeviceOrientation.Portrait;

        if (newOrientation != _currentOrientation)
        {
            _currentOrientation = newOrientation;
            OrientationChanged?.Invoke(this, new OrientationChangedEventArgs(newOrientation));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to start accelerometer")]
    private static partial void LogStartError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to stop accelerometer")]
    private static partial void LogStopError(ILogger logger, Exception ex);
}
