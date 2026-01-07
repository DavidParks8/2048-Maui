using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

public sealed class WallOverlayRenderer : IWallOverlayRenderer
{
    private const string WallGlowAnimationKey = "WallGlowAnimation";

    private readonly IReduceMotionService _reduceMotionService;

    private GraphicsView? _wallDivider;
    private StripedWallDrawable? _wallDividerDrawable;

    public WallOverlayRenderer(IReduceMotionService reduceMotionService)
    {
        _reduceMotionService = reduceMotionService;
    }

    public void Reset(VisualElement animationHost)
    {
        StopWallGlow(animationHost);
        _wallDivider = null;
        _wallDividerDrawable = null;
    }

    public void Update(
        Grid gameBoard,
        AbsoluteLayout overlayLayer,
        int boardSize,
        WallSegment? wall,
        VisualElement animationHost
    )
    {
        EnsureWallDividerCreated(overlayLayer);

        if (_wallDivider is null || _wallDividerDrawable is null)
        {
            return;
        }

        var wallColor = GetWallColor();
        _wallDividerDrawable.BaseColor = wallColor;
        _wallDividerDrawable.StripeColor = GetWallStripeColor();
        _wallDividerDrawable.ShadowColor = GetWallShadowColor();
        _wallDivider.Invalidate();

        if (wall is null)
        {
            _wallDivider.IsVisible = false;
            StopWallGlow(animationHost);
            return;
        }

        // Need a measured size to place the wall accurately.
        if (gameBoard.Width <= 0 || gameBoard.Height <= 0)
        {
            _wallDivider.IsVisible = false;
            StopWallGlow(animationHost);
            return;
        }

        if (boardSize <= 1)
        {
            _wallDivider.IsVisible = false;
            StopWallGlow(animationHost);
            return;
        }

        var spacingX = gameBoard.ColumnSpacing;
        var spacingY = gameBoard.RowSpacing;
        if (spacingX <= 0)
        {
            spacingX = 10;
        }

        if (spacingY <= 0)
        {
            spacingY = 10;
        }

        var cellWidth = (gameBoard.Width - (boardSize - 1) * spacingX) / boardSize;
        var cellHeight = (gameBoard.Height - (boardSize - 1) * spacingY) / boardSize;
        if (cellWidth <= 0 || cellHeight <= 0)
        {
            _wallDivider.IsVisible = false;
            StopWallGlow(animationHost);
            return;
        }

        _wallDividerDrawable.GlowColor = GetWallGlowColor(_wallDividerDrawable.BaseColor);

        // The wall divider view is only as thick as the grid spacing. Any glow/shadow that
        // extends beyond its bounds will be clipped, so we inflate the view bounds to
        // create room for the glow halo.
        var dividerThickness = (float)(
            wall.Orientation == WallOrientation.Vertical ? spacingX : spacingY
        );
        var glowPadding = Math.Clamp(dividerThickness * 1.6f, 8f, 18f);
        _wallDividerDrawable.GlowPadding = glowPadding;

        if (wall.Orientation == WallOrientation.Vertical)
        {
            var x = (wall.Divider + 1) * cellWidth + wall.Divider * spacingX;
            var y = wall.Start * cellHeight + wall.Start * spacingY;
            var height = wall.Length * cellHeight + (wall.Length - 1) * spacingY;
            AbsoluteLayout.SetLayoutBounds(
                _wallDivider,
                new Rect(
                    x - glowPadding,
                    y - glowPadding,
                    spacingX + glowPadding * 2,
                    height + glowPadding * 2
                )
            );
        }
        else
        {
            var x = wall.Start * cellWidth + wall.Start * spacingX;
            var y = (wall.Divider + 1) * cellHeight + wall.Divider * spacingY;
            var width = wall.Length * cellWidth + (wall.Length - 1) * spacingX;
            AbsoluteLayout.SetLayoutBounds(
                _wallDivider,
                new Rect(
                    x - glowPadding,
                    y - glowPadding,
                    width + glowPadding * 2,
                    spacingY + glowPadding * 2
                )
            );
        }

        _wallDivider.IsVisible = true;
        StartWallGlow(animationHost);
    }

    private void EnsureWallDividerCreated(AbsoluteLayout overlayLayer)
    {
        if (_wallDivider != null)
        {
            // Re-attach if the overlay layer was rebuilt.
            if (_wallDivider.Parent != overlayLayer)
            {
                overlayLayer.Children.Add(_wallDivider);
            }

            return;
        }

        var wallColor = GetWallColor();
        _wallDividerDrawable = new StripedWallDrawable
        {
            BaseColor = wallColor,
            StripeColor = GetWallStripeColor(),
            ShadowColor = GetWallShadowColor(),
            GlowColor = GetWallGlowColor(wallColor),
        };

        _wallDivider = new GraphicsView
        {
            IsVisible = false,
            InputTransparent = true,
            Drawable = _wallDividerDrawable,
        };

        AbsoluteLayout.SetLayoutFlags(
            _wallDivider,
            Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None
        );
        overlayLayer.Children.Add(_wallDivider);
    }

