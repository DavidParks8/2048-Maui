using Microsoft.Maui.Devices.Sensors;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Android implementation of accelerometer-based orientation monitoring.
/// </summary>
public partial class AccelerometerService : IAccelerometerService
{
    private DeviceOrientation _currentOrientation = DeviceOrientation.Portrait;
    private bool _isMonitoring;
    private const double OrientationThreshold = 1.15; // 15% threshold to prevent jittery changes

    /// <inheritdoc/>
    public event EventHandler<OrientationChangedEventArgs>? OrientationChanged;

    /// <inheritdoc/>
    public bool IsSupported => true;

    /// <inheritdoc/>
    public DeviceOrientation CurrentOrientation => _currentOrientation;

    /// <inheritdoc/>
    public void Start()
    {
        if (_isMonitoring || !Accelerometer.Default.IsSupported)
            return;

        try
        {
            Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
            Accelerometer.Default.Start(SensorSpeed.UI);
            _isMonitoring = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start accelerometer: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_isMonitoring)
            return;

        try
        {
            Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
            Accelerometer.Default.Stop();
            _isMonitoring = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to stop accelerometer: {ex.Message}");
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
}
