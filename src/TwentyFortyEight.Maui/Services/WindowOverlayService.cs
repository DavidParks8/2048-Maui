using System.Windows.Input;
using TwentyFortyEight.Maui.Components;
#if IOS || MACCATALYST
using Microsoft.Maui.Platform;
#endif

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for showing overlays at the app/window level.
/// On iOS/macCatalyst, uses platform APIs to cover the entire screen including nav bars.
/// On other platforms, adds the overlay to the current page's visual tree.
/// </summary>
public interface IWindowOverlayService
{
    /// <summary>
    /// Shows the bottom sheet overlay with the specified content.
    /// The sheet will automatically close when the user taps the scrim, close button, or drags it down.
    /// </summary>
    void ShowBottomSheet(string title, View content);

    /// <summary>
    /// Hides the currently visible bottom sheet.
    /// </summary>
    void HideBottomSheet();

    /// <summary>
    /// Gets whether a bottom sheet is currently visible.
    /// </summary>
    bool IsBottomSheetVisible { get; }

    /// <summary>
    /// Raised when the bottom sheet is dismissed (by user or programmatically).
    /// </summary>
    event EventHandler? BottomSheetDismissed;
}

public class WindowOverlayService : IWindowOverlayService
{
    private BottomSheetOverlay? _currentSheet;
    private Layout? _hostLayout;

#if IOS || MACCATALYST
    private UIKit.UIView? _overlayView;
    private IDisposable? _boundsObserver;
    private ContentPage? _wrapperPage;
#endif

    public bool IsBottomSheetVisible => _currentSheet?.IsVisible == true;

    public event EventHandler? BottomSheetDismissed;

    public void ShowBottomSheet(string title, View content)
    {
        // Remove any existing sheet
        HideBottomSheet();

        // Create command that closes the sheet
        var dismissCommand = new Command(() => HideBottomSheet());

        // Create new sheet
        _currentSheet = new BottomSheetOverlay
        {
            Title = title,
            SheetContent = content,
            CloseCommand = dismissCommand,
            ScrimTapCommand = dismissCommand,
            IsVisible = true,
        };

#if IOS || MACCATALYST
        if (!TryShowOverlayNative())
        {
            ShowOverlayInPage();
        }
#else
        ShowOverlayInPage();
#endif
    }

#if IOS || MACCATALYST
    private bool TryShowOverlayNative()
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

        // Wrap in a ContentPage for proper sizing, ignore safe area so scrim covers entire screen
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

        // Observe window bounds changes for resize handling
        _boundsObserver = window.AddObserver(
            "bounds",
            Foundation.NSKeyValueObservingOptions.New,
            change =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_overlayView != null && window != null)
                    {
                        var newBounds = window.Bounds;
                        _overlayView.Frame = newBounds;
                        _overlayView.SetNeedsLayout();
                        _overlayView.LayoutIfNeeded();

                        if (_wrapperPage != null)
                        {
                            _wrapperPage.Arrange(new Rect(0, 0, newBounds.Width, newBounds.Height));
                        }

                        _currentSheet?.InvalidateMeasure();
                    }
                });
            }
        );

        return true;
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
#endif

    /// <summary>
    /// Cross-platform fallback: adds the overlay to the current page's root layout.
    /// Works on all platforms but won't cover the navigation bar.
    /// </summary>
    private void ShowOverlayInPage()
    {
        if (_currentSheet == null)
        {
            return;
        }

        // Find the current page and its root layout
        var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (currentPage is Shell shell)
        {
            currentPage = shell.CurrentPage;
        }

        // Navigate to the actual content page if wrapped in navigation
        if (currentPage is NavigationPage navPage)
        {
            currentPage = navPage.CurrentPage;
        }

        // Find a suitable layout to host the overlay
        _hostLayout = FindHostLayout(currentPage);
        if (_hostLayout == null)
        {
            return;
        }

        // Add the overlay to fill the layout
        AbsoluteLayout.SetLayoutBounds(_currentSheet, new Rect(0, 0, 1, 1));
        AbsoluteLayout.SetLayoutFlags(
            _currentSheet,
            Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All
        );

        if (_hostLayout is AbsoluteLayout absoluteLayout)
        {
            absoluteLayout.Children.Add(_currentSheet);
        }
        else if (_hostLayout is Grid grid)
        {
            // For Grid, span all rows/columns and set high ZIndex
            Grid.SetRowSpan(_currentSheet, Math.Max(1, grid.RowDefinitions.Count));
            Grid.SetColumnSpan(_currentSheet, Math.Max(1, grid.ColumnDefinitions.Count));
            _currentSheet.ZIndex = 1000;
            grid.Children.Add(_currentSheet);
        }
    }

    private static Layout? FindHostLayout(Page? page)
    {
        if (page is not ContentPage contentPage)
        {
            return null;
        }

        // If the page content is already an AbsoluteLayout or Grid, use it directly
        if (contentPage.Content is AbsoluteLayout absoluteLayout)
        {
            return absoluteLayout;
        }

        if (contentPage.Content is Grid grid)
        {
            return grid;
        }

        // Otherwise, wrap the existing content in a Grid
        if (contentPage.Content is View existingContent)
        {
            var wrapperGrid = new Grid();
            contentPage.Content = null;
            wrapperGrid.Children.Add(existingContent);
            contentPage.Content = wrapperGrid;
            return wrapperGrid;
        }

        return null;
    }

    public void HideBottomSheet()
    {
        if (_currentSheet == null)
        {
            return;
        }

#if IOS || MACCATALYST
        _boundsObserver?.Dispose();
        _boundsObserver = null;

        if (_overlayView != null)
        {
            _overlayView.RemoveFromSuperview();
            _overlayView = null;
        }

        _wrapperPage = null;
#endif

        // Remove from page layout if that's where it was added
        if (_hostLayout != null && _currentSheet != null)
        {
            _hostLayout.Children.Remove(_currentSheet);
            _hostLayout = null;
        }

        _currentSheet = null;

        // Notify listeners that the sheet was dismissed
        BottomSheetDismissed?.Invoke(this, EventArgs.Empty);
    }
}
