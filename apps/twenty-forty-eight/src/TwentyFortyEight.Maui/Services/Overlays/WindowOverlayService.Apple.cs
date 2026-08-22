#if IOS || MACCATALYST
using Microsoft.Maui.Platform;

namespace TwentyFortyEight.Maui.Services;

public partial class WindowOverlayService
{
    private UIKit.UIView? _overlayView;
    private IDisposable? _boundsObserver;
    private ContentPage? _wrapperPage;

    private partial bool TryShowOverlayNative()
    {
        if (_currentSheet == null)
        {
            return false;
        }

        var window = GetKeyWindow();
        if (window == null)
        {
            return false;
        }

        var mauiContext = Application.Current?.Windows.FirstOrDefault()?.Handler?.MauiContext;
        if (mauiContext == null)
        {
            return false;
        }

        _wrapperPage = new ContentPage
        {
            Content = _currentSheet,
            SafeAreaEdges = SafeAreaEdges.None,
        };

        _overlayView = _wrapperPage.ToPlatform(mauiContext);
        if (_overlayView == null)
        {
            return false;
        }

        _overlayView.Frame = window.Bounds;
        _overlayView.AutoresizingMask =
            UIKit.UIViewAutoresizing.FlexibleWidth | UIKit.UIViewAutoresizing.FlexibleHeight;
        window.AddSubview(_overlayView);

        _boundsObserver = window.AddObserver(
            "bounds",
            Foundation.NSKeyValueObservingOptions.New,
            change =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_overlayView != null)
                    {
                        var newBounds = window.Bounds;
                        _overlayView.Frame = newBounds;
                        _overlayView.SetNeedsLayout();
                        _overlayView.LayoutIfNeeded();

                        _wrapperPage?.Arrange(new Rect(0, 0, newBounds.Width, newBounds.Height));
                        _currentSheet?.InvalidateMeasure();
                    }
                });
            }
        );

        return true;
    }

    private partial void CleanupNativeOverlay()
    {
        _boundsObserver?.Dispose();
        _boundsObserver = null;

        if (_overlayView != null)
        {
            _overlayView.RemoveFromSuperview();
            _overlayView = null;
        }

        _wrapperPage = null;
    }

    private static UIKit.UIWindow? GetKeyWindow()
    {
        var scenes = UIKit.UIApplication.SharedApplication.ConnectedScenes;
        foreach (var scene in scenes)
        {
            if (scene is UIKit.UIWindowScene windowScene)
            {
                foreach (var window in windowScene.Windows)
                {
                    if (window.IsKeyWindow)
                    {
                        return window;
                    }
                }
            }
        }

        return null;
    }
}
#endif
