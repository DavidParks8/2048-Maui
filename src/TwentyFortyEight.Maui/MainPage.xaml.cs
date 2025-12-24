using System.Diagnostics.CodeAnalysis;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Behaviors;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Models;

namespace TwentyFortyEight.Maui;

public partial class MainPage : ContentPage
{
    private readonly GameViewModel _viewModel;
    private readonly TileAnimationService _animationService;
    private readonly IAccelerometerService _accelerometerService;
    private readonly Dictionary<TileViewModel, Border> _tileBorders = new();
    private CancellationTokenSource? _animationCts;
    private readonly KeyboardInputBehavior _keyboardBehavior;
    private DeviceOrientation _currentOrientation = DeviceOrientation.Portrait;

    // Touch/pointer tracking for swipe detection
    private Point? _pointerStartPoint;
    private Point _panAccumulator;

    public MainPage(
        GameViewModel viewModel,
        TileAnimationService animationService,
        IAccelerometerService accelerometerService
    )
    {
        InitializeComponent();

        _viewModel = viewModel;
        _animationService = animationService;
        _accelerometerService = accelerometerService;
        BindingContext = _viewModel;

        // Subscribe to tiles updated event for animations
        _viewModel.TilesUpdated += OnTilesUpdated;

        // Subscribe to orientation changes
        _accelerometerService.OrientationChanged += OnOrientationChanged;

        // Add tiles to the grid
        CreateTiles();

        // Set up gesture recognizers for swipe detection
        SetupGestureRecognizers();

        // Set up keyboard handling via platform behavior
        _keyboardBehavior = new KeyboardInputBehavior();
        _keyboardBehavior.DirectionPressed += OnKeyboardDirectionPressed;
        this.Behaviors.Add(_keyboardBehavior);
    }

    /// <summary>
    /// Sets up gesture recognizers for cross-platform swipe detection.
    /// Uses both Pan and Pointer gestures for maximum compatibility.
    /// </summary>
    private void SetupGestureRecognizers()
    {
        // Pan gesture for touch swipes (works on mobile)
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        RootLayout.GestureRecognizers.Add(panGesture);

        // Pointer gesture for better mouse/touch support (especially on Windows)
        var pointerGesture = new PointerGestureRecognizer();
        pointerGesture.PointerPressed += OnPointerPressed;
        pointerGesture.PointerReleased += OnPointerReleased;
        RootLayout.GestureRecognizers.Add(pointerGesture);
    }