    private static Color GetWallColor()
    {
        var app = Application.Current;
        if (app == null)
        {
            return Colors.Gray;
        }

        // Gray600 is too close to GamePanelBackgroundDark (#3c3a32), making walls hard to see.
        // Use existing "native" text colors for better contrast (no new colors introduced).
        var key =
            app.RequestedTheme == AppTheme.Dark
                ? "NativeTextTertiaryDark"
                : "NativeTextTertiaryLight";
        if (app.Resources.TryGetValue(key, out var value) && value is Color color)
        {
            return color;
        }

        // Fallback to grays if the native keys aren't available.
        var fallbackKey = app.RequestedTheme == AppTheme.Dark ? "Gray300" : "Gray500";
        if (
            app.Resources.TryGetValue(fallbackKey, out var fallbackValue)
            && fallbackValue is Color fallbackColor
        )
        {
            return fallbackColor;
        }

        return Colors.Gray;
    }

    private static Color GetWallGlowColor(Color _)
    {
        // "Lightsaber" style glow: explicitly red.
        // Alpha varies by theme to avoid overwhelming the light theme.
        var app = Application.Current;
        if (app?.RequestedTheme == AppTheme.Dark)
        {
            return Colors.Purple.WithAlpha(0.75f);
        }

        return Colors.Purple.WithAlpha(0.45f);
    }

    private void StartWallGlow(VisualElement animationHost)
    {
        if (_wallDivider is null || _wallDividerDrawable is null)
        {
            return;
        }

        if (!_wallDivider.IsVisible)
        {
            StopWallGlow(animationHost);
            return;
        }

        // Respect accessibility setting: keep a static glow without motion.
        if (_reduceMotionService.ShouldReduceMotion())
        {
            _wallDividerDrawable.GlowPhase = 0.55f;
            _wallDivider.Invalidate();
            StopWallGlow(animationHost);
            return;
        }

        if (animationHost.AnimationIsRunning(WallGlowAnimationKey))
        {
            return;
        }

        // Pulse the glow (0→1→0) continuously.
        var pulse = new Animation
        {
            {
                0.0,
                0.5,
                new Animation(
                    v =>
                    {
                        _wallDividerDrawable.GlowPhase = (float)v;
                        _wallDivider.Invalidate();
                    },
                    0.10,
                    1.00,
                    easing: Easing.SinInOut
                )
            },
            {
                0.5,
                1.0,
                new Animation(
                    v =>
                    {
                        _wallDividerDrawable.GlowPhase = (float)v;
                        _wallDivider.Invalidate();
                    },
                    1.00,
                    0.10,
                    easing: Easing.SinInOut
                )
            },
        };

        pulse.Commit(
            animationHost,
            WallGlowAnimationKey,
            rate: 16,
            length: 1400,
            finished: null,
            repeat: () => true
        );
    }

    private static void StopWallGlow(VisualElement animationHost)
    {
        animationHost.AbortAnimation(WallGlowAnimationKey);
    }

    private static Color GetWallStripeColor()
    {
        // Stripes need strong contrast for colorblind accessibility.
        // Use existing palette colors (Black/White) with alpha rather than introducing new colors.
        var app = Application.Current;
        if (app?.RequestedTheme == AppTheme.Dark)
        {
            return Colors.White.WithAlpha(0.55f);
        }

        return Colors.Black.WithAlpha(0.35f);
    }

    private static Color GetWallShadowColor()
    {
        // Keep the shadow subtle but visible in both themes.
        var app = Application.Current;
        if (app?.RequestedTheme == AppTheme.Dark)
        {
            return Colors.Black.WithAlpha(0.85f);
        }

        return Colors.Black.WithAlpha(0.35f);
    }

    private sealed class StripedWallDrawable : IDrawable
    {
        public Color BaseColor { get; set; } = Colors.Gray;
        public Color StripeColor { get; set; } = Colors.Gray;
        public Color ShadowColor { get; set; } = Colors.Black.WithAlpha(0.35f);
        public Color GlowColor { get; set; } = Colors.Transparent;

        /// <summary>
        /// Extra padding (in pixels) reserved in the view bounds for glow/shadow.
        /// The actual wall "core" is drawn inset by this amount.
        /// </summary>
        public float GlowPadding { get; set; } = 12f;

        /// <summary>
        /// Animated phase (0..1) driving the "lightsaber" glow intensity.
        /// </summary>
        public float GlowPhase { get; set; }

        // Tuned to remain visible even when the divider is thin (e.g., 5-10px).
        public float StripeSize { get; set; } = 3f;

