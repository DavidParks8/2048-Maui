namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Interface for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets whether haptic feedback is enabled.
    /// </summary>
    bool HapticsEnabled { get; set; }
}
