namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private const string AnimationsEnabledKey = "AnimationsEnabled";
    private const string AnimationSpeedKey = "AnimationSpeed";

    /// <summary>
    /// Gets or sets whether animations are enabled.
    /// </summary>
    public bool AnimationsEnabled
    {
        get => Preferences.Get(AnimationsEnabledKey, true);
        set => Preferences.Set(AnimationsEnabledKey, value);
    }

    /// <summary>
    /// Gets or sets the animation speed multiplier (0.5 = slow, 1.0 = normal, 1.5 = fast).
    /// </summary>
    public double AnimationSpeed
    {
        get => Preferences.Get(AnimationSpeedKey, 1.0);
        set => Preferences.Set(AnimationSpeedKey, Math.Clamp(value, 0.5, 1.5));
    }
}
