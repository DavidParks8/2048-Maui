using System.Windows.Input;
using Maui.BindableProperty.Generator.Core;
using Microsoft.Maui.Controls;

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

    [AutoBindable]
    private readonly bool _reduceMotionEnabled;

#pragma warning restore CS0169

    // Ratios for sheet heights
    private const double HalfExpandedRatio = 0.45;
    private const double DismissThreshold = 0.25;

    private const uint ShowDurationMs = 280;
    private const uint HideDurationMs = 140;
    private const uint ReduceMotionShowFadeDurationMs = 160;
    private const uint ReduceMotionHideFadeDurationMs = 120;

    private double _windowHeight;
    private double _topInset = 0; // Top inset where sheet should stop (bottom of nav bar)
    private double _dragStartTranslation;
    private bool _isAnimating;
    private bool _isDragging;
    private bool _isInitialized;
    private bool _showAnimationStarted;

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
                _isDragging = true;
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
                try
                {
                    await SnapToNearestDetentAsync();
                }
                finally
                {
                    _isDragging = false;
                }
                break;

            case GestureStatus.Canceled:
                // On Android (especially with mouse input in the emulator), Pan can be canceled
                // when the pointer exits the gesture area. Snapping here causes mid-drag jumps.
                _isDragging = false;
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
            var previousWindowHeight = _windowHeight;

#if IOS || MACCATALYST
            // iOS/macCatalyst: use the actual window bounds and compute a nav-bar-aware top inset.
            var windowHeight = GetFullWindowHeight();
            _windowHeight = windowHeight > 0 ? windowHeight : height;
#else
            // Other platforms: the overlay is hosted inside the page visual tree, so the allocated
            // height is the correct coordinate space for translation/detents.
            _topInset = 0;
            _windowHeight = height;
#endif

            if (IsVisible)
            {
                // Android can trigger additional layout passes while translating during gestures.
                // Never re-initialize or adjust translation while dragging/animating.
                if (_isDragging || _isAnimating)
                {
                    if (SheetContainer != null)
                    {
                        SheetContainer.HeightRequest = _windowHeight;
                    }
                    return;
                }

                // Check if this is a resize vs initial display
                if (previousWindowHeight > 0 && Math.Abs(previousWindowHeight - _windowHeight) > 1)
                {
                    // Window resized - update sheet size while maintaining current visible ratio
                    UpdateSheetForResize(previousWindowHeight);
                }
                else
                {
                    // Only initialize once per show; repeated layout passes should not reset translation.
                    if (!_isInitialized)
                    {
                        InitializeSheet();
                    }
                    else if (SheetContainer != null)
                    {
                        SheetContainer.HeightRequest = _windowHeight;
                    }
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
                _isInitialized = false;
                _showAnimationStarted = false;
                InitializeSheet();
            }
            else
            {
                _isInitialized = false;
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

        // Prepare initial state (offscreen, scrim hidden)
        Scrim.Opacity = 0;

        // Ensure consistent opacity when reusing the view.
        SheetContainer.Opacity = ReduceMotionEnabled ? 0 : 1;
        SheetContainer.TranslationY = _windowHeight;

        _isInitialized = true;

        // Animate the presentation once per show.
        _ = AnimateShowIfNeededAsync();
    }

    private void ResetSheet()
    {
        if (SheetContainer == null || Scrim == null)
        {
            return;
        }

        // Reset to hidden state
        SheetContainer.TranslationY = _windowHeight;
        SheetContainer.Opacity = ReduceMotionEnabled ? 0 : 1;
        Scrim.Opacity = 0;
    }

    private async Task AnimateShowIfNeededAsync()
    {
        if (SheetContainer == null || Scrim == null || _windowHeight <= 0)
        {
            return;
        }

        if (_showAnimationStarted || _isAnimating)
        {
            return;
        }

        _showAnimationStarted = true;
        _isAnimating = true;

        try
        {
            // Position sheet at half-expanded (respecting nav bar position)
            var targetTranslation = GetHalfExpandedTranslation();
            targetTranslation = Math.Max(targetTranslation, _topInset);

            if (ReduceMotionEnabled)
            {
                // Fade only (no movement)
                SheetContainer.TranslationY = targetTranslation;
                SheetContainer.Opacity = 0;
                Scrim.Opacity = 0;

                await Task.WhenAll(
                    SheetContainer.FadeToAsync(1, ReduceMotionShowFadeDurationMs, Easing.CubicOut),
                    Scrim.FadeToAsync(1, ReduceMotionShowFadeDurationMs, Easing.CubicOut)
                );
            }
            else
            {
                // Slide from offscreen + scrim fade in
                SheetContainer.Opacity = 1;
                SheetContainer.TranslationY = _windowHeight;
                Scrim.Opacity = 0;

                await Task.WhenAll(
                    SheetContainer.TranslateToAsync(
                        0,
                        targetTranslation,
                        ShowDurationMs,
                        Easing.CubicOut
                    ),
                    Scrim.FadeToAsync(1, ShowDurationMs, Easing.CubicOut)
                );
            }
        }
        finally
        {
            _isAnimating = false;
        }
    }

    public async Task AnimateDismissAsync()
    {
        if (SheetContainer == null || Scrim == null)
        {
            return;
        }

        if (_isAnimating)
        {
            return;
        }

        // Ensure we have a valid coordinate space before dismissing.
        if (_windowHeight <= 0)
        {
            _windowHeight = GetFullWindowHeight();
            if (_windowHeight <= 0)
            {
                _windowHeight = Height > 0 ? Height : 800;
            }
        }

        _isAnimating = true;

        try
        {
            if (ReduceMotionEnabled)
            {
                await Task.WhenAll(
                    SheetContainer.FadeToAsync(0, ReduceMotionHideFadeDurationMs, Easing.CubicIn),
                    Scrim.FadeToAsync(0, ReduceMotionHideFadeDurationMs, Easing.CubicIn)
                );
            }
            else
            {
                await Task.WhenAll(
                    SheetContainer.TranslateToAsync(
                        0,
                        _windowHeight,
                        HideDurationMs,
                        Easing.CubicIn
                    ),
                    Scrim.FadeToAsync(0, HideDurationMs, Easing.CubicIn)
                );
            }
        }
        finally
        {
            _isAnimating = false;
        }
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
        // Non-iOS platforms: the overlay is hosted inside the current page's visual tree,
        // so we should size to the actual allocated/visual height (not an estimated window height).
        _topInset = 0;

        // Prefer this view's size if available.
        if (Height > 0 && Height != double.PositiveInfinity)
        {
            return Height;
        }

        // Fallback: traverse up to find a Page and use its height.
        Element? current = Parent;
        while (current != null)
        {
            if (current is Page page && page.Height > 0 && page.Height != double.PositiveInfinity)
            {
                return page.Height;
            }

            current = current.Parent;
        }

        return 0;
    }
}
