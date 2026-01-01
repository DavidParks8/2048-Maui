using SkiaSharp;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Victory;

/// <summary>
/// Shared renderer for hyperspace warp lines, used by transition and sustain phases.
/// </summary>
public class WarpLineRenderer(IRandomSource random)
{
    private readonly SKPaint _warpPaint = new()
    {
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
        Style = SKPaintStyle.Stroke,
    };

    public void Draw(
        SKCanvas canvas,
        SKImageInfo info,
        VictoryAnimationContext ctx,
        float intensity
    )
    {
        if (!ctx.WarpLinesInitialized)
        {
            InitializeWarpLines(ctx.WarpLines);
            ctx.WarpLinesInitialized = true;
        }

        float cx = info.Width / 2f;
        float cy = info.Height / 2f;
        float maxDist = MathF.Max(info.Width, info.Height) * 0.6f;

        // True time-based movement: deltaSeconds comes from the orchestrator.
        float deltaSeconds = ctx.DeltaSeconds;

        for (int i = 0; i < ctx.WarpLines.Length; i++)
        {
            ref WarpLine line = ref ctx.WarpLines[i];

            // Update position
            line.Distance -= deltaSeconds * ctx.WarpSpeed * line.Speed;
            if (line.Distance <= 0)
            {
                ResetWarpLine(ref line);
                line.Distance = 1f;
            }

            // Calculate visual properties
            float depth = 1f - line.Distance;
            float brightness = depth * depth * intensity * ctx.WarpOpacity;

            if (brightness < 0.01f)
            {
                continue;
            }

            float dirX = MathF.Cos(line.Angle);
            float dirY = MathF.Sin(line.Angle);

            float startDist = depth * maxDist;
            float endDist = startDist * (1f + 0.15f * line.Length);

            // Use SKPoint ctor directly (struct) to avoid allocations.
            SKPoint start = new(cx + dirX * startDist, cy + dirY * startDist);
            SKPoint end = new(cx + dirX * endDist, cy + dirY * endDist);

            _warpPaint.Color = new SKColor(255, 255, 255, (byte)(brightness * 160));
            _warpPaint.StrokeWidth = depth * 1.3f + 0.5f;

            canvas.DrawLine(start, end, _warpPaint);
        }
    }

    private void InitializeWarpLines(WarpLine[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            ResetWarpLine(ref lines[i]);
        }
    }

    private void ResetWarpLine(ref WarpLine line)
    {
        line.Angle = (float)(random.NextDouble() * Math.PI * 2);
        line.Distance = (float)random.NextDouble();
        line.Speed = 0.5f + (float)random.NextDouble();
        line.Length = 0.8f + (float)random.NextDouble() * 0.4f;
    }
}
