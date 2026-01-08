#if IOS || MACCATALYST
using System.Runtime.CompilerServices;
using CoreAnimation;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform;
using TwentyFortyEight.Maui.Components;
using TwentyFortyEight.ViewModels.Services;
using UIKit;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Applies the iOS/macCatalyst "liquid glass" material effect to MAUI views via attached properties.
/// </summary>
/// <remarks>
/// This applier is invoked by the handler mapper system when a view is initialized or when
/// LiquidGlass attached properties change. It manages the lifecycle of UIVisualEffectView
/// instances and handles UIKit translucency constraints.
///
/// Key design principles:
/// - Host views must be explicitly non-opaque (Opaque = false) for glass compositing
/// - MAUI-painted backgrounds are suppressed to prevent occlusion
/// - Corner radius is applied immediately to all layers, including sublayers
/// - Animations respect Reduce Motion accessibility setting
/// </remarks>
internal sealed class LiquidGlassApplier(IReduceMotionService reduceMotionService)
    : ILiquidGlassApplier
{
    /// <summary>
    /// Weak table mapping host UIView instances to their UIVisualEffectView overlays.
    /// Weak references prevent memory leaks when views are recycled.
    /// </summary>
    private readonly ConditionalWeakTable<UIView, UIVisualEffectView> _effects = [];

    /// <summary>
    /// Main entry point: applies or removes the glass effect based on the current attached properties.
    /// </summary>
    /// <remarks>
    /// This method is called by the handler mapper system. It handles all UIKit translucency
    /// constraints required for proper glass compositing:
    /// - Sets host view to non-opaque
    /// - Clears host view background
    /// - Suppresses MAUI background painting
    /// - Applies Border-specific adjustments (BackgroundColor neutralization)
    /// - Ensures corner radius is applied to all layers
    /// </remarks>
    public void Apply(IViewHandler handler, IView view)
    {
        if (handler.PlatformView is not UIView root)
            return;

        if (view is not BindableObject bindable)
            return;

        var effectType = LiquidGlass.GetEffectType(bindable);

        if (effectType == LiquidGlassEffectType.None)
        {
            Remove(root);
            return;
        }

        // Critical translucency setup: UIKit requires host view to be non-opaque
        // for UIVisualEffectView compositing to work correctly.
        root.Opaque = false;
        root.BackgroundColor = UIColor.Clear;
        root.Layer.BackgroundColor = UIColor.Clear.CGColor;

        // IMPORTANT: A single UIView cannot both clip its children to rounded corners
        // and also render an outer shadow. When shadow is enabled, we avoid clipping on
        // the host view and instead rely on rounded/masked sublayers.
        bool enableShadow = LiquidGlass.GetEnableShadowEffect(bindable);
        root.ClipsToBounds = !enableShadow;

        // Prevent MAUI from repainting opaque backgrounds over glass
        if (view is VisualElement ve && ve.Background is not null)
        {
            ve.Background = null;
        }

        // Border-specific: neutralize BackgroundColor so stroke still renders but background doesn't occlude glass
        if (view is Border border)
        {
            border.BackgroundColor = Colors.Transparent;
        }

        var reduceMotionEnabled = reduceMotionService.ShouldReduceMotion();

        var effectView = EnsureEffect(root, view, effectType);
        UpdateCornerRadius(root, view, effectView, reduceMotionEnabled, enableShadow);
        UpdateShadow(root, view);

        // Visual parity: suppress Border stroke/fill that exists as cross-platform fallback
        root.Layer.BorderWidth = 0;
        root.Layer.BorderColor = UIColor.Clear.CGColor;
    }

    /// <summary>
    /// Creates or retrieves the UIVisualEffectView overlay for the host view.
    /// </summary>
    /// <remarks>
    /// Uses a ConditionalWeakTable to cache effect views per host, avoiding duplication.
    /// The effect view is inserted at index 0 (below content, above backing layers) and
    /// constrained to fill the host view entirely.
    /// </remarks>
    private UIVisualEffectView EnsureEffect(
        UIView root,
        IView view,
        LiquidGlassEffectType effectType
    )
    {
        if (_effects.TryGetValue(root, out var existing))
            return existing;

        var effectView = CreateEffectView(view, effectType);
        effectView.TranslatesAutoresizingMaskIntoConstraints = false;
        effectView.UserInteractionEnabled = false;
        effectView.Layer.MasksToBounds = true;

        // Insert glass below content but above any backing layers MAUI may have added
        root.InsertSubview(effectView, 0);

        NSLayoutConstraint.ActivateConstraints(
            [
                effectView.TopAnchor.ConstraintEqualTo(root.TopAnchor),
                effectView.LeadingAnchor.ConstraintEqualTo(root.LeadingAnchor),
                effectView.TrailingAnchor.ConstraintEqualTo(root.TrailingAnchor),
                effectView.BottomAnchor.ConstraintEqualTo(root.BottomAnchor),
            ]
        );

        _effects.Add(root, effectView);
        return effectView;
    }

    /// <summary>
    /// Removes the UIVisualEffectView overlay when glass is disabled (EffectType.None).
    /// </summary>
    /// <remarks>
    /// Cleans up the effect view from the weak table and restores reasonable defaults
    /// to the host layer in case the view is reused.
    /// </remarks>
    private void Remove(UIView root)
    {
        if (!_effects.TryGetValue(root, out var effect))
            return;

        effect.RemoveFromSuperview();
        effect.Dispose();
        _effects.Remove(root);

        // Restore defaults in case this view is reused.
        root.Layer.CornerRadius = 0;
        root.Layer.ShadowOpacity = 0;
    }

    /// <summary>
    /// Creates the appropriate UIVisualEffectView (glass or blur) based on the effect type and iOS version.
    /// </summary>
    /// <remarks>
    /// On iOS 26+, creates a native UIGlassEffect with the specified style (Regular or Clear).
    /// On older iOS versions, falls back to a standard blur effect (UIBlurEffect.SystemMaterial).
    /// The tint color is resolved from the view's background properties.
    /// </remarks>
    private static UIVisualEffectView CreateEffectView(IView view, LiquidGlassEffectType effectType)
    {
        var tint = ResolveTintColor(view);

        if (UIDevice.CurrentDevice.CheckSystemVersion(26, 0))
        {
            var style =
                effectType == LiquidGlassEffectType.Clear
                    ? UIGlassEffectStyle.Clear
                    : UIGlassEffectStyle.Regular;

            var glass = UIGlassEffect.Create(style);
            glass.TintColor = tint;
            glass.Interactive = false;
            return new(glass);
        }

        return new(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial));
    }

    /// <summary>
    /// Resolves the tint color for the glass effect from the view's background.
    /// </summary>
    /// <remarks>
    /// Only translucent (Alpha &lt; 1f) backgrounds are used as explicit tints. Opaque backgrounds
    /// are ignored to avoid incorrect tinting. Falls back to UIColor.Clear for clear-tinted glass.
    /// </remarks>
    private static UIColor ResolveTintColor(IView view)
    {
        if (view is not VisualElement element)
            return UIColor.Clear;

        // XAML uses opaque backgrounds as a cross-platform fallback (Android/WinUI).
        // To keep iOS visual parity with the previous LiquidGlassView (clear-tinted glass),
        // only treat *translucent* backgrounds as an explicit glass tint.
        if (element.Background is SolidColorBrush brush && brush.Color is { } color)
        {
            if (color.Alpha < 1f)
                return color.ToPlatform();
        }

#pragma warning disable CS0618 // BackgroundColor is maintained for compatibility with older XAML usage.
        var backgroundColor = element.BackgroundColor;
        if (backgroundColor is { } bc && bc.Alpha < 1f)
            return bc.ToPlatform();
#pragma warning restore CS0618

        return UIColor.Clear;
    }

    /// <summary>
    /// Applies corner radius to the effect view and all host layer sublayers.
    /// </summary>
    /// <remarks>
    /// Corner radius is applied immediately (even on first layout pass when bounds may be zero)
    /// to prevent rectangular backing layers from showing through with sharp corners.
    /// If bounds are known, the radius is clamped to half the smaller dimension.
    /// All sublayers are also rounded to ensure MAUI-added backing layers don't create
    /// a rectangular outline.
    /// </remarks>
    private static void UpdateCornerRadius(
        UIView root,
        IView view,
        UIVisualEffectView effectView,
        bool reduceMotionEnabled,
        bool enableShadow
    )
    {
        var radius = ResolveCornerRadius(view);
        if (radius <= 0)
            return;

        var r = (nfloat)radius;

        // Optionally clamp to half the smaller dimension if bounds are known
        var bounds = root.Bounds;
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            var max = Math.Min(bounds.Width, bounds.Height) / 2.0;
            r = (nfloat)Math.Min(radius, max);
        }

        // Apply corner radius immediately (don't wait for bounds on first pass)
        // This ensures the rectangular backing layer doesn't show through with sharp corners
        AnimateIfAllowed(
            () =>
            {
                effectView.Layer.CornerRadius = r;
                root.Layer.CornerRadius = r;

                // When shadows are enabled we must not mask the host layer.
                // Masking is handled by sublayers (including the effect overlay).
                root.Layer.MasksToBounds = !enableShadow;

                // Also apply to all sublayers to catch any MAUI-added backing layers
                foreach (var sublayer in root.Layer.Sublayers ?? [])
                {
                    sublayer.CornerRadius = r;
                    sublayer.MasksToBounds = true;
                }
            },
            reduceMotionEnabled
        );
    }

    /// <summary>
    /// Resolves the corner radius from attached properties or the view's StrokeShape.
    /// </summary>
    /// <remarks>
    /// Priority: attached CornerRadius property &gt; Border.StrokeShape (RoundRectangle) &gt; 0.
    /// For RoundRectangle, returns the maximum of the four corner values.
    /// </remarks>
    private static double ResolveCornerRadius(IView view)
    {
        if (view is BindableObject bindable)
        {
            var attachedRadius = LiquidGlass.GetCornerRadius(bindable);
            if (attachedRadius > 0)
                return attachedRadius;
        }

        if (view is Border border && border.StrokeShape is RoundRectangle round)
        {
            var cr = round.CornerRadius;
            return Math.Max(
                Math.Max(cr.TopLeft, cr.TopRight),
                Math.Max(cr.BottomLeft, cr.BottomRight)
            );
        }

        return 0;
    }

    /// <summary>
    /// Applies a subtle drop shadow to the host view if enabled via attached properties.
    /// </summary>
    /// <remarks>
    /// Shadow is applied to the host layer (not the effect view) with fixed opacity and radius.
    /// ShadowOpacity is set to 0 if shadow is disabled.
    /// </remarks>
    private static void UpdateShadow(UIView root, IView view)
    {
        if (view is not BindableObject bindable)
            return;

        if (!LiquidGlass.GetEnableShadowEffect(bindable))
        {
            root.Layer.ShadowOpacity = 0;
            root.Layer.ShadowPath = null;
            return;
        }

        root.ClipsToBounds = false;
        root.Layer.MasksToBounds = false;
        root.Layer.ShadowColor = UIColor.Black.CGColor;
        root.Layer.ShadowOpacity = 0.08f;

        // Keep the shadow subtle/tight so it doesn't get clipped by common parents
        // like ScrollView content containers.
        root.Layer.ShadowRadius = 10;
        root.Layer.ShadowOffset = new(0, 4);

        // Provide a rounded shadow path when bounds are known for better performance and shape.
        var cornerRadius = (nfloat)ResolveCornerRadius(view);
        if (root.Bounds.Width > 0 && root.Bounds.Height > 0 && cornerRadius > 0)
        {
            root.Layer.ShadowPath = UIBezierPath.FromRoundedRect(root.Bounds, cornerRadius).CGPath;
        }
        else
        {
            root.Layer.ShadowPath = null;
        }
    }

    /// <summary>
    /// Wraps the update action in a CATransaction, respecting the Reduce Motion accessibility setting.
    /// </summary>
    /// <remarks>
    /// If Reduce Motion is enabled, the action executes immediately without animation.
    /// Otherwise, the update is wrapped in a CATransaction with a 0.15 second duration,
    /// allowing smooth animation at the display's native refresh rate (including ProMotion 120 Hz).
    /// </remarks>
    private static void AnimateIfAllowed(Action update, bool reduceMotionEnabled)
    {
        if (reduceMotionEnabled)
        {
            update();
            return;
        }

        CATransaction.Begin();
        CATransaction.AnimationDuration = 0.15;
        update();
        CATransaction.Commit();
    }
}
#endif
