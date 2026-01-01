using SkiaSharp;

namespace TwentyFortyEight.Maui.Victory.Phases;

public class WarpSustainPhaseDrawer(WarpLineRenderer warpRenderer) : IVictoryPhaseDrawer
{
    private readonly SKPaint _scrimPaint = new() { IsAntialias = true };

    // Sustain runs indefinitely until modal is dismissed
    public float Duration => float.MaxValue;

    public void Draw(SKCanvas canvas, SKImageInfo info, float progress, VictoryAnimationContext ctx)
    {
        // Full scrim
        _scrimPaint.Color = new SKColor(0, 0, 0, (byte)(0.7f * 255));
        canvas.DrawRect(0, 0, info.Width, info.Height, _scrimPaint);

        // Draw warp lines (sustained)
        warpRenderer.Draw(canvas, info, ctx, intensity: 1f);
    }
}
