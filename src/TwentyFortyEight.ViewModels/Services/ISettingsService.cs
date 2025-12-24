namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Interface for managing application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets whether animations are enabled.
    /// </summary>
    bool AnimationsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the animation speed multiplier (0.5 = slow, 1.0 = normal, 1.5 = fast).
    /// </summary>
    double AnimationSpeed { get; set; }
}
