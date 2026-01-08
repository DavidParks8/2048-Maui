namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Service to query OS accessibility settings that affect UX.
/// </summary>
public interface IAccessibilitySettingsService
{
    /// <summary>
    /// Raised when relevant accessibility settings change at the OS level.
    /// </summary>
    event EventHandler? AccessibilitySettingsChanged;

    /// <summary>
    /// Returns true if the user prefers reduced motion animations.
    /// </summary>
    bool ShouldReduceMotion();

    /// <summary>
    /// Returns true if OS-level Voice Control (hands-free) is enabled.
    /// </summary>
    bool IsVoiceControlEnabled();
}
