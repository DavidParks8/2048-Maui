#if IOS || MACCATALYST
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using TwentyFortyEight.Maui.Components;
using UIKit;
using ContentView = Microsoft.Maui.Platform.ContentView;

namespace TwentyFortyEight.Maui.Platforms.iOS.Handlers;

public class LiquidGlassViewHandler : ContentViewHandler
{
    UIVisualEffectView? _effectView;
    ContentView? _root;

    // Hide the base ContentViewHandler.Mapper so we can add LiquidGlassView-specific property mappings
    // (EffectType/CornerRadius/Shadow + Background*) that drive updates to the native UIVisualEffectView.
    public static readonly new PropertyMapper<LiquidGlassView, LiquidGlassViewHandler> Mapper = new(
        ContentViewHandler.Mapper
    )
    {
        [nameof(LiquidGlassView.EffectType)] = MapEffect,
        [nameof(LiquidGlassView.CornerRadius)] = MapCornerRadius,
        [nameof(LiquidGlassView.EnableShadowEffect)] = MapShadow,
        [nameof(VisualElement.BackgroundColor)] = MapEffect,
        [nameof(VisualElement.Background)] = MapEffect,
    };

    public LiquidGlassViewHandler()
        : base(Mapper) { }

    protected override ContentView CreatePlatformView()
    {
        var root = base.CreatePlatformView();
        root.BackgroundColor = UIColor.Clear;
        root.ClipsToBounds = true;
        return root;
    }

    protected override void ConnectHandler(ContentView platformView)
    {
        base.ConnectHandler(platformView);
        _root = platformView;
        EnsureEffect(platformView);
    }

    protected override void DisconnectHandler(ContentView platformView)
    {
        RemoveEffect();
        _root = null;
        base.DisconnectHandler(platformView);
    }

    public override void PlatformArrange(Rect frame)
    {
        base.PlatformArrange(frame);
        if (_root != null)
            UpdateCornerRadius(_root);
    }

    private static void MapEffect(LiquidGlassViewHandler handler, LiquidGlassView view)
    {
        if (handler._root == null)
            return;

        handler.EnsureEffect(handler._root);
        handler.UpdateCornerRadius(handler._root);
    }

    private static void MapCornerRadius(LiquidGlassViewHandler handler, LiquidGlassView view)
    {
        if (handler._root != null)
            handler.UpdateCornerRadius(handler._root);
    }

    private static void MapShadow(LiquidGlassViewHandler handler, LiquidGlassView view)
    {
        if (handler._root != null)
            handler.UpdateShadow(handler._root);
    }

    private void EnsureEffect(ContentView root)
    {
        RemoveEffect();

        if (VirtualView is not LiquidGlassView view)
            return;

        var tint = ResolveTintColor(view);

        _effectView = CreateEffectView(view, tint);
        if (_effectView == null)
            return;

        _effectView.TranslatesAutoresizingMaskIntoConstraints = false;
        _effectView.UserInteractionEnabled = false;
        _effectView.Layer.MasksToBounds = true;

        root.InsertSubview(_effectView, 0);

        NSLayoutConstraint.ActivateConstraints(
            [
                _effectView.TopAnchor.ConstraintEqualTo(root.TopAnchor),
                _effectView.LeadingAnchor.ConstraintEqualTo(root.LeadingAnchor),
                _effectView.TrailingAnchor.ConstraintEqualTo(root.TrailingAnchor),
                _effectView.BottomAnchor.ConstraintEqualTo(root.BottomAnchor),
            ]
        );

        UpdateCornerRadius(root);
        UpdateShadow(root);
    }

    private void RemoveEffect()
    {
        if (_effectView == null)
            return;

        _effectView.RemoveFromSuperview();
        _effectView.Dispose();
        _effectView = null;
    }

    private static UIVisualEffectView? CreateEffectView(LiquidGlassView view, UIColor tint)
    {
        // iOS/macCatalyst 26+ native liquid glass
        if (UIDevice.CurrentDevice.CheckSystemVersion(26, 0))
        {
            var style =
                view.EffectType == LiquidGlassEffectType.Clear
                    ? UIGlassEffectStyle.Clear
                    : UIGlassEffectStyle.Regular;
            var effect = UIGlassEffect.Create(style);
            effect.TintColor = tint;
            effect.Interactive = false;
            return new(effect);
        }

        // Fallback for older OS versions: standard blur
        var blur = UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial);
        return new(blur);
    }

    private static UIColor ResolveTintColor(LiquidGlassView view) =>
        // Prefer BackgroundColor if set, else transparent.
        // (This is intentionally conservative; we don't try to interpret gradients.)
        view.BackgroundColor?.ToPlatform() ?? UIColor.Clear;

    private void UpdateCornerRadius(ContentView root)
    {
        if (_effectView?.Layer == null || VirtualView is not LiquidGlassView view)
            return;

        var bounds = root.Bounds;
        var width = bounds.Width;
        var height = bounds.Height;
        if (width <= 0 || height <= 0)
            return;

        var maxRadius = Math.Min(width, height) / 2.0;
        var radius = Math.Min(view.CornerRadius, maxRadius);
        _effectView.Layer.CornerRadius = (nfloat)radius;
        root.Layer.CornerRadius = (nfloat)radius;
        root.Layer.MasksToBounds = true;
    }

    private void UpdateShadow(ContentView root)
    {
        if (root.Layer == null || VirtualView is not LiquidGlassView view)
            return;

        if (!view.EnableShadowEffect)
        {
            root.Layer.ShadowOpacity = 0f;
            return;
        }

        // Keep clipping on the effect view; apply shadow to the host layer.
        root.Layer.MasksToBounds = false;
        root.Layer.ShadowColor = UIColor.Black.CGColor;
        root.Layer.ShadowOpacity = 0.08f;
        root.Layer.ShadowRadius = (nfloat)view.CornerRadius;
        root.Layer.ShadowOffset = new CoreGraphics.CGSize(5, 5);
    }
}
#endif
