using TwentyFortyEight.Maui.Components;

namespace TwentyFortyEight.Maui.Services;

public partial class WindowOverlayService : IWindowOverlayService
{
    private BottomSheetOverlay? _currentSheet;
    private Layout? _hostLayout;

    private readonly IInputCoordinationService _inputCoordinationService;
    private bool? _previousInputBlocked;

    public bool IsBottomSheetVisible => _currentSheet?.IsVisible == true;

    public event EventHandler? BottomSheetDismissed;

    public WindowOverlayService(IInputCoordinationService inputCoordinationService)
    {
        _inputCoordinationService = inputCoordinationService;
    }

    public void ShowBottomSheet(string title, View content)
    {
        HideBottomSheet();

        bool previousInputBlocked = _inputCoordinationService.IsInputBlocked;

        try
        {
            var dismissCommand = new Command(() => HideBottomSheet());

            _currentSheet = new BottomSheetOverlay
            {
                Title = title,
                SheetContent = content,
                CloseCommand = dismissCommand,
                ScrimTapCommand = dismissCommand,
                IsVisible = true,
            };

            bool presented = TryShowOverlayNative() || ShowOverlayInPage();

            if (!presented)
            {
                _currentSheet = null;
                return;
            }

            _previousInputBlocked = previousInputBlocked;
            _inputCoordinationService.IsInputBlocked = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bottom sheet failed: {ex}");
            _currentSheet = null;
            _previousInputBlocked = null;
            _inputCoordinationService.IsInputBlocked = previousInputBlocked;
        }
    }

    private void RestorePreviousInputBlockedState()
    {
        if (_previousInputBlocked is null)
        {
            return;
        }

        _inputCoordinationService.IsInputBlocked = _previousInputBlocked.Value;
        _previousInputBlocked = null;
    }

#if IOS || MACCATALYST
    private partial bool TryShowOverlayNative();

    private partial void CleanupNativeOverlay();
#else
    private bool TryShowOverlayNative() => false;

    private void CleanupNativeOverlay() { }
#endif

    /// <summary>
    /// Cross-platform fallback: adds the overlay to the current page's root layout.
    /// Works on all platforms but won't cover the navigation bar.
    /// </summary>
    private bool ShowOverlayInPage()
    {
        if (_currentSheet == null)
        {
            return false;
        }

        var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (currentPage is Shell shell)
        {
            currentPage = shell.CurrentPage;
        }

        if (currentPage is NavigationPage navPage)
        {
            currentPage = navPage.CurrentPage;
        }

        _hostLayout = FindHostLayout(currentPage);
        if (_hostLayout == null)
        {
            return false;
        }

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
            Grid.SetRowSpan(_currentSheet, Math.Max(1, grid.RowDefinitions.Count));
            Grid.SetColumnSpan(_currentSheet, Math.Max(1, grid.ColumnDefinitions.Count));
            _currentSheet.ZIndex = 1000;
            grid.Children.Add(_currentSheet);
        }

        return true;
    }

    private static Layout? FindHostLayout(Page? page)
    {
        if (page is not ContentPage contentPage)
        {
            return null;
        }

        if (contentPage.Content is AbsoluteLayout absoluteLayout)
        {
            return absoluteLayout;
        }

        if (contentPage.Content is Grid grid)
        {
            return grid;
        }

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

        RestorePreviousInputBlockedState();
        CleanupNativeOverlay();

        if (_hostLayout != null)
        {
            _hostLayout.Children.Remove(_currentSheet);
            _hostLayout = null;
        }

        _currentSheet = null;

        BottomSheetDismissed?.Invoke(this, EventArgs.Empty);
    }
}
