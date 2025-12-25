using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of ISettingsService using Preferences.
/// </summary>
public class MauiSettingsService : ISettingsService
{
    private const string AnimationsEnabledKey = "AnimationsEnabled";
    private const string AnimationSpeedKey = "AnimationSpeed";
    private const string HapticsEnabledKey = "HapticsEnabled";
    private const string AdsRemovedKey = "AdsRemoved";
    private const double DefaultAnimationSpeed = 1.0;
    private const double MinAnimationSpeed = 0.5;
    private const double MaxAnimationSpeed = 1.5;

    /// <inheritdoc />
    public bool AnimationsEnabled
    {
        get => Preferences.Get(AnimationsEnabledKey, true);
        set => Preferences.Set(AnimationsEnabledKey, value);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool HapticsEnabled
    {
        get => Preferences.Get(HapticsEnabledKey, true);
        set => Preferences.Set(HapticsEnabledKey, value);
    }

    /// <inheritdoc />
    public bool AdsRemoved
    {
        get => Preferences.Get(AdsRemovedKey, false);
        set => Preferences.Set(AdsRemovedKey, value);
    }
}
