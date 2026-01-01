using SkiaSharp;

namespace TwentyFortyEight.Maui.Victory;

/// <summary>
/// Interface for drawing a single phase of the victory animation.
/// </summary>
public interface IVictoryPhaseDrawer
{
    /// <summary>
    /// Duration of this phase in milliseconds.
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Draw the phase at the given progress (0 to 1).
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
    /// <param name="info">Canvas size info.</param>
    /// <param name="progress">Progress through this phase (0 = start, 1 = end).</param>
    /// <param name="context">Shared context containing snapshots and positions.</param>
    void Draw(SKCanvas canvas, SKImageInfo info, float progress, VictoryAnimationContext context);
}
