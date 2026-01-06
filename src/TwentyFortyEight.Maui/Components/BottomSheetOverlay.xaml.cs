using System.Windows.Input;
using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// Reusable bottom sheet overlay with liquid glass effect.
/// Uses TranslationY-based positioning like native iOS sheets.
/// The sheet is sized to full window height and positioned via TranslationY.
/// </summary>
public partial class BottomSheetOverlay : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    [AutoBindable(OnChanged = nameof(OnTitleChanged))]
    private readonly string _title = string.Empty;

    [AutoBindable(OnChanged = nameof(OnSheetContentChanged))]
    private readonly View? _sheetContent;

    [AutoBindable]
    private readonly ICommand? _closeCommand;

    [AutoBindable]
    private readonly ICommand? _scrimTapCommand;

#pragma warning restore CS0169

    // Ratios for sheet heights
    private const double HalfExpandedRatio = 0.45;
    private const double DismissThreshold = 0.25;

    private double _windowHeight;
    private double _topInset; // Top inset where sheet should stop (bottom of nav bar)
    private double _dragStartTranslation;
    private bool _isAnimating;

    public BottomSheetOverlay()
    {
        InitializeComponent();
    }

    private void OnTitleChanged(string oldValue, string newValue)
    {
        if (TitleLabel != null)
        {
            TitleLabel.Text = newValue;
        }
    }

    private void OnSheetContentChanged(View? oldValue, View? newValue)
    {
        if (ContentCard != null)
        {
            ContentCard.Content = newValue;
        }
    }

    private void OnScrimTapped(object? sender, TappedEventArgs e)
    {
        ScrimTapCommand?.Execute(null);
    }

    private void OnCloseButtonClicked(object? sender, EventArgs e)
    {
        CloseCommand?.Execute(null);
    }

    private async void OnSheetPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (SheetContainer == null || _isAnimating || _windowHeight <= 0)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _dragStartTranslation = SheetContainer.TranslationY;
                break;

            case GestureStatus.Running:
                // Calculate new translation (positive = down/less visible, negative would go above screen)
                var newTranslationY = _dragStartTranslation + e.TotalY;

                // Clamp: minimum = top inset (bottom of nav bar),
                // maximum = window height (fully hidden)
                newTranslationY = Math.Clamp(newTranslationY, _topInset, _windowHeight);
                SheetContainer.TranslationY = newTranslationY;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                await SnapToNearestDetentAsync();
                break;
        }
    }

    private async Task SnapToNearestDetentAsync()
    {
        if (SheetContainer == null || _windowHeight <= 0)
        {
            return;
        }

        _isAnimating = true;

        try
        {
            var currentTranslation = SheetContainer.TranslationY;
            var halfExpandedTranslation = GetHalfExpandedTranslation();
            var fullExpandedTranslation = _topInset; // Stops at bottom of nav bar
            var dismissTranslation = _windowHeight * (1 - DismissThreshold);

            // Determine which detent to snap to
            double targetTranslation;

            if (currentTranslation >= dismissTranslation)
            {
                // Dismiss the sheet
                CloseCommand?.Execute(null);
                return;
            }
            else if (currentTranslation <= halfExpandedTranslation / 2)
            {
                // Closer to full expanded (use fullExpandedTranslation which respects safe area)
                targetTranslation = fullExpandedTranslation;
            }
            else
            {
                // Closer to half expanded
                targetTranslation = halfExpandedTranslation;
            }

            // Animate to target
            await SheetContainer.TranslateToAsync(0, targetTranslation, 200, Easing.CubicOut);
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private double GetHalfExpandedTranslation()
    {
        // For half expanded (45% visible), translation = 55% of window height
        return _windowHeight * (1 - HalfExpandedRatio);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (height > 0 && height != double.PositiveInfinity)
        {
            // Always get the full window height including nav bar (may have changed on resize)
            var windowHeight = GetFullWindowHeight();
            var previousWindowHeight = _windowHeight;

            if (windowHeight > 0)
            {
                _windowHeight = windowHeight;
            }
            else
            {
                _windowHeight = height;
            }

            if (IsVisible)
            {
                // Check if this is a resize vs initial display
                if (previousWindowHeight > 0 && Math.Abs(previousWindowHeight - _windowHeight) > 1)
                {
                    // Window resized - update sheet size while maintaining current visible ratio
                    UpdateSheetForResize(previousWindowHeight);
                }
                else
                {
                    InitializeSheet();
                }
            }
        }
    }

    private void UpdateSheetForResize(double previousWindowHeight)
    {
        if (SheetContainer == null)
        {
            return;
        }

        // Calculate the current visible ratio based on previous window height
        var currentTranslation = SheetContainer.TranslationY;
        var previousVisibleRatio =
            1 - ((currentTranslation - _topInset) / (previousWindowHeight - _topInset));

        // Clamp ratio to valid range
        previousVisibleRatio = Math.Clamp(previousVisibleRatio, 0, 1);

        // Update sheet height
        SheetContainer.HeightRequest = _windowHeight;

        // Calculate new translation to maintain the same visible ratio
        var newTranslation = _topInset + (1 - previousVisibleRatio) * (_windowHeight - _topInset);
        SheetContainer.TranslationY = newTranslation;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsVisible))
        {
            if (IsVisible)
            {
                InitializeSheet();
            }
            else
            {
                ResetSheet();
            }
        }
    }

    private void InitializeSheet()
    {
        if (SheetContainer == null || Scrim == null)
        {
            return;
        }

        // Get window height if we don't have it yet
        if (_windowHeight <= 0)
        {
            _windowHeight = GetFullWindowHeight();
            if (_windowHeight <= 0)
            {
                _windowHeight = Height > 0 ? Height : 800; // fallback
            }
        }

        // Set initial content
        if (TitleLabel != null)
        {
            TitleLabel.Text = Title;
        }

        if (ContentCard != null && SheetContent != null)
        {
            ContentCard.Content = SheetContent;
        }

        // Set sheet height to full window height
        SheetContainer.HeightRequest = _windowHeight;

        // Position sheet at half-expanded (respecting nav bar position)
        var halfExpandedTranslation = GetHalfExpandedTranslation();
        // Ensure half-expanded is at least at nav bar level
        halfExpandedTranslation = Math.Max(halfExpandedTranslation, _topInset);
        SheetContainer.TranslationY = halfExpandedTranslation;

        // Show scrim
        Scrim.Opacity = 1;
    }

    private void ResetSheet()
    {
        if (SheetContainer == null || Scrim == null)
        {
            return;
        }

        // Reset to hidden state
        SheetContainer.TranslationY = _windowHeight;
        Scrim.Opacity = 0;
    }

    private double GetFullWindowHeight()
    {
        // Try to get the actual window/screen height
#if IOS || MACCATALYST
        var viewController = Microsoft.Maui.ApplicationModel.Platform.GetCurrentUIViewController();
        var window = viewController?.View?.Window;
        if (window != null)
        {
            // Get the navigation bar bottom position
            // This is where the sheet should stop when fully expanded
            var safeAreaTop = window.SafeAreaInsets.Top;

            // Try to get the navigation bar height from the navigation controller
            var navController = viewController?.NavigationController;
            var navBarHeight = navController?.NavigationBar?.Frame.Height ?? 44;

            // The top inset is safe area + navigation bar height
            _topInset = safeAreaTop + navBarHeight;

            return window.Bounds.Height;
        }
#endif
        // Fallback: traverse up to find Page and use its height
        Element? current = Parent;
        while (current != null)
        {
            if (current is Page page && page.Height > 0 && page.Height != double.PositiveInfinity)
            {
                // Add estimated nav bar height
                return page.Height + 100;
            }
            current = current.Parent;
        }
        return 0;
    }
}
