using System;

namespace TwentyFortyEight.Maui.Victory;

/// <summary>
/// Constants for the cinematic victory animation phases.
/// All values in milliseconds.
/// </summary>
public static class CinematicTimingConstants
{
    /// <summary>
    /// Duration of the impact/shockwave phase.
    /// </summary>
    public const float ImpactDuration = 600f;

    /// <summary>
    /// Duration of the warp transition (tile dissolves into warp).
    /// </summary>
    public const float WarpTransitionDuration = 400f;

    /// <summary>
    /// Number of warp lines to draw.
    /// </summary>
    public const int WarpLineCount = 160;

    /// <summary>
    /// Maximum radius of the shockwave as a multiplier of screen width.
    /// </summary>
    public const float MaxShockwaveRadius = 1.5f;

    /// <summary>
    /// Warp speed multiplier during sustain phase (slower while modal is visible).
    /// </summary>
    public const float WarpSustainSpeedMultiplier = 0.4f;

    /// <summary>
    /// Warp opacity during sustain phase (dimmer while modal is visible).
    /// </summary>
    public const float WarpSustainOpacity = 0.25f;

    /// <summary>
    /// Linear interpolation between two values.
    /// </summary>
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>
    /// Cubic ease-out function for smooth deceleration.
    /// </summary>
    public static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);
}
