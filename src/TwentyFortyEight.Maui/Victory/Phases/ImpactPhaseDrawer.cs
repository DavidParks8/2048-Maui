using SkiaSharp;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Victory.Phases;

public class ImpactPhaseDrawer(IUserFeedbackService feedbackService) : IVictoryPhaseDrawer
{
    private readonly SKPaint _shockwavePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
    };

    private bool _hapticFired;

    public float Duration => CinematicTimingConstants.ImpactDuration;

    public void Draw(SKCanvas canvas, SKImageInfo info, float progress, VictoryAnimationContext ctx)
    {
        // Trigger haptic once at the moment of impact
        if (!_hapticFired)
        {
            _hapticFired = true;
            feedbackService.PerformVictoryHaptic();
        }

        // Draw frozen board
        canvas.DrawImage(ctx.BoardSnapshot, 0, 0);

        // Draw shockwave rings
        float maxRadius =
            Math.Max(info.Width, info.Height) * CinematicTimingConstants.MaxShockwaveRadius;
        float radius = CinematicTimingConstants.EaseOutCubic(progress) * maxRadius;
        float alpha = 1f - progress;

        // Outer ring (gold)
        _shockwavePaint.Color = new SKColor(255, 215, 0, (byte)(alpha * 200));
        _shockwavePaint.StrokeWidth = CinematicTimingConstants.Lerp(8f, 2f, progress);
        canvas.DrawCircle(ctx.TileCenter, radius, _shockwavePaint);

        // Inner ring (white)
        _shockwavePaint.Color = new SKColor(255, 255, 255, (byte)(alpha * 150));
        _shockwavePaint.StrokeWidth = CinematicTimingConstants.Lerp(4f, 1f, progress);
        canvas.DrawCircle(ctx.TileCenter, radius * 0.7f, _shockwavePaint);
    }
}
