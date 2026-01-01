namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Represents a semantic haptic pattern.
/// </summary>
public enum HapticPattern
{
    /// <summary>
    /// Standard move feedback.
    /// </summary>
    Move,

    /// <summary>
    /// Distinct victory feedback.
    /// </summary>
    Victory,
}

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
    /// Performs the default haptic feedback.
    /// Backwards-compatible with older call sites.
    /// </summary>
    void PerformHaptic();

    /// <summary>
    /// Performs a semantic haptic feedback pattern.
    /// Implementations must avoid throwing when haptics are not supported.
    /// </summary>
    /// <param name="pattern">The haptic pattern to perform.</param>
    void PerformHaptic(HapticPattern pattern);
}
