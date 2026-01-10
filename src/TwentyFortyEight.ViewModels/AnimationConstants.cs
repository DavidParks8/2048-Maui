namespace TwentyFortyEight.ViewModels;

/// <summary>
/// Constants for animation durations used throughout the application.
/// These values are shared between the ViewModel and animation service layers.
/// Durations are kept short to allow rapid input without blocking.
/// </summary>
public static class AnimationConstants
{
    /// <summary>
    /// Base duration of the slide animation in milliseconds.
    /// </summary>
    public const uint BaseSlideAnimationDuration = 120;

    /// <summary>
    /// Base duration of the scale-up animation for merged tiles in milliseconds.
    /// </summary>
    public const uint BaseMergePulseUpDuration = 100;

    /// <summary>
    /// Base duration of the scale-down animation for merged tiles in milliseconds.
    /// </summary>
    public const uint BaseMergePulseDownDuration = 75;

    /// <summary>
    /// Base duration of the scale animation for new tiles in milliseconds.
    /// </summary>
    public const uint BaseNewTileScaleDuration = 100;
}
