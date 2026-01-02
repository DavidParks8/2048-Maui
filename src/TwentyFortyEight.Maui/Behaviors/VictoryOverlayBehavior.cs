using Microsoft.Extensions.DependencyInjection;
using TwentyFortyEight.Maui.Components;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// Manages the victory overlays (warp + modal) on the game board.
/// Implemented as a behavior to ensure overlays are wired only after
/// the visual tree/handlers are ready (important for iOS lifecycle).
/// </summary>
public sealed class VictoryOverlayBehavior : Behavior<Border>
{
    private Border? _attachedElement;
    private VictoryModalOverlay? _victoryModal;
    private LoopingLottieOverlay? _warpOverlay;
    private VictoryViewModel? _victoryViewModel;

    private CancellationTokenSource? _showModalDelayCts;

    private IInputCoordinationService? _inputCoordinationService;

    protected override void OnAttachedTo(Border bindable)
    {
        base.OnAttachedTo(bindable);

        _attachedElement = bindable;
        bindable.Loaded += OnLoaded;
        bindable.HandlerChanged += OnHandlerChanged;

        // Handler may already exist.
        TryInitializeFromHandler();
    }

    protected override void OnDetachingFrom(Border bindable)
    {
        bindable.Loaded -= OnLoaded;
        bindable.HandlerChanged -= OnHandlerChanged;

        if (_warpParentGrid is not null)
        {
            _warpParentGrid.SizeChanged -= OnWarpParentSizeChanged;
        }

        UnwireOverlayEvents();

        _showModalDelayCts?.Cancel();
        _showModalDelayCts?.Dispose();
        _showModalDelayCts = null;

        _inputCoordinationService = null;
        _attachedElement = null;
        _warpParentGrid = null;

        base.OnDetachingFrom(bindable);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        TryInitializeFromHandler();
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        TryInitializeFromHandler();
    }

    private void TryInitializeFromHandler()
    {
        if (_attachedElement is null)
            return;

        // Resolve from App.Services instead of element handler services (iOS can delay handler readiness until user interaction).
        if (Application.Current is not App app)
            return;

        var services = app.Services;

        _inputCoordinationService = services.GetService<IInputCoordinationService>();

        _victoryViewModel ??= services.GetService<VictoryViewModel>();
        _victoryModal ??= FindVictoryModal(_attachedElement);

        // Find the warp overlay from XAML (defined in MainPage.xaml)
        _warpOverlay ??= FindWarpOverlay(_attachedElement);

        // Cache the parent grid for size tracking
        _warpParentGrid ??= FindPageRootGrid(_attachedElement);

        WireOverlayEvents();
        SyncWarpAndInput();
    }

    private Grid? _warpParentGrid;

    private static LoopingLottieOverlay? FindWarpOverlay(Element element)
    {
        // Find the page's root Grid and look for the warp overlay
        var rootGrid = FindPageRootGrid(element);
        if (rootGrid is null)
            return null;

        foreach (var child in rootGrid.Children)
        {
            if (child is LoopingLottieOverlay overlay)
            {
                return overlay;
            }
        }

        return null;
    }

    private static VictoryModalOverlay? FindVictoryModal(Border attachedElement)
    {
        if (attachedElement.Content is not Grid innerGrid)
            return null;

        foreach (var child in innerGrid.Children)
        {
            if (child is VictoryModalOverlay overlay)
            {
                return overlay;
            }
        }

        return null;
    }

    private void OnWarpParentSizeChanged(object? sender, EventArgs e)
    {
        UpdateWarpOverlaySize();
    }

    private void UpdateWarpOverlaySize()
    {
        if (_warpOverlay is null || _warpParentGrid is null)
            return;

        // Calculate size based on the diagonal of the container to ensure full coverage
        // Use the larger dimension * 2 to ensure the circle fills the entire screen
        var width = _warpParentGrid.Width > 0 ? _warpParentGrid.Width : 1000;
        var height = _warpParentGrid.Height > 0 ? _warpParentGrid.Height : 1000;
        var diagonal = Math.Sqrt(width * width + height * height);

        // Use diagonal * 2 to ensure full coverage even when animation is at smallest scale
        var size = diagonal * 2;

        _warpOverlay.WidthRequest = size;
        _warpOverlay.HeightRequest = size;
    }

    private static Grid? FindPageRootGrid(Element element)
    {
        var current = element.Parent;
        while (current is not null)
        {
            if (current is ContentPage page && page.Content is Grid rootGrid)
            {
                return rootGrid;
            }
            current = current.Parent;
        }
        return null;
    }

