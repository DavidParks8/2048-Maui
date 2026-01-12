using System.Windows.Input;
using Maui.BindableProperty.Generator.Core;
using TwentyFortyEight.ViewModels.Helpers;

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

    // Fallback ratio when content measurement isn't available.
    private const double HalfExpandedRatio = 0.45;
    private const double DismissThreshold = 0.25;

    private const uint ShowDurationMs = 280;
    private const uint HideDurationMs = 140;
    private const uint ReduceMotionShowFadeDurationMs = 160;
    private const uint ReduceMotionHideFadeDurationMs = 120;

    private const double BaseBottomPadding = 16;
    private const double DetentSnapTolerance = 24;
    private const double DismissVelocityThreshold = 800; // pixels per second

    private double _windowHeight;
    private double _windowWidth;
    private double _topInset = 0; // Top inset where sheet should stop (bottom of nav bar)
    private double _bottomInset = 0; // Bottom safe area inset (home indicator)
    private double _dragStartTranslation;
    private bool _isAnimating;
    private bool _isDragging;
    private bool _isInitialized;
    private bool _showAnimationStarted;
    private readonly PanVelocityTracker _velocityTracker = new();

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
        SetSheetContent(newValue);
    }

    private void SetSheetContent(View? content)
    {
        if (ContentScrollView != null)
        {
            ContentScrollView.Content = content;
            return;
        }

        if (ContentCard != null)
        {
            ContentCard.Content = content;
        }
    }

    private void ApplySafeAreaPadding()
    {
        if (SheetContainer == null)
        {
            return;
        }

        var padding = SheetContainer.Padding;
        var desiredBottomPadding = BaseBottomPadding + _bottomInset;

        if (Math.Abs(padding.Bottom - desiredBottomPadding) < 0.5)
        {
            return;
        }

        SheetContainer.Padding = new Thickness(
            padding.Left,
            padding.Top,
            padding.Right,
            desiredBottomPadding
        );
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
                _velocityTracker.Reset();
                _velocityTracker.RecordSample(e.TotalX, e.TotalY, DateTime.UtcNow);
                break;

            case GestureStatus.Running:
                // Calculate new translation (positive = down/less visible, negative would go above screen)
                var newTranslationY = _dragStartTranslation + e.TotalY;

                // Clamp: minimum = top inset (bottom of nav bar),
                // maximum = window height (fully hidden)
                newTranslationY = Math.Clamp(newTranslationY, _topInset, _windowHeight);
                SheetContainer.TranslationY = newTranslationY;

                // Track for velocity calculation
                _velocityTracker.RecordSample(e.TotalX, e.TotalY, DateTime.UtcNow);
                break;

            case GestureStatus.Completed:
                try
                {
                    await SnapToNearestDetentAsync(_velocityTracker.GetVelocity());
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

    private Task SnapToNearestDetentAsync()
    {
        return SnapToNearestDetentAsync(velocity: 0.0);
    }

    private async Task SnapToNearestDetentAsync(double velocity)
    {
        if (SheetContainer == null || _windowHeight <= 0)
        {
            return;
        }

        _isAnimating = true;

        try
        {
            var currentTranslation = SheetContainer.TranslationY;

            // Check if fast swipe down (positive velocity = downward)
            if (velocity > DismissVelocityThreshold)
            {
                // Fast swipe down - dismiss immediately
                CloseCommand?.Execute(null);
                return;
            }

            var halfExpandedTranslation = GetHalfExpandedTranslation();
            var fullExpandedTranslation = _topInset; // Stops at bottom of nav bar
            var dismissTranslation = _windowHeight * (1 - DismissThreshold);

            var snapMidpoint =
                fullExpandedTranslation + (halfExpandedTranslation - fullExpandedTranslation) / 2;

            // Determine which detent to snap to
            double targetTranslation;

            if (currentTranslation >= dismissTranslation)
            {
                // Dismiss the sheet
                CloseCommand?.Execute(null);
                return;
            }
            else if (currentTranslation <= snapMidpoint)
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
        // Prefer a content-fit detent so large screens don't show excessive empty space.
        if (TryGetContentBasedTranslation(out var contentTranslation))
        {
            return Math.Max(contentTranslation, _topInset);
        }

        // Fallback detent based on screen ratio.
        return Math.Max(_windowHeight * (1 - HalfExpandedRatio), _topInset);
    }

    private bool TryGetContentBasedTranslation(out double translation)
    {
        return TryGetContentBasedTranslation(
            _windowHeight,
            _windowWidth,
            _topInset,
            _bottomInset,
            out translation
        );
    }

    private bool TryGetContentBasedTranslation(
        double windowHeight,
        double windowWidth,
        double topInset,
        double bottomInset,
        out double translation
    )
    {
        translation = 0;

        if (
            SheetContainer == null
            || ContentCard == null
            || SheetContent == null
            || windowHeight <= 0
            || windowWidth <= 0
        )
        {
            return false;
        }

        // Approximate available widths based on margins and max width requests.
        // This doesn't have to be pixel-perfect; it just needs a stable measurement.
        var sheetAvailableWidth = Math.Max(
            0,
            windowWidth - SheetContainer.Margin.HorizontalThickness
        );
        var cardAvailableWidth = Math.Max(
            0,
            sheetAvailableWidth - ContentCard.Margin.HorizontalThickness
        );

        if (ContentCard.MaximumWidthRequest > 0)
        {
            cardAvailableWidth = Math.Min(cardAvailableWidth, ContentCard.MaximumWidthRequest);
        }

        var contentAvailableWidth = Math.Max(
            0,
            cardAvailableWidth - ContentCard.Padding.HorizontalThickness
        );
        if (contentAvailableWidth <= 0)
        {
            return false;
        }

        var headerHeight = 0.0;
        if (HeaderContainer != null)
        {
            headerHeight =
                HeaderContainer.Height > 0
                    ? HeaderContainer.Height
                    : HeaderContainer.Measure(sheetAvailableWidth, double.PositiveInfinity).Height;
        }

        var contentHeight = SheetContent
            .Measure(contentAvailableWidth, double.PositiveInfinity)
            .Height;

        var rowSpacing = SheetGrid?.RowSpacing ?? 0;

        // SheetContainer padding is adjusted to BaseBottomPadding + bottomInset; use the passed-in inset
        // so we can compute both pre- and post-resize detents accurately.
        var sheetPaddingVertical = SheetContainer.Padding.Top + (BaseBottomPadding + bottomInset);

        var totalVisibleHeight =
            sheetPaddingVertical
            + headerHeight
            + rowSpacing
            + ContentCard.Margin.VerticalThickness
            + ContentCard.Padding.VerticalThickness
            + contentHeight;

        // Translation = how far down the sheet starts from the top.
        translation = windowHeight - totalVisibleHeight;
        translation = Math.Clamp(translation, topInset, windowHeight);

        return true;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        var previousWindowWidth = _windowWidth;
        var previousTopInset = _topInset;
        var previousBottomInset = _bottomInset;

        if (width > 0 && width != double.PositiveInfinity)
        {
            _windowWidth = width;
        }

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
                var heightChanged =
                    previousWindowHeight > 0 && Math.Abs(previousWindowHeight - _windowHeight) > 1;
                var widthChanged =
                    previousWindowWidth > 0 && Math.Abs(previousWindowWidth - _windowWidth) > 1;

                if (heightChanged || widthChanged)
                {
                    // Window resized - update safe-area padding and re-evaluate detent.
                    ApplySafeAreaPadding();
                    UpdateSheetForResize(
                        previousWindowHeight,
                        previousWindowWidth,
                        previousTopInset,
                        previousBottomInset
                    );
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

    private void UpdateSheetForResize(
        double previousWindowHeight,
        double previousWindowWidth,
        double previousTopInset,
        double previousBottomInset
    )
    {
        if (SheetContainer == null)
        {
            return;
        }

        // If we're currently near a detent, snap to the recalculated detent after resize.
        // Otherwise, preserve the visible ratio to avoid surprising jumps.
        var currentTranslation = SheetContainer.TranslationY;

        var previousFullDetent = previousTopInset;
        var previousPreferredDetent = Math.Max(
            previousWindowHeight * (1 - HalfExpandedRatio),
            previousTopInset
        );

        if (
            TryGetContentBasedTranslation(
                previousWindowHeight,
                previousWindowWidth,
                previousTopInset,
                previousBottomInset,
                out var previousContentDetent
            )
        )
        {
            previousPreferredDetent = Math.Max(previousContentDetent, previousTopInset);
        }

        var distToFull = Math.Abs(currentTranslation - previousFullDetent);
        var distToPreferred = Math.Abs(currentTranslation - previousPreferredDetent);
        var isNearDetent = Math.Min(distToFull, distToPreferred) <= DetentSnapTolerance;

        // Update sheet height
        SheetContainer.HeightRequest = _windowHeight;

        if (isNearDetent)
        {
            var snapToFull = distToFull <= distToPreferred;
            SheetContainer.TranslationY = snapToFull ? _topInset : GetHalfExpandedTranslation();
            return;
        }

        // Calculate the current visible ratio based on previous window height
        var previousDenominator = previousWindowHeight - previousTopInset;
        if (previousDenominator <= 0)
        {
            SheetContainer.TranslationY = Math.Clamp(currentTranslation, _topInset, _windowHeight);
            return;
        }

        var previousVisibleRatio =
            1 - ((currentTranslation - previousTopInset) / previousDenominator);

        // Clamp ratio to valid range
        previousVisibleRatio = Math.Clamp(previousVisibleRatio, 0, 1);

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

        SetSheetContent(SheetContent);

        // Respect iOS home indicator safe area.
        ApplySafeAreaPadding();

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
            _windowWidth = window.Bounds.Width;

            // Get the navigation bar bottom position
            // This is where the sheet should stop when fully expanded
            var safeAreaInsets = window.SafeAreaInsets;
            var safeAreaTop = safeAreaInsets.Top;
            _bottomInset = safeAreaInsets.Bottom;

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
        _bottomInset = 0;

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
