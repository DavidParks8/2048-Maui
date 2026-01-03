#if __MACCATALYST__
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;
using TwentyFortyEight.Maui.Components;

namespace TwentyFortyEight.Maui.Platforms.iOS.Handlers;

public class BottomBarHandler : ContentViewHandler
{
    UIVisualEffectView? _blur;

    public static new PropertyMapper<BottomBar, BottomBarHandler> Mapper = new(
        ContentViewHandler.Mapper
    )
    {
        [nameof(BottomBar.IosMaterial)] = MapIosMaterial,
        [nameof(BottomBar.BarHeight)] = MapBarHeight,
    };

    public BottomBarHandler()
        : base(Mapper) { }

    protected override Microsoft.Maui.Platform.ContentView CreatePlatformView()
    {
        var root = base.CreatePlatformView();

        root.BackgroundColor = UIColor.Clear;

        _blur = new UIVisualEffectView
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            Frame = root.Bounds,
            Effect = UIBlurEffect.FromStyle(
                ToBlurStyle(
                    (VirtualView as BottomBar)?.IosMaterial ?? IosMaterialStyle.SystemChromeMaterial
                )
            ),
        };

        root.InsertSubview(_blur, 0);

        var hairline = new UIView { BackgroundColor = UIColor.FromRGBA(200, 200, 200, 40) };
        hairline.TranslatesAutoresizingMaskIntoConstraints = false;
        root.AddSubview(hairline);
        NSLayoutConstraint.ActivateConstraints(
            new[]
            {
                hairline.TopAnchor.ConstraintEqualTo(root.TopAnchor),
                hairline.LeadingAnchor.ConstraintEqualTo(root.LeadingAnchor),
                hairline.TrailingAnchor.ConstraintEqualTo(root.TrailingAnchor),
                hairline.HeightAnchor.ConstraintEqualTo(0.5f),
            }
        );

        return root;
    }

    static void MapIosMaterial(BottomBarHandler handler, BottomBar view)
    {
        if (handler._blur == null)
            return;
        handler._blur.Effect = UIBlurEffect.FromStyle(ToBlurStyle(view.IosMaterial));
    }

    static void MapBarHeight(BottomBarHandler handler, BottomBar view)
    {
        // Height is controlled by MAUI layout.
    }

    static UIBlurEffectStyle ToBlurStyle(IosMaterialStyle style)
    {
        if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
        {
            return style switch
            {
                IosMaterialStyle.SystemUltraThinMaterial =>
                    UIBlurEffectStyle.SystemUltraThinMaterial,
                IosMaterialStyle.SystemThinMaterial => UIBlurEffectStyle.SystemThinMaterial,
                IosMaterialStyle.SystemMaterial => UIBlurEffectStyle.SystemMaterial,
                IosMaterialStyle.SystemThickMaterial => UIBlurEffectStyle.SystemThickMaterial,
                IosMaterialStyle.SystemChromeMaterial => UIBlurEffectStyle.SystemChromeMaterial,
                _ => UIBlurEffectStyle.SystemChromeMaterial,
            };
        }

        return UIBlurEffectStyle.Light;
    }
}
#endif