    private void WireOverlayEvents()
    {
        // Idempotent wiring.
        if (_victoryModal is not null)
        {
            _victoryModal.PropertyChanged -= OnOverlayPropertyChanged;
        }
        if (_warpOverlay is not null)
        {
            _warpOverlay.PropertyChanged -= OnOverlayPropertyChanged;
        }

        if (_victoryViewModel is not null)
        {
            _victoryViewModel.State.PropertyChanged -= OnVictoryStatePropertyChanged;
            _victoryViewModel.AnimationStartRequested -= OnVictoryAnimationStartRequested;
            _victoryViewModel.AnimationStopRequested -= OnVictoryAnimationStopRequested;
        }

        if (_victoryModal is not null)
        {
            _victoryModal.PropertyChanged += OnOverlayPropertyChanged;
        }
        if (_warpOverlay is not null)
        {
            _warpOverlay.PropertyChanged += OnOverlayPropertyChanged;
        }

        if (_victoryViewModel is not null)
        {
            _victoryViewModel.State.PropertyChanged += OnVictoryStatePropertyChanged;
            _victoryViewModel.AnimationStartRequested += OnVictoryAnimationStartRequested;
            _victoryViewModel.AnimationStopRequested += OnVictoryAnimationStopRequested;
        }

        // Subscribe to size changes if we have a parent grid
        if (_warpParentGrid is not null)
        {
            _warpParentGrid.SizeChanged -= OnWarpParentSizeChanged;
            _warpParentGrid.SizeChanged += OnWarpParentSizeChanged;
            UpdateWarpOverlaySize();
        }
    }

    private void UnwireOverlayEvents()
    {
        if (_warpOverlay is not null)
        {
            _warpOverlay.PropertyChanged -= OnOverlayPropertyChanged;
        }

        if (_victoryModal is not null)
        {
            _victoryModal.PropertyChanged -= OnOverlayPropertyChanged;
        }

        if (_victoryViewModel is not null)
        {
            _victoryViewModel.State.PropertyChanged -= OnVictoryStatePropertyChanged;
            _victoryViewModel.AnimationStartRequested -= OnVictoryAnimationStartRequested;
            _victoryViewModel.AnimationStopRequested -= OnVictoryAnimationStopRequested;
        }
    }

    private void OnVictoryAnimationStartRequested(object? sender, EventArgs e)
    {
        CancelShowModalDelay();
        _showModalDelayCts = new CancellationTokenSource();
        _ = ShowModalAfterDelayAsync(_showModalDelayCts.Token);
    }

    private void OnVictoryAnimationStopRequested(object? sender, EventArgs e)
    {
        CancelShowModalDelay();
    }

    private void CancelShowModalDelay()
    {
        _showModalDelayCts?.Cancel();
        _showModalDelayCts?.Dispose();
        _showModalDelayCts = null;
    }

    private async Task ShowModalAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(700), token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        VictoryViewModel? victoryViewModel = _victoryViewModel;
        if (victoryViewModel is null)
        {
            return;
        }

        IDispatcher? dispatcher = _attachedElement?.Dispatcher;
        if (dispatcher is null)
        {
            victoryViewModel.ShowModal();
            return;
        }

        dispatcher.Dispatch(victoryViewModel.ShowModal);
    }

    private void OnVictoryStatePropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (
            e.PropertyName == nameof(ViewModels.Models.VictoryState.IsActive)
            || e.PropertyName == nameof(ViewModels.Models.VictoryState.IsModalVisible)
        )
        {
            SyncWarpAndInput();
        }
    }

    private void SyncWarpAndInput()
    {
        if (_victoryViewModel is null)
            return;

        bool isActive = _victoryViewModel.State.IsActive;

        // Warp overlay is decoupled from the modal; it runs whenever victory is active.
        if (_warpOverlay is not null)
        {
            if (isActive)
            {
                UpdateWarpOverlaySize();
                _warpOverlay.Start();
                _ = FadeInWarpOverlayAsync();
            }
            else
            {
                _warpOverlay.Stop();
                _warpOverlay.Opacity = 0;
            }
        }

        SyncInputBlockingState();
    }

    private void OnOverlayPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(VisualElement.IsVisible))
        {
            SyncInputBlockingState();
        }
    }

    private void SyncInputBlockingState()
    {
        if (_inputCoordinationService is null)
            return;

        bool isActive = _victoryViewModel?.State.IsActive == true;
        bool warpVisible = _warpOverlay?.IsVisible == true;
        bool modalVisible = _victoryModal?.IsVisible == true;
        _inputCoordinationService.IsInputBlocked = isActive || warpVisible || modalVisible;
    }

    private async Task FadeInWarpOverlayAsync()
    {
        if (_warpOverlay is null)
            return;

        // Fade in from 0 to 1 over 600ms
        await _warpOverlay.FadeToAsync(1, 600, Easing.CubicOut);
    }
}
