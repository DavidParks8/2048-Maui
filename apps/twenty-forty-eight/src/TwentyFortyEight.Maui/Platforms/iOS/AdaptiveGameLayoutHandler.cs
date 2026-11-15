using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using TwentyFortyEight.Maui.Components;

namespace TwentyFortyEight.Maui.Platforms.iOS;

public class AdaptiveGameLayoutHandler : LayoutHandler
{
    protected override LayoutView CreatePlatformView() =>
        new NativeGameLayout((AdaptiveGameLayout)VirtualView) { CrossPlatformLayout = VirtualView };

    private sealed class NativeGameLayout(AdaptiveGameLayout layout) : LayoutView
    {
        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            // UIKit settles reserved regions during layout, not the hinge callback.
            layout.NotifyNativeLayoutUpdated();
        }
    }
}
