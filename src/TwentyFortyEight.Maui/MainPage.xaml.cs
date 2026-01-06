using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Shapes;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Converters;
using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Helpers;
using TwentyFortyEight.ViewModels.Messages;
using TwentyFortyEight.ViewModels.Models;
#if IOS
using UIKit;
#endif

namespace TwentyFortyEight.Maui;

public partial class MainPage : ContentPage
{
    private readonly GameViewModel _viewModel;
    private readonly VictoryViewModel _victoryViewModel;
    private readonly TileAnimationService _animationService;
    private readonly IInputCoordinationService _inputCoordinationService;
    private readonly IGestureRecognizerService _gestureRecognizerService;
    private readonly IWindowOverlayService _windowOverlayService;
    private readonly ILogger<MainPage> _logger;
    private readonly IToolbarIconService _toolbarIconService;
    private readonly Dictionary<TileViewModel, Border> _tileBorders = [];
    private CancellationTokenSource? _animationCts;
    private Task _activeTileAnimationTask = Task.CompletedTask;

    private bool _isModeSheetVisible;
    private bool _revertModeSelectionOnDismiss;
    private int _modeSheetOriginalBoardSize;

    // Responsive sizing
    private const double DefaultBoardSize = 400;
    private const double MinBoardSize = 280;
    private const double MaxBoardSize = 500;

    public MainPage(
        GameViewModel viewModel,
        VictoryViewModel victoryViewModel,
        TileAnimationService animationService,
        IInputCoordinationService inputCoordinationService,
        IGestureRecognizerService gestureRecognizerService,
        IWindowOverlayService windowOverlayService,
        ILogger<MainPage> logger,
        IToolbarIconService toolbarIconService
    )
    {
        InitializeComponent();

        _viewModel = viewModel;
        _victoryViewModel = victoryViewModel;
        _animationService = animationService;
        _inputCoordinationService = inputCoordinationService;
        _gestureRecognizerService = gestureRecognizerService;
        _windowOverlayService = windowOverlayService;
        _logger = logger;
        _toolbarIconService = toolbarIconService;
        BindingContext = _viewModel;

        // Wire up ViewModel victory event to VictoryViewModel
        _viewModel.VictoryAnimationRequested += OnVictoryAnimationRequested;

        // Wire up VictoryViewModel events
        _victoryViewModel.NewGameRequested += OnNewGameRequested;

        // Native/system icons (set in code-behind to keep XAML platform-agnostic)
        UndoButton.IconImageSource = _toolbarIconService.Undo;
        ToolbarModeButton.IconImageSource = _toolbarIconService.Mode;

        // Subscribe to tiles updated event for animations
        _viewModel.TilesUpdated += OnTilesUpdated;

        WeakReferenceMessenger.Default.Register<RulesetChangedMessage>(
            this,
            static (object recipient, RulesetChangedMessage _) =>
            {
                MainThread.BeginInvokeOnMainThread(((MainPage)recipient).RebuildBoardGrid);
            }
        );

        // Add tiles to the grid
        CreateTiles();

        // Set up input coordination (keyboard, gamepad, scroll)
        _inputCoordinationService.RegisterBehaviors(this);
        _inputCoordinationService.DirectionInputReceived += OnDirectionInputReceived;

        // Set up gesture recognizers for swipe detection
        _gestureRecognizerService.AttachSwipeRecognizers(RootLayout);
        _gestureRecognizerService.SwipeDetected += OnSwipeDetected;

        // Subscribe to bottom sheet dismissal to sync ViewModel state
        _windowOverlayService.BottomSheetDismissed += OnBottomSheetDismissed;

        // Handle social gaming toolbar items visibility
        UpdateToolbarItems(_viewModel.IsSocialGamingAvailable);
    }

    private void RebuildBoardGrid()
    {
        // Cancel any pending animations and reset tile states.
        _animationCts?.Cancel();

        try
        {
            TileAnimationService.ResetTileStates(GameBoard, _tileBorders);
        }
        catch
        {
            // Ignore if grid is in the middle of being rebuilt.
        }

        // Clear existing visuals.
        GameBoard.Children.Clear();
        GameBoard.RowDefinitions.Clear();
        GameBoard.ColumnDefinitions.Clear();
        _tileBorders.Clear();

        CreateTiles();
    }

    private void OnNewGameRequested(object? sender, EventArgs e)
    {
        _viewModel.NewGameCommand.Execute(null);
    }

    private void OnDirectionInputReceived(object? sender, Direction direction)
    {
        _viewModel.MoveCommand.Execute(direction);
    }