        private static void FillRoundedRect(ICanvas canvas, RectF rect, float radius, Color color)
        {
            PathF path = new();
            path.AppendRoundedRectangle(rect, radius);
            canvas.FillColor = color;
            canvas.FillPath(path);
        }

        private static void DrawSoftHalo(
            ICanvas canvas,
            RectF rect,
            float baseRadius,
            Color color,
            float maxExpand,
            float maxAlpha,
            float offsetX,
            float offsetY,
            int steps
        )
        {
            if (steps <= 0 || maxExpand <= 0 || maxAlpha <= 0)
            {
                return;
            }

            // Outer -> inner. Inner layers are brighter; outer are softer.
            for (int i = steps; i >= 1; i--)
            {
                float t = i / (float)steps; // 1..(1/steps)
                float expand = maxExpand * t;

                // Brighten toward the center.
                float alpha = Math.Min(maxAlpha * (0.08f / t), maxAlpha);

                var inflated = new RectF(
                    rect.X - expand + offsetX,
                    rect.Y - expand + offsetY,
                    rect.Width + 2 * expand,
                    rect.Height + 2 * expand
                );

                FillRoundedRect(canvas, inflated, baseRadius + expand, color.WithAlpha(alpha));
            }
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
            {
                return;
            }

            var pad = Math.Clamp(
                GlowPadding,
                0f,
                MathF.Min(dirtyRect.Width, dirtyRect.Height) / 2f
            );
            var coreRect = new RectF(
                dirtyRect.X + pad,
                dirtyRect.Y + pad,
                dirtyRect.Width - pad * 2,
                dirtyRect.Height - pad * 2
            );

            if (coreRect.Width <= 0 || coreRect.Height <= 0)
            {
                coreRect = dirtyRect;
                pad = 0f;
            }

            // Round the ends of the divider (pill shape) by clipping.
            // Using half the thickness ensures fully rounded caps.
            var radius = MathF.Min(coreRect.Width, coreRect.Height) / 2f;
            PathF clipPath = new();
            clipPath.AppendRoundedRectangle(coreRect, radius);

            // Animated colored glow behind the divider.
            // NOTE: On some platforms (notably MacCatalyst), ICanvas.SetShadow in GraphicsView
            // can be a no-op. Use a manual multi-pass halo so the effect is always visible.
            var phase = Math.Clamp(GlowPhase, 0f, 1f);
            if (GlowColor != Colors.Transparent)
            {
                var maxAlpha = Math.Clamp(0.18f + 0.45f * phase, 0f, 0.70f);
                var maxExpand = Math.Clamp(Math.Min(pad, radius * (2.0f + 2.0f * phase)), 0f, 18f);
                DrawSoftHalo(
                    canvas,
                    coreRect,
                    radius,
                    GlowColor,
                    maxExpand: maxExpand,
                    maxAlpha: maxAlpha,
                    offsetX: 0,
                    offsetY: 0,
                    steps: 6
                );
            }

            // Subtle drop shadow behind the divider.
            var shadowOffsetY = Math.Clamp(radius * 0.20f, 1f, 3f);
            DrawSoftHalo(
                canvas,
                coreRect,
                radius,
                ShadowColor,
                maxExpand: Math.Clamp(Math.Min(pad, radius * 1.1f), 0f, 9f),
                maxAlpha: Math.Clamp(ShadowColor.Alpha, 0f, 0.50f),
                offsetX: 0,
                offsetY: shadowOffsetY,
                steps: 4
            );

            canvas.SaveState();
            canvas.ClipPath(clipPath);

            canvas.FillColor = BaseColor;
            canvas.FillRectangle(coreRect);

            if (StripeSize > 0)
            {
                canvas.FillColor = StripeColor;

                // If the wall is vertical (thin width, tall height), draw horizontal bands.
                // If the wall is horizontal (wide width, thin height), draw vertical bands.
                if (coreRect.Width <= coreRect.Height)
                {
                    float y = coreRect.Y;
                    bool draw = true;
                    while (y < coreRect.Bottom)
                    {
                        if (draw)
                        {
                            var h = Math.Min(StripeSize, coreRect.Bottom - y);
                            canvas.FillRectangle(coreRect.X, y, coreRect.Width, h);
                        }

                        y += StripeSize;
                        draw = !draw;
                    }
                }
                else
                {
                    float x = coreRect.X;
                    bool draw = true;
                    while (x < coreRect.Right)
                    {
                        if (draw)
                        {
                            var w = Math.Min(StripeSize, coreRect.Right - x);
                            canvas.FillRectangle(x, coreRect.Y, w, coreRect.Height);
                        }

                        x += StripeSize;
                        draw = !draw;
                    }
                }
            }

            canvas.RestoreState();
        }
    }
}
