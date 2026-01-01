namespace TwentyFortyEight.Maui.Victory;

/// <summary>
/// Represents a single line in the hyperspace warp effect.
/// Allocated once and reused to avoid per-frame allocations.
/// </summary>
public struct WarpLine
{
    /// <summary>
    /// Angle in radians from center.
    /// </summary>
    public float Angle;

    /// <summary>
    /// Distance from center (0 = center, 1 = edge). Decreases over time.
    /// </summary>
    public float Distance;

    /// <summary>
    /// Speed multiplier for this line (0.5 to 1.5 for variation).
    /// </summary>
    public float Speed;

    /// <summary>
    /// Length multiplier for the line trail.
    /// </summary>
    public float Length;
}