    private void OnKeyboardDirectionPressed(object? sender, Direction direction)
    {
        _viewModel.MoveCommand.Execute(direction);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        _pointerStartPoint = e.GetPosition(RootLayout);
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (_pointerStartPoint is null)
            return;

        var endPoint = e.GetPosition(RootLayout);
        if (endPoint is null)
            return;

        var deltaX = endPoint.Value.X - _pointerStartPoint.Value.X;
        var deltaY = endPoint.Value.Y - _pointerStartPoint.Value.Y;

        ProcessSwipe(deltaX, deltaY);

        _pointerStartPoint = null;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Re-subscribe to events (they are unsubscribed in OnDisappearing)
        // Unsubscribe first to prevent duplicate handlers if OnAppearing is called multiple times
        _viewModel.TilesUpdated -= OnTilesUpdated;
        _keyboardBehavior.DirectionPressed -= OnKeyboardDirectionPressed;
        _accelerometerService.OrientationChanged -= OnOrientationChanged;

        _viewModel.TilesUpdated += OnTilesUpdated;
        _keyboardBehavior.DirectionPressed += OnKeyboardDirectionPressed;
        _accelerometerService.OrientationChanged += OnOrientationChanged;

        // Start accelerometer monitoring if supported
        if (_accelerometerService.IsSupported)
        {
            _accelerometerService.Start();
        }
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
        _keyboardBehavior.DirectionPressed -= OnKeyboardDirectionPressed;
        _accelerometerService.OrientationChanged -= OnOrientationChanged;

        // Stop accelerometer monitoring
        _accelerometerService.Stop();
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "String-based bindings are safe here as the types are preserved by MAUI and the binding context is set explicitly."
    )]
    private void CreateTiles()
    {
        var boardSize = _viewModel.BoardSize;

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
            var border = new Border
            {
                Stroke = Colors.Transparent,
                StrokeThickness = 0,
                Padding = 0,
                BackgroundColor = tile.BackgroundColor,
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
            border.SetBinding(Border.BackgroundColorProperty, nameof(tile.BackgroundColor));

            var label = (Label)border.Content;
            label.SetBinding(Label.TextProperty, nameof(tile.DisplayValue));
            label.SetBinding(Label.TextColorProperty, nameof(tile.TextColor));
            label.SetBinding(Label.FontSizeProperty, nameof(tile.FontSize));

            border.BindingContext = tile;

            Grid.SetRow(border, tile.Row);
            Grid.SetColumn(border, tile.Column);

            // Store the mapping
            _tileBorders[tile] = border;

            GameBoard.Children.Add(border);
        }
    }

    /// <summary>
    /// Minimum distance in pixels required to register a swipe gesture.
    /// </summary>
    private const double MinSwipeDistance = 30;

    /// <summary>
    /// Processes a swipe gesture and executes the corresponding move command.
    /// </summary>
    /// <param name="deltaX">Horizontal displacement.</param>
    /// <param name="deltaY">Vertical displacement.</param>
    private void ProcessSwipe(double deltaX, double deltaY)
    {
        Direction? direction = null;

        if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            if (Math.Abs(deltaX) > MinSwipeDistance)
            {
                direction = deltaX > 0 ? Direction.Right : Direction.Left;
            }
        }
        else
        {
            if (Math.Abs(deltaY) > MinSwipeDistance)
            {
                direction = deltaY > 0 ? Direction.Down : Direction.Up;
            }
        }

        if (direction.HasValue)
        {
            _viewModel.MoveCommand.Execute(direction.Value);
        }
    }

    private async void OnTilesUpdated(object? sender, TileUpdateEventArgs e)
    {
        // Cancel any pending animations before starting new ones
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = new CancellationTokenSource();

        try
        {
            await _animationService.AnimateAsync(
                e,
                GameBoard,
                _viewModel.BoardSize,
                _tileBorders,
                _animationCts.Token
            );
        }
        catch (OperationCanceledException)
        {
            // Animation was cancelled - reset tile states to ensure consistent UI
            TileAnimationService.ResetTileStates(GameBoard, _tileBorders);
        }
        catch (Exception ex)
        {
            // Log but don't crash - animations are non-critical
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
        finally
        {
            // Signal the ViewModel that animation is complete so next move can proceed
            _viewModel.SignalAnimationComplete();
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panAccumulator = new Point(0, 0);
                break;

            case GestureStatus.Running:
                // Track the cumulative pan distance
                _panAccumulator = new Point(e.TotalX, e.TotalY);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                ProcessSwipe(_panAccumulator.X, _panAccumulator.Y);
                break;
        }
    }

    private async void OnOrientationChanged(object? sender, OrientationChangedEventArgs e)
    {
        try
        {
            if (_currentOrientation == e.Orientation)
                return;

            _currentOrientation = e.Orientation;

            // Animate layout changes based on orientation
            await AnimateLayoutForOrientation(e.Orientation);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error during orientation change animation: {ex.Message}"
            );
        }
    }

    private async Task AnimateLayoutForOrientation(DeviceOrientation orientation)
    {
        const uint animationDuration = 300;

        if (orientation == DeviceOrientation.Landscape)
        {
            // In landscape mode, move scores and buttons to the right side of the board
            // Fade out first
            await Task.WhenAll(
                ScoreContainer.FadeToAsync(0, animationDuration / 2),
                ControlsContainer.FadeToAsync(0, animationDuration / 2)
            );

            // Position both containers in row 2, but at different vertical positions
            Grid.SetRow(ScoreContainer, 2);
            Grid.SetColumn(ScoreContainer, 0);
            Grid.SetColumnSpan(ScoreContainer, 1);
            Grid.SetRowSpan(ScoreContainer, 1);

            Grid.SetRow(ControlsContainer, 3);
            Grid.SetColumn(ControlsContainer, 0);

            // Change positioning for landscape - float to the right side
            ScoreContainer.HorizontalOptions = LayoutOptions.End;
            ScoreContainer.VerticalOptions = LayoutOptions.Start;
            ScoreContainer.Margin = new Thickness(20, 0, 0, 0);

            ControlsContainer.HorizontalOptions = LayoutOptions.End;
            ControlsContainer.VerticalOptions = LayoutOptions.Start;
            ControlsContainer.Margin = new Thickness(20, 0, 0, 0);

            // Fade back in
            await Task.WhenAll(
                ScoreContainer.FadeToAsync(1, animationDuration / 2),
                ControlsContainer.FadeToAsync(1, animationDuration / 2)
            );
        }
        else
        {
            // In portrait mode, restore original layout
            // Fade out first
            await Task.WhenAll(
                ScoreContainer.FadeToAsync(0, animationDuration / 2),
                ControlsContainer.FadeToAsync(0, animationDuration / 2)
            );

            // Restore original positions
            Grid.SetRow(ScoreContainer, 0);
            Grid.SetColumn(ScoreContainer, 1);
            Grid.SetColumnSpan(ScoreContainer, 2);
            Grid.SetRowSpan(ScoreContainer, 1);

            Grid.SetRow(ControlsContainer, 3);
            Grid.SetColumn(ControlsContainer, 0);

            // Restore original positioning
            ScoreContainer.HorizontalOptions = LayoutOptions.End;
            ScoreContainer.VerticalOptions = LayoutOptions.Fill;
            ScoreContainer.Margin = new Thickness(0);

            ControlsContainer.HorizontalOptions = LayoutOptions.Center;
            ControlsContainer.VerticalOptions = LayoutOptions.Fill;
            ControlsContainer.Margin = new Thickness(0, 10, 0, 0);

            // Fade back in
            await Task.WhenAll(
                ScoreContainer.FadeToAsync(1, animationDuration / 2),
                ControlsContainer.FadeToAsync(1, animationDuration / 2)
            );
        }
    }
}