    private void OnSwipeDetected(object? sender, Direction direction)
    {
        _viewModel.MoveCommand.Execute(direction);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Re-subscribe to events (they are unsubscribed in OnDisappearing)
        // Unsubscribe first to prevent duplicate handlers if OnAppearing is called multiple times
        _viewModel.TilesUpdated -= OnTilesUpdated;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _inputCoordinationService.DirectionInputReceived -= OnDirectionInputReceived;
        _gestureRecognizerService.SwipeDetected -= OnSwipeDetected;

        _viewModel.TilesUpdated += OnTilesUpdated;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _inputCoordinationService.DirectionInputReceived += OnDirectionInputReceived;
        _gestureRecognizerService.SwipeDetected += OnSwipeDetected;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Cancel any pending animations
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;

        // Unsubscribe from ViewModel events to prevent memory leaks
        _viewModel.TilesUpdated -= OnTilesUpdated;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _inputCoordinationService.DirectionInputReceived -= OnDirectionInputReceived;
        _gestureRecognizerService.SwipeDetected -= OnSwipeDetected;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || height <= 0)
            return;

        UpdateBoardSize(width, height);
    }

    private void UpdateBoardSize(double pageWidth, double pageHeight)
    {
        // Cancel any ongoing animations during resize
        _animationCts?.Cancel();

        // Account for padding and non-board UI elements
        const double horizontalReserved = 50; // 20px page padding + 10px border padding + 5px safety margin on each side
        const double verticalReserved = 260; // header ~80px, status ~30px, controls ~70px, spacing ~30px, padding 40px, board padding 20px

        double availableWidth = pageWidth - horizontalReserved;
        double availableHeight = pageHeight - verticalReserved;

        // Take the smaller dimension to maintain square aspect ratio
        double targetSize = Math.Min(availableWidth, availableHeight);

        // Apply min/max constraints
        double boardSize = Math.Clamp(targetSize, MinBoardSize, MaxBoardSize);

        // Apply to GameBoard
        GameBoard.WidthRequest = boardSize;
        GameBoard.HeightRequest = boardSize;

        // Calculate and update scale factor for font sizes
        const int defaultGridSize = 4;
        _viewModel.BoardScaleFactor =
            (boardSize / DefaultBoardSize) * (defaultGridSize / (double)_viewModel.BoardSize);

        // Scale tile spacing for very small boards
        double tileSpacing = Math.Max(5, boardSize / 40);
        GameBoard.ColumnSpacing = tileSpacing;
        GameBoard.RowSpacing = tileSpacing;
    }

    private void CreateTiles()
    {
        var boardSize = _viewModel.BoardSize;

        // Ensure we only subscribe once even if tiles are rebuilt.
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Create row and column definitions dynamically based on board size
        for (int i = 0; i < boardSize; i++)
        {
            GameBoard.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            GameBoard.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        // Create tile views
        for (int i = 0; i < _viewModel.Tiles.Count; i++)
        {
            var tile = _viewModel.Tiles[i];

            Border emptyCell = new()
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                Padding = 0,
                Background = new SolidColorBrush(TileColorHelper.GetTileBackgroundColor(0)),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 5,
                },
            };

            Border border = new()
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                Padding = 0,
                Background = new SolidColorBrush(tile.BackgroundColor),
                Content = new Label
                {
                    Text = tile.DisplayValue,
                    FontSize = tile.FontSize,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = tile.TextColor,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                },
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 5,
                },
            };

            // Set up bindings
            border.SetBinding(
                Border.BackgroundProperty,
                static (TileViewModel vm) => vm.BackgroundColor,
                converter: ColorToBrushConverter.Instance
            );

            Label label = (Label)border.Content;
            label.SetBinding(Label.TextProperty, static (TileViewModel vm) => vm.DisplayValue);
            label.SetBinding(Label.TextColorProperty, static (TileViewModel vm) => vm.TextColor);

            // Bind FontSize with scale converter
            BindScaledFontSize(label);

            border.BindingContext = tile;

            Grid.SetRow(emptyCell, tile.Row);
            Grid.SetColumn(emptyCell, tile.Column);
            Grid.SetRow(border, tile.Row);
            Grid.SetColumn(border, tile.Column);

            // Store the mapping
            _tileBorders[tile] = border;

            GameBoard.Children.Add(emptyCell);
            GameBoard.Children.Add(border);
        }

        // PropertyChanged subscription handled at the start of this method.
    }

    private void BindScaledFontSize(Label label)
    {
        IValueConverter converter = (IValueConverter)Resources["FontSizeScaleConverter"];

        label.SetBinding(
            Label.FontSizeProperty,
            static (TileViewModel vm) => vm.FontSize,
            mode: BindingMode.OneWay,
            converter: converter,
            converterParameter: _viewModel.BoardScaleFactor
        );
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (e.PropertyName == nameof(_viewModel.BoardScaleFactor))
        {
            // Update font size bindings for all tiles with new scale factor
            foreach (var border in _tileBorders.Values)
            {
                if (border.Content is Label label)
                {
                    BindScaledFontSize(label);
                }
            }
        }
        else if (e.PropertyName == nameof(GameViewModel.BoardSize))
        {
            UpdateBoardSize(Width, Height);
        }
        else if (e.PropertyName == nameof(GameViewModel.IsSocialGamingAvailable))
        {
            UpdateToolbarItems(_viewModel.IsSocialGamingAvailable);
        }
        else if (e.PropertyName == nameof(GameViewModel.IsNewGameConfirmationVisible))
        {
            HandleNewGameConfirmationVisibilityChanged();
        }
    }

    private void HandleNewGameConfirmationVisibilityChanged()
    {
        if (_viewModel.IsNewGameConfirmationVisible)
        {
            // Use a platform-native alert instead of a bottom sheet.
            Dispatcher.Dispatch(async () =>
            {
                bool confirmed = await DisplayAlertAsync(
                    AppStrings.RestartConfirmTitle,
                    AppStrings.RestartConfirmMessage,
                    AppStrings.StartNew,
                    AppStrings.Cancel
                );

                if (confirmed)
                {
                    _viewModel.ConfirmNewGameCommand.Execute(null);
                }
                else
                {
                    _viewModel.DismissNewGameConfirmationCommand.Execute(null);
                }
            });
        }
    }

    private void OnBottomSheetDismissed(object? sender, EventArgs e)
    {
        // Sync ViewModel state when sheet is dismissed by user interaction
        if (_isModeSheetVisible)
        {
            if (_revertModeSelectionOnDismiss)
            {
                _viewModel.PendingBoardSize = _modeSheetOriginalBoardSize;
            }

            _isModeSheetVisible = false;
            _revertModeSelectionOnDismiss = false;
        }
    }

    private void OnModeClicked(object? sender, EventArgs e)
    {
        // Seed pending values from the active ruleset.
        _modeSheetOriginalBoardSize = _viewModel.BoardSize;
        _viewModel.PendingBoardSize = _viewModel.BoardSize;
        _isModeSheetVisible = true;
        _revertModeSelectionOnDismiss = true;

        var modeSelectionView = new Components.ModeSelectionView(
            _viewModel,
            _modeSheetOriginalBoardSize
        );
        modeSelectionView.PlayRequested += async (_, _) =>
            await CommitModeSelectionAsync();

        _windowOverlayService.ShowBottomSheet(AppStrings.ModeTitle, modeSelectionView);
    }

    private async Task CommitModeSelectionAsync()
    {
        // Avoid reverting pending values when we dismiss programmatically after a commit.
        _revertModeSelectionOnDismiss = false;

        try
        {
            await _viewModel.PlaySelectedModeCommand.ExecuteAsync(null);
        }
        finally
        {
            _windowOverlayService.HideBottomSheet();
        }
    }

    private async void OnVictoryAnimationRequested(object? sender, EventArgs e)
    {
        // Block input during victory animation (restore after)
        bool previousInputBlocked = _inputCoordinationService.IsInputBlocked;
        _inputCoordinationService.IsInputBlocked = true;

        // The Core engine raises VictoryAchieved before the ViewModel raises TilesUpdated for
        // the move that produced the winning tile. Yield once so the TilesUpdated handler can
        // start (and set _activeTileAnimationTask), then await that animation to finish so the
        // victory UI starts after the winning move finishes animating.
        await Task.Yield();

        Task tileAnimationTask = _activeTileAnimationTask;
        try
        {
            await tileAnimationTask;
        }
        catch (OperationCanceledException)
        {
            // If animations were cancelled (e.g., resize/navigation), OnTilesUpdated resets the UI.
            // Proceed with victory handling using the best available final state.
        }

        try
        {
            // Trigger victory through the VictoryViewModel (MVVM pattern)
            _victoryViewModel.TriggerVictory(_viewModel.Score);
        }
        finally
        {
            _inputCoordinationService.IsInputBlocked = previousInputBlocked;
        }
    }

    private async void OnTilesUpdated(object? sender, TileUpdateEventArgs e)
    {
        // Cancel any pending animations before starting new ones
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = new CancellationTokenSource();

        _activeTileAnimationTask = _animationService.AnimateAsync(
            e,
            GameBoard,
            _viewModel.BoardSize,
            _tileBorders,
            _viewModel.BoardScaleFactor,
            _animationCts.Token
        );

        try
        {
            await _activeTileAnimationTask;
        }
        catch (OperationCanceledException)
        {
            // Animation was cancelled - reset tile states to ensure consistent UI
            TileAnimationService.ResetTileStates(GameBoard, _tileBorders);
        }
        catch (Exception ex)
        {
            // Log but don't crash - animations are non-critical
            LogAnimationError(_logger, ex);
        }
        finally
        {
            // Ensure future awaiters don't get stuck on a faulted/canceled task.
            _activeTileAnimationTask = Task.CompletedTask;
        }
    }

    private void UpdateToolbarItems(bool isSocialGamingAvailable)
    {
        if (isSocialGamingAvailable)
        {
            // Add social gaming toolbar items if not already present
            if (!ToolbarItems.Contains(ToolbarLeaderboardButton))
            {
                ToolbarItems.Insert(0, ToolbarLeaderboardButton);
            }
            if (!ToolbarItems.Contains(ToolbarAchievementsButton))
            {
                ToolbarItems.Insert(1, ToolbarAchievementsButton);
            }
        }
        else
        {
            // Remove social gaming toolbar items
            ToolbarItems.Remove(ToolbarLeaderboardButton);
            ToolbarItems.Remove(ToolbarAchievementsButton);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Animation error")]
    private static partial void LogAnimationError(ILogger logger, Exception ex);
}
