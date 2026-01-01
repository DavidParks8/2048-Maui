using SkiaSharp;

namespace TwentyFortyEight.Maui.Victory;

/// <summary>
/// Shared context passed to all phase drawers.
/// </summary>
public class VictoryAnimationContext
{
    public required SKImage BoardSnapshot { get; init; }
    public required SKImage TileSnapshot { get; init; }
    public required SKPoint TileCenter { get; init; }
    public required SKSize TileSize { get; init; }

    /// <summary>
    /// Time since last rendered frame (seconds). Set by the orchestrator.
    /// Used for time-based effects like warp-line movement.
    /// </summary>
    public float DeltaSeconds { get; set; }

    /// <summary>
    /// Warp lines array, shared across warp phases.
    /// </summary>
    public WarpLine[] WarpLines { get; } = new WarpLine[CinematicTimingConstants.WarpLineCount];

    /// <summary>
    /// Tracks whether WarpLines have been initialized for this animation context.
    /// (Needed because WarpLineRenderer is registered as a singleton.)
    /// </summary>
    public bool WarpLinesInitialized { get; set; }

    /// <summary>
    /// Warp speed multiplier (reduced during sustain).
    /// </summary>
    public float WarpSpeed { get; set; } = 1f;

    /// <summary>
    /// Warp opacity multiplier (reduced during sustain).
    /// </summary>
    public float WarpOpacity { get; set; } = 1f;
}
