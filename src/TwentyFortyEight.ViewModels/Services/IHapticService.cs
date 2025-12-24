namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Interface for haptic feedback functionality.
/// </summary>
public interface IHapticService
{
    /// <summary>
    /// Gets a value indicating whether haptic feedback is supported on this device.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Performs a haptic feedback if supported and enabled.
    /// </summary>
    void PerformHaptic();
}
