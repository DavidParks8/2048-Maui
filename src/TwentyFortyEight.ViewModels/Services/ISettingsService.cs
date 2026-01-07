namespace TwentyFortyEight.ViewModels.Services;

using TwentyFortyEight.Core;

/// <summary>
/// Interface for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets whether haptic feedback is enabled.
    /// </summary>
    bool HapticsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether Coach Mode is enabled.
    /// </summary>
    bool CoachEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether Coach nudges are enabled.
    /// </summary>
    bool CoachNudgesEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last active game configuration that should be restored on startup.
    /// </summary>
    GameConfig LastActiveGameConfig { get; set; }
}
