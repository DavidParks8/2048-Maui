using SkiaSharp;

namespace TwentyFortyEight.Maui.Victory.Phases;

public class WarpTransitionPhaseDrawer(WarpLineRenderer warpRenderer) : IVictoryPhaseDrawer
{
    private readonly SKPaint _scrimPaint = new() { IsAntialias = true };
    private readonly SKPaint _tilePaint = new() { IsAntialias = true };

    public float Duration => CinematicTimingConstants.WarpTransitionDuration;

    public void Draw(SKCanvas canvas, SKImageInfo info, float progress, VictoryAnimationContext ctx)
    {
        // Darken background
        float scrimAlpha = CinematicTimingConstants.Lerp(0f, 0.7f, progress);
        _scrimPaint.Color = new SKColor(0, 0, 0, (byte)(scrimAlpha * 255));
        canvas.DrawRect(0, 0, info.Width, info.Height, _scrimPaint);

        // Tile dissolves (fade out in first half)
        if (progress < 0.5f)
        {
            float tileAlpha = 1f - (progress * 2f);
            _tilePaint.Color = new SKColor(255, 255, 255, (byte)(tileAlpha * 255));

            canvas.Save();
            canvas.Translate(
                ctx.TileCenter.X - ctx.TileSize.Width / 2,
                ctx.TileCenter.Y - ctx.TileSize.Height / 2
            );
            canvas.DrawImage(ctx.TileSnapshot, 0, 0, _tilePaint);
            canvas.Restore();
        }

        // Draw emerging warp lines
        warpRenderer.Draw(canvas, info, ctx, intensity: progress);
    }
}
