namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public class SettingsService : ISettingsService
{
    private const string AnimationsEnabledKey = "AnimationsEnabled";
    private const string AnimationSpeedKey = "AnimationSpeed";
    private const double DefaultAnimationSpeed = 1.0;
    private const double MinAnimationSpeed = 0.5;
    private const double MaxAnimationSpeed = 1.5;

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
        get
        {
            var speed = Preferences.Get(AnimationSpeedKey, DefaultAnimationSpeed);

            if (!double.IsFinite(speed))
            {
                speed = DefaultAnimationSpeed;
                Preferences.Set(AnimationSpeedKey, speed);
                return speed;
            }

            var clamped = Math.Clamp(speed, MinAnimationSpeed, MaxAnimationSpeed);
            if (clamped != speed)
            {
                speed = clamped;
                Preferences.Set(AnimationSpeedKey, speed);
            }

            return speed;
        }
        set =>
            Preferences.Set(
                AnimationSpeedKey,
                Math.Clamp(value, MinAnimationSpeed, MaxAnimationSpeed)
            );
    }
}
