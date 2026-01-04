#if IOS || MACCATALYST
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;
using TwentyFortyEight.Maui.Components;

namespace TwentyFortyEight.Maui.Platforms.iOS.Handlers;

public class VictoryModalOverlayHandler : ContentViewHandler
{
    UIVisualEffectView? _blur;

    public static new PropertyMapper<VictoryModalOverlay, VictoryModalOverlayHandler> Mapper = new(
        ContentViewHandler.Mapper
    )
    {
        [nameof(VictoryModalOverlay.IosMaterial)] = MapIosMaterial,
    };

    public VictoryModalOverlayHandler()
        : base(Mapper) { }

    protected override Microsoft.Maui.Platform.ContentView CreatePlatformView()
    {
        var root = base.CreatePlatformView();

        // Make the ContentView background transparent so blur shows through
        root.BackgroundColor = UIColor.Clear;

        _blur = new UIVisualEffectView
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            Frame = root.Bounds,
            Effect = UIBlurEffect.FromStyle(
                IosMaterialHelper.ToBlurStyle(
                    (VirtualView as VictoryModalOverlay)?.IosMaterial ?? IosMaterialStyle.SystemThickMaterial
                )
            ),
        };

        // Put material behind content.
        root.InsertSubview(_blur, 0);

        return root;
    }

    static void MapIosMaterial(VictoryModalOverlayHandler handler, VictoryModalOverlay view)
    {
        if (handler._blur == null)
            return;
        handler._blur.Effect = UIBlurEffect.FromStyle(IosMaterialHelper.ToBlurStyle(view.IosMaterial));
    }
}
#endif
