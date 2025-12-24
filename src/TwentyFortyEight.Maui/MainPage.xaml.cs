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
    private readonly Dictionary<TileViewModel, Border> _tileBorders = new();
    private CancellationTokenSource? _animationCts;
    private readonly KeyboardInputBehavior _keyboardBehavior;

    // Touch/pointer tracking for swipe detection
    private Point? _pointerStartPoint;
    private Point _panAccumulator;

    // Responsive sizing
    private const double DefaultBoardSize = 400;
    private const double MinBoardSize = 280;
    private const double MaxBoardSize = 500;

    public MainPage(GameViewModel viewModel, TileAnimationService animationService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _animationService = animationService;
        BindingContext = _viewModel;

        // Subscribe to tiles updated event for animations
        _viewModel.TilesUpdated += OnTilesUpdated;

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
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _keyboardBehavior.DirectionPressed -= OnKeyboardDirectionPressed;

        _viewModel.TilesUpdated += OnTilesUpdated;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _keyboardBehavior.DirectionPressed += OnKeyboardDirectionPressed;
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
        _keyboardBehavior.DirectionPressed -= OnKeyboardDirectionPressed;
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
        const double horizontalReserved = 80; // 20px page padding + 10px border padding + 20px safety margin on each side
        const double verticalReserved = 280; // header ~80px, status ~30px, controls ~70px, spacing ~30px, padding 40px, board padding 20px, margins ~10px

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
        _viewModel.BoardScaleFactor = boardSize / DefaultBoardSize;

        // Scale tile spacing for very small boards
        double tileSpacing = Math.Max(5, boardSize / 40);
        GameBoard.ColumnSpacing = tileSpacing;
        GameBoard.RowSpacing = tileSpacing;
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

            // Bind FontSize with scale converter
            var fontSizeBinding = new Binding
            {
                Path = nameof(tile.FontSize),
                Converter = (IValueConverter)Resources["FontSizeScaleConverter"],
                ConverterParameter = _viewModel.BoardScaleFactor,
            };

            label.SetBinding(Label.FontSizeProperty, fontSizeBinding);

            border.BindingContext = tile;

            Grid.SetRow(border, tile.Row);
            Grid.SetColumn(border, tile.Column);

            // Store the mapping
            _tileBorders[tile] = border;

            GameBoard.Children.Add(border);
        }

        // Subscribe once to BoardScaleFactor changes to update all label font size bindings
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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
                    // Create a new binding with updated ConverterParameter
                    var fontSizeBinding = new Binding
                    {
                        Path = "FontSize",
                        Converter = (IValueConverter)Resources["FontSizeScaleConverter"],
                        ConverterParameter = _viewModel.BoardScaleFactor,
                    };

                    label.SetBinding(Label.FontSizeProperty, fontSizeBinding);
                }
            }
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
                _viewModel.BoardScaleFactor,
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
}
