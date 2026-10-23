using Microsoft.Maui.Platform;
using TwentyFortyEight.ViewModels.Helpers;
using UIKit;

namespace TwentyFortyEight.Maui;

public partial class MainPage
{
    private UIHingeInteraction? _hingeInteraction;
    private IUITraitChangeRegistration? _contentSizeRegistration;
    private UIView? _displayRegionView;

    partial void ConnectDisplayRegions()
    {
        if (RootLayout.Handler?.PlatformView is not UIView view || _displayRegionView == view)
        {
            return;
        }

        DisconnectDisplayRegions();
        _displayRegionView = view;
        // Dynamic Type resizes the cards without moving their absolute bounds.
        _contentSizeRegistration =
            view.RegisterForTraitChanges<UITraitPreferredContentSizeCategory>(
                (_, _) => OnConsoleMeasureInvalidated(null, EventArgs.Empty)
            );
        if (OperatingSystem.IsIOSVersionAtLeast(27, 1))
        {
            _hingeInteraction = new UIHingeInteraction(
                (_, _) => Dispatcher.Dispatch(() => _displayRegionView?.SetNeedsLayout())
            );
            view.AddInteraction(_hingeInteraction);
        }
    }

    partial void DisconnectDisplayRegions()
    {
        if (_contentSizeRegistration != null)
        {
            _displayRegionView?.UnregisterForTraitChanges(_contentSizeRegistration);
            _contentSizeRegistration = null;
        }

        if (OperatingSystem.IsIOSVersionAtLeast(27, 1) && _hingeInteraction != null)
        {
            _displayRegionView?.RemoveInteraction(_hingeInteraction);
            _hingeInteraction.Dispose();
            _hingeInteraction = null;
        }

        _displayRegionView = null;
    }

    partial void UpdateDisplayRegions()
    {
        if (
            !OperatingSystem.IsIOSVersionAtLeast(27, 1)
            || RootLayout.Handler?.PlatformView is not UIView view
            || view.Bounds.Width <= 0
            || view.Bounds.Height <= 0
        )
        {
            return;
        }

        var available = view.Bounds.ToRectangle();
        Rect? fold = null;
        using var divisionKind = UIViewReservedRegionKind.CreateDivision();
        using var occlusionKind = UIViewReservedRegionKind.CreateOcclusion();
        var divisions = view.GetReservedRegions(
            divisionKind,
            UIViewReservedRegionQueryOptions.IncludeInactive
        );
        bool hasFoldableDisplay = divisions.Length > 0;
        foreach (var region in divisions)
        {
            if (region.Active)
            {
                fold = GetReservedBounds(region);
                break;
            }
        }

        foreach (var region in view.GetReservedRegions(occlusionKind))
        {
            if (region.Active)
            {
                available = BoardLayoutCalculator.ExcludeReservedRegion(
                    available,
                    GetReservedBounds(region)
                );
            }
        }

        if (
            _clearDisplayBounds == available
            && _activeFoldBounds == fold
            && _hasFoldableDisplay == hasFoldableDisplay
        )
        {
            return;
        }

        if (
            _hasFoldableDisplay
            && hasFoldableDisplay
            && _activeFoldBounds.HasValue != fold.HasValue
        )
        {
            BeginBoardPostureTransition();
        }

        _animationCts?.Cancel();
        _swipePreviewInteractionService.Reset();
        _clearDisplayBounds = available;
        _activeFoldBounds = fold;
        _hasFoldableDisplay = hasFoldableDisplay;
        UpdateResponsiveLayout();
    }

    partial void PreparePostureLayout()
    {
        _displayRegionView?.SetNeedsLayout();
        _displayRegionView?.LayoutIfNeeded();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("ios27.1")]
    private static Rect GetReservedBounds(UIViewReservedRegion region)
    {
        var frame = region.Frame;
        var margins = region.Margins;
        return new Rect(
            frame.X - margins.Left,
            frame.Y - margins.Top,
            frame.Width + margins.Left + margins.Right,
            frame.Height + margins.Top + margins.Bottom
        );
    }

    partial void UpdateBottomSheetBounds(Rect? bounds)
    {
        Rect? windowBounds = null;
        if (
            bounds is Rect rect
            && RootLayout.Handler?.PlatformView is UIView view
            && view.Window is { } window
        )
        {
            windowBounds = view.ConvertRectToView(rect.ToCGRect(), window).ToRectangle();
        }

        _windowOverlayService.SetBottomSheetBounds(windowBounds);
    }
}
