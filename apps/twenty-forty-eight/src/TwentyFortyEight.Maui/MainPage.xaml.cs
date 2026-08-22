using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Converters;
using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Helpers;
using TwentyFortyEight.ViewModels.Messages;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Services;
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
    private readonly IUserFeedbackService _userFeedbackService;
    private readonly ISwipePreviewInteractionService _swipePreviewInteractionService;
    private readonly IWindowOverlayService _windowOverlayService;
    private readonly IWallOverlayRenderer _wallOverlayRenderer;
    private readonly IAccessibilitySettingsService _accessibilitySettingsService;
    private readonly ILogger<MainPage> _logger;
    private readonly IToolbarIconService _toolbarIconService;
    private readonly IMessenger _messenger;
    private readonly IAdversarialSwipeTracker _adversarialSwipeTracker;
    private readonly ISwipeAttemptDetector _swipeAttemptDetector;
    private readonly Dictionary<TileViewModel, Border> _tileBorders = [];
    private readonly Dictionary<TileViewModel, Label> _tileLabels = [];
    private readonly Dictionary<TileViewModel, Border> _emptyCells = [];
    private readonly StringBuilder _boardAccessibilityBuilder = new(capacity: 256);
    private EventHandler<AppThemeChangedEventArgs>? _themeChangedHandler;
    private CancellationTokenSource? _animationCts;
    private Task _activeTileAnimationTask = Task.CompletedTask;

    private bool _isModeSheetVisible;
    private bool _revertModeSelectionOnDismiss;
    private int _modeSheetOriginalBoardSize;
    private GameMode _modeSheetOriginalGameMode;

    public MainPage(
        GameViewModel viewModel,
        VictoryViewModel victoryViewModel,
        TileAnimationService animationService,
        IInputCoordinationService inputCoordinationService,
        IGestureRecognizerService gestureRecognizerService,
        IUserFeedbackService userFeedbackService,
        ISwipePreviewInteractionService swipePreviewInteractionService,
        IWindowOverlayService windowOverlayService,
        IWallOverlayRenderer wallOverlayRenderer,
        IAccessibilitySettingsService accessibilitySettingsService,
        ILogger<MainPage> logger,
        IToolbarIconService toolbarIconService,
        IMessenger messenger,
        IAdversarialSwipeTracker adversarialSwipeTracker,
        ISwipeAttemptDetector swipeAttemptDetector
    )
    {
        InitializeComponent();

        _viewModel = viewModel;
        _victoryViewModel = victoryViewModel;
        _animationService = animationService;
        _inputCoordinationService = inputCoordinationService;
        _gestureRecognizerService = gestureRecognizerService;
        _userFeedbackService = userFeedbackService;
        _swipePreviewInteractionService = swipePreviewInteractionService;
        _windowOverlayService = windowOverlayService;
        _wallOverlayRenderer = wallOverlayRenderer;
        _accessibilitySettingsService = accessibilitySettingsService;
        _logger = logger;
        _toolbarIconService = toolbarIconService;
        _messenger = messenger;
        _adversarialSwipeTracker = adversarialSwipeTracker;
        _swipeAttemptDetector = swipeAttemptDetector;
        BindingContext = _viewModel;

        // Wire up ViewModel victory event to VictoryViewModel
        _viewModel.VictoryAnimationRequested += OnVictoryAnimationRequested;

        // Wire up board reset event to update accessibility description
        _viewModel.BoardReset += OnBoardReset;

        // Wire up VictoryViewModel events
        _victoryViewModel.NewGameRequested += OnNewGameRequested;

        // Native/system icons (set in code-behind to keep XAML platform-agnostic)
        UndoButton.IconImageSource = _toolbarIconService.Undo;
        ToolbarModeButton.IconImageSource = _toolbarIconService.Mode;

        // Subscribe to tiles updated event for animations
        _viewModel.TilesUpdated += OnTilesUpdated;

        _messenger.Register<RulesetChangedMessage>(
            this,
            static (recipient, _) =>
            {
                MainThread.BeginInvokeOnMainThread(((MainPage)recipient).RebuildBoardGrid);
            }
        );

        // Add tiles to the grid
        CreateTiles();
        UpdateWallOverlay(_viewModel.Wall);
        UpdateBoardAccessibilityDescription();

        // Show deterministic direction targets only when OS Voice Control is enabled.
        UpdateVoiceControlMoveButtonsVisibility();

        GameBoard.SizeChanged += OnGameBoardSizeChanged;

        // Keep wall colors legible across theme changes.
        var app = Application.Current;
        if (app != null)
        {
            _themeChangedHandler = (_, _) => UpdateWallOverlay(_viewModel.Wall);
            app.RequestedThemeChanged += _themeChangedHandler;
        }

        // Set up input coordination (keyboard, gamepad, scroll)
        _inputCoordinationService.RegisterBehaviors(this);
        _inputCoordinationService.DirectionInputReceived += OnDirectionInputReceived;

        // Set up gesture recognizers for swipe detection.
        // Adversarial mode uses tap-to-spawn; swipes/pans can interfere with taps (and can trigger
        // long-running UIGestureRecognizer blocking warnings on iOS), so disable them there.
        UpdateSwipeRecognizersForMode();
        _gestureRecognizerService.SwipePanUpdated += OnSwipePanUpdated;
        _swipeAttemptDetector.SwipeAttempted += OnSwipeAttempted;

        // Subscribe to bottom sheet dismissal to sync ViewModel state
        _windowOverlayService.BottomSheetDismissed += OnBottomSheetDismissed;

        // Handle social gaming toolbar items visibility
        UpdateToolbarItems(_viewModel.IsSocialGamingAvailable);
    }

    private void OnAccessibilitySettingsChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateVoiceControlMoveButtonsVisibility);
    }

    private void UpdateVoiceControlMoveButtonsVisibility()
    {
        bool showDirectionButtons;
        try
        {
            // In Adversarial mode, player taps to spawn tiles - directional buttons are not used.
            if (_viewModel.IsAdversarialMode)
            {
                showDirectionButtons = false;
            }
            else
            {
                showDirectionButtons = _accessibilitySettingsService.IsVoiceControlEnabled();
            }
        }
        catch
        {
            showDirectionButtons = false;
        }

        // Only update if changed to avoid unnecessary layout invalidations and property change events.
        if (MoveLeftContainer.IsVisible != showDirectionButtons)
        {
            MoveLeftContainer.IsVisible = showDirectionButtons;
            MoveUpContainer.IsVisible = showDirectionButtons;
            MoveDownContainer.IsVisible = showDirectionButtons;
            MoveRightContainer.IsVisible = showDirectionButtons;
        }
    }

    private void UpdateTileCellsAccessibilityForMode()
    {
        bool isAdversarial = _viewModel.IsAdversarialMode;
        foreach (var (tile, emptyCell) in _emptyCells)
        {
            UpdateTileCellAccessibility(emptyCell, tile, isAdversarial);
        }
    }

    private static void UpdateTileCellAccessibility(
        Border emptyCell,
        TileViewModel tile,
        bool isAdversarialMode
    )
    {
        // In Adversarial mode, only EMPTY cells are tap targets for spawning tiles.
        // Occupied cells should not be in the accessibility tree.
        bool shouldBeAccessible = isAdversarialMode && tile.Value == 0;

        if (shouldBeAccessible)
        {
            AutomationProperties.SetIsInAccessibleTree(emptyCell, true);
            AutomationProperties.SetName(
                emptyCell,
                $"Spawn at row {tile.Row + 1}, column {tile.Column + 1}"
            );
        }
        else
        {
            AutomationProperties.SetIsInAccessibleTree(emptyCell, false);
        }
    }

    private void RebuildBoardGrid()
    {
        // Cancel any pending animations and reset tile states.
        _animationCts?.Cancel();

        // Cancel any active swipe preview.
        _swipePreviewInteractionService.Reset();

        // Stop any wall overlay animations and drop renderer state.
        _wallOverlayRenderer.Reset(this);

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
        _tileLabels.Clear();
        _emptyCells.Clear();
        WallOverlayLayer.Children.Clear();

        CreateTiles();
        UpdateWallOverlay(_viewModel.Wall);
        UpdateBoardAccessibilityDescription();

        // Invalidate animation caches and warm up overlay pool for the new board size.
        // Max movements on an NxN board is roughly N*N (every tile could move).
        _animationService.InvalidateCache();
        _animationService.WarmUpOverlayPool(_viewModel.BoardSize * _viewModel.BoardSize);
    }

    private void OnNewGameRequested(object? sender, EventArgs e)
    {
        _viewModel.NewGameCommand.Execute(null);
    }

    private void OnBoardReset(object? sender, EventArgs e)
    {
        UpdateBoardAccessibilityDescription();
    }

    private void OnDirectionInputReceived(object? sender, Direction direction)
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
        _gestureRecognizerService.SwipePanUpdated -= OnSwipePanUpdated;
        _swipeAttemptDetector.SwipeAttempted -= OnSwipeAttempted;

        _viewModel.TilesUpdated += OnTilesUpdated;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _inputCoordinationService.DirectionInputReceived += OnDirectionInputReceived;
        _gestureRecognizerService.SwipePanUpdated += OnSwipePanUpdated;
        _swipeAttemptDetector.SwipeAttempted += OnSwipeAttempted;

        // Ensure swipe recognizers match the active mode when returning to the page.
        UpdateSwipeRecognizersForMode();

        // Keep Voice Control visibility in sync with OS state.
        // Re-subscribe to changes and immediately check current state to handle edge cases
        // where VoiceOver was toggled via Accessibility Shortcut (triple-click) while in-app.
        _accessibilitySettingsService.AccessibilitySettingsChanged -=
            OnAccessibilitySettingsChanged;
        _accessibilitySettingsService.AccessibilitySettingsChanged +=
            OnAccessibilitySettingsChanged;
        UpdateVoiceControlMoveButtonsVisibility();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _accessibilitySettingsService.AccessibilitySettingsChanged -=
            OnAccessibilitySettingsChanged;

        GameBoard.SizeChanged -= OnGameBoardSizeChanged;

        if (_themeChangedHandler != null)
        {
            Application.Current!.RequestedThemeChanged -= _themeChangedHandler;
            _themeChangedHandler = null;
        }

        // Stop any wall overlay animations.
        _wallOverlayRenderer.Reset(this);

        // Cancel any pending animations
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;

        // Cancel any swipe preview overlays.
        _swipePreviewInteractionService.Reset();

        // Unsubscribe from ViewModel events to prevent memory leaks
        _viewModel.TilesUpdated -= OnTilesUpdated;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _inputCoordinationService.DirectionInputReceived -= OnDirectionInputReceived;
        _gestureRecognizerService.SwipePanUpdated -= OnSwipePanUpdated;
        _swipeAttemptDetector.SwipeAttempted -= OnSwipeAttempted;
    }

    private async void OnSwipeAttempted(object? sender, EventArgs e)
    {
        // Track swipe attempts in adversarial mode and show toast hint after threshold
        if (_adversarialSwipeTracker.RecordSwipeAttempt())
        {
            await _userFeedbackService.ShowAdversarialModeTapHintAsync();
        }
    }

    private async void OnSwipePanUpdated(object? sender, SwipePanEventArgs e)
    {
        // Swipe events in adversarial mode are handled by the lightweight SwipeAttemptDetector,
        // which is separate from the main gesture recognizer to avoid tap interference.
        if (_viewModel.IsAdversarialMode)
        {
            return;
        }

        await _swipePreviewInteractionService.HandleSwipePanUpdatedAsync(
            e,
            BuildSwipePreviewContext()
        );
    }

    private void UpdateSwipeRecognizersForMode()
    {
        if (_viewModel.IsAdversarialMode)
        {
            // Adversarial mode uses tap-to-spawn. Detach the full gesture recognizers
            // (which can interfere with taps on iOS) and use a lightweight detector instead.
            _gestureRecognizerService.DetachSwipeRecognizers(RootLayout);
            _swipePreviewInteractionService.Reset();
            _swipeAttemptDetector.Attach(RootLayout);
        }
        else
        {
            // Normal mode: use full gesture recognizers for swipe preview and detection
            _swipeAttemptDetector.Detach(RootLayout);
            _adversarialSwipeTracker.Reset();
            _gestureRecognizerService.AttachSwipeRecognizers(RootLayout);
        }
    }

    private SwipePreviewUiContext BuildSwipePreviewContext()
    {
        return new SwipePreviewUiContext(
            GameBoard,
            _viewModel.BoardSize,
            _tileBorders,
            _viewModel.BoardScaleFactor,
            _inputCoordinationService.IsInputBlocked,
            _isModeSheetVisible,
            IsTileAnimationRunning: !_activeTileAnimationTask.IsCompleted
        );
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || height <= 0)
            return;

        UpdateBoardSize(width, height);
        UpdateMoveButtonsAlignment(width);
    }

    private void UpdateMoveButtonsAlignment(double pageWidth)
    {
        // On narrow screens, align movement buttons to start to avoid overlapping with undo button
        const double narrowScreenThreshold = 450;

        if (MoveButtonsContainer != null)
        {
            MoveButtonsContainer.HorizontalOptions =
                pageWidth < narrowScreenThreshold ? LayoutOptions.Start : LayoutOptions.Center;
        }
    }

    private void UpdateBoardSize(double pageWidth, double pageHeight)
    {
        // Cancel any ongoing animations during resize
        _animationCts?.Cancel();

        // Calculate responsive board dimensions using extracted helper
        double boardSize = BoardLayoutCalculator.CalculateBoardSize(pageWidth, pageHeight);

        // Apply to GameBoard
        GameBoard.WidthRequest = boardSize;
        GameBoard.HeightRequest = boardSize;

        // Keep overlay layer sized/centered with the board.
        WallOverlayLayer.WidthRequest = boardSize;
        WallOverlayLayer.HeightRequest = boardSize;

        // Calculate and update scale factor for font sizes
        _viewModel.BoardScaleFactor = BoardLayoutCalculator.CalculateScaleFactor(
            boardSize,
            _viewModel.BoardSize
        );

        // Scale tile spacing for very small boards
        double tileSpacing = BoardLayoutCalculator.CalculateTileSpacing(boardSize);
        GameBoard.ColumnSpacing = tileSpacing;
        GameBoard.RowSpacing = tileSpacing;

        // Wall position depends on spacing and actual board size.
        UpdateWallOverlay(_viewModel.Wall);
    }

    private void OnGameBoardSizeChanged(object? sender, EventArgs e)
    {
        // Wall position depends on the rendered size.
        UpdateWallOverlay(_viewModel.Wall);
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
            var tileIndex = i; // Capture for closure

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

            // Tiles/cells are not actionable in standard modes; keep them out of the accessibility tree.
            // In Adversarial mode, empty cells become tap targets for spawning tiles, so we make them accessible.
            UpdateTileCellAccessibility(emptyCell, tile, _viewModel.IsAdversarialMode);

            // Store reference for later accessibility updates when mode changes
            _emptyCells[tile] = emptyCell;

            // Add tap gesture for adversarial mode where player spawns tiles.
            // Create a shared tap handler - we'll add it to both emptyCell and the visible border
            // since the border sits on top and would otherwise block taps.
            void HandleTileTap(object? s, TappedEventArgs e)
            {
                if (
                    _viewModel.IsAdversarialMode
                    && _viewModel.TapEmptyCellCommand.CanExecute(tileIndex)
                )
                {
                    _viewModel.TapEmptyCellCommand.Execute(tileIndex);
                }
            }

            TapGestureRecognizer emptyTapGesture = new();
            emptyTapGesture.Tapped += HandleTileTap;
            emptyCell.GestureRecognizers.Add(emptyTapGesture);

            var label = new Label
            {
                Text = tile.DisplayValue,
                FontSize = tile.FontSize,
                FontAttributes = FontAttributes.Bold,
                // The game board has fixed-size tiles; letting OS Dynamic Type scale these
                // labels causes clipping/incorrect layout when accessibility text size is large.
                FontAutoScalingEnabled = false,
                TextColor = tile.TextColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.NoWrap,
                MaxLines = 1,
            };

            AutomationProperties.SetIsInAccessibleTree(label, false);

            Grid content = new();
            content.Children.Add(label);

            AutomationProperties.SetIsInAccessibleTree(content, false);

            Border border = new()
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                Padding = 0,
                Background = new SolidColorBrush(tile.BackgroundColor),
                Content = content,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 5,
                },
            };

            AutomationProperties.SetIsInAccessibleTree(border, false);

            // Prevent a brief flash of newly spawned tiles before the spawn animation hides them.
            // The ViewModel sets IsNewTile=true before updating Value, so this trigger keeps the
            // tile invisible until the animation service takes over.
            DataTrigger newTileTrigger = new(typeof(Border))
            {
                Binding = BindingBase.Create(static (TileViewModel vm) => vm.IsNewTile),
                Value = true,
            };
            newTileTrigger.Setters.Add(
                new Setter { Property = VisualElement.OpacityProperty, Value = 0d }
            );
            newTileTrigger.Setters.Add(
                new Setter { Property = VisualElement.ScaleProperty, Value = 0d }
            );
            border.Triggers.Add(newTileTrigger);

            // Set up bindings
            border.SetBinding(
                Border.BackgroundProperty,
                static (TileViewModel vm) => vm.BackgroundColor,
                converter: ColorToBrushConverter.Instance
            );

            label.SetBinding(Label.TextProperty, static (TileViewModel vm) => vm.DisplayValue);
            label.SetBinding(Label.TextColorProperty, static (TileViewModel vm) => vm.TextColor);

            // Bind FontSize with scale converter
            BindScaledFontSize(label);

            border.BindingContext = tile;

            // Add tap gesture to border as well since it overlays emptyCell
            TapGestureRecognizer borderTapGesture = new();
            borderTapGesture.Tapped += HandleTileTap;
            border.GestureRecognizers.Add(borderTapGesture);

            Grid.SetRow(emptyCell, tile.Row);
            Grid.SetColumn(emptyCell, tile.Column);
            Grid.SetRow(border, tile.Row);
            Grid.SetColumn(border, tile.Column);

            // Store the mapping
            _tileBorders[tile] = border;
            _tileLabels[tile] = label;

            GameBoard.Children.Add(emptyCell);
            GameBoard.Children.Add(border);
        }

        // PropertyChanged subscription handled at the start of this method.
    }

    private void UpdateBoardAccessibilityDescription()
    {
        try
        {
            var board = _viewModel.CurrentBoard;
            var boardSize = board.Size;
            if (boardSize <= 0)
            {
                return;
            }

            var builder = _boardAccessibilityBuilder;
            builder.Clear();
            builder.Append(AppStrings.GameBoardDescription);

            for (int row = 0; row < boardSize; row++)
            {
                builder.Append(' ');
                builder.AppendFormat(AppStrings.BoardRowFormat, row + 1, string.Empty);

                // Replace the trailing ": ." introduced by the empty placeholder above.
                // This avoids allocating a per-row joined string.
                builder.Length -= 2;

                for (int col = 0; col < boardSize; col++)
                {
                    if (col > 0)
                    {
                        builder.Append(", ");
                    }

                    int value = board[row, col];
                    if (value == 0)
                    {
                        builder.Append(AppStrings.BoardEmptyCell);
                    }
                    else
                    {
                        builder.Append(value);
                    }
                }

                builder.Append('.');
            }

            SemanticProperties.SetDescription(BoardContainer, builder.ToString());
        }
        catch
        {
            // Accessibility description should never crash the game.
        }
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
            foreach (var label in _tileLabels.Values)
            {
                BindScaledFontSize(label);
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
        else if (e.PropertyName == nameof(GameViewModel.Wall))
        {
            UpdateWallOverlay(_viewModel.Wall);
        }
        else if (e.PropertyName == nameof(GameViewModel.IsCoachNudgeVisible))
        {
            HandleCoachNudgeVisibilityChanged();
        }
        else if (e.PropertyName == nameof(GameViewModel.IsAdversarialMode))
        {
            UpdateSwipeRecognizersForMode();
            UpdateVoiceControlMoveButtonsVisibility();
            UpdateTileCellsAccessibilityForMode();
        }
    }

    private void UpdateWallOverlay(WallSegment? wall)
    {
        _wallOverlayRenderer.Update(GameBoard, WallOverlayLayer, _viewModel.BoardSize, wall, this);
    }

    private void HandleCoachNudgeVisibilityChanged()
    {
        if (!_viewModel.IsCoachNudgeVisible)
        {
            return;
        }

        // Move keyboard focus to the nudge action button so it is immediately reachable.
        // (The announcement itself is handled via IUserFeedbackService in the ViewModel.)
        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(100);
            if (_viewModel.IsCoachNudgeVisible)
            {
                CoachNudgeContainer?.FocusEnableButton();
            }
        });
    }

    private void OnBottomSheetDismissed(object? sender, EventArgs e)
    {
        // Sync ViewModel state when sheet is dismissed by user interaction
        if (_isModeSheetVisible)
        {
            if (_revertModeSelectionOnDismiss)
            {
                _viewModel.PendingBoardSize = _modeSheetOriginalBoardSize;
                _viewModel.PendingGameMode = _modeSheetOriginalGameMode;
            }

            _isModeSheetVisible = false;
            _revertModeSelectionOnDismiss = false;
        }
    }

    private void OnModeClicked(object? sender, EventArgs e)
    {
        // Seed pending values from the active ruleset.
        _modeSheetOriginalBoardSize = _viewModel.BoardSize;
        _modeSheetOriginalGameMode = _viewModel.GameMode;
        _viewModel.PendingBoardSize = _viewModel.BoardSize;
        _viewModel.PendingGameMode = _viewModel.GameMode;
        _isModeSheetVisible = true;
        _revertModeSelectionOnDismiss = true;

        var modeSelectionView = new Components.ModeSelectionView(
            _viewModel,
            _modeSheetOriginalBoardSize,
            _modeSheetOriginalGameMode
        );
        modeSelectionView.PlayRequested += async (_, _) => await CommitModeSelectionAsync();

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
            _victoryViewModel.TriggerVictory(
                _viewModel.Score,
                undoCount: _viewModel.UndoCount,
                isAdversarialMode: _viewModel.IsAdversarialMode
            );
        }
        finally
        {
            _inputCoordinationService.IsInputBlocked = previousInputBlocked;
        }
    }

    private async void OnTilesUpdated(object? sender, TileUpdateEventArgs e)
    {
        UpdateWallOverlay(e.WallAfterMove);
        UpdateBoardAccessibilityDescription();

        // In Adversarial mode, update which cells are accessible (only empty cells).
        if (_viewModel.IsAdversarialMode)
        {
            UpdateTileCellsAccessibilityForMode();
        }

        await _swipePreviewInteractionService.HandleTilesUpdatedAsync(
            e,
            BuildSwipePreviewContext()
        );

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
