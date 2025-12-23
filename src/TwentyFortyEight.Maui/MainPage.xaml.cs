using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Models;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.Maui.ViewModels;

namespace TwentyFortyEight.Maui;

public partial class MainPage : ContentPage
{
    private readonly GameViewModel _viewModel;
    private readonly TileAnimationService _animationService;
    private Point _swipeStartPoint;
    private readonly Dictionary<TileViewModel, Border> _tileBorders = new();
    private CancellationTokenSource? _animationCts;

#if WINDOWS
    private Microsoft.UI.Xaml.UIElement? _windowsContent;
#endif

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

        // Add pan gesture for swipe detection (works better than SwipeGestureRecognizer)
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GameBoard.GestureRecognizers.Add(panGesture);

        // Set up keyboard handling after page loads
        this.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        // Focus the page to receive keyboard input
        this.Focus();

#if WINDOWS
        SetupWindowsInputHandling();
#endif
    }

#if WINDOWS
    private void SetupWindowsInputHandling()
    {
        var window = this.GetParentWindow();
        if (
            window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow
            && nativeWindow.Content is Microsoft.UI.Xaml.UIElement content
        )
        {
            _windowsContent = content;
            content.KeyDown += OnWindowsKeyDown;

            // Set up manipulation events for better touch support
            content.ManipulationMode =
                Microsoft.UI.Xaml.Input.ManipulationModes.TranslateX
                | Microsoft.UI.Xaml.Input.ManipulationModes.TranslateY;
            content.ManipulationCompleted += OnManipulationCompleted;
        }
    }

    private void CleanupWindowsInputHandling()
    {
        if (_windowsContent is not null)
        {
            _windowsContent.KeyDown -= OnWindowsKeyDown;
            _windowsContent.ManipulationCompleted -= OnManipulationCompleted;
            _windowsContent = null;
        }
    }

    private void OnManipulationCompleted(
        object sender,
        Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e
    )
    {
        ProcessSwipe(e.Cumulative.Translation.X, e.Cumulative.Translation.Y);
        e.Handled = true;
    }

    private void OnWindowsKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        Direction? direction = e.Key switch
        {
            Windows.System.VirtualKey.Up => Direction.Up,
            Windows.System.VirtualKey.Down => Direction.Down,
            Windows.System.VirtualKey.Left => Direction.Left,
            Windows.System.VirtualKey.Right => Direction.Right,
            Windows.System.VirtualKey.W => Direction.Up,
            Windows.System.VirtualKey.S => Direction.Down,
            Windows.System.VirtualKey.A => Direction.Left,
            Windows.System.VirtualKey.D => Direction.Right,
            _ => null,
        };

        if (direction.HasValue)
        {
            _viewModel.MoveCommand.Execute(direction.Value);
            e.Handled = true;
        }
    }
#endif

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.Focus();
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

#if WINDOWS
        CleanupWindowsInputHandling();
#endif
    }

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
                    FontSize = 32,
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
                _swipeStartPoint = new Point(0, 0);
                break;

            case GestureStatus.Running:
                // Track the cumulative pan distance
                _swipeStartPoint = new Point(e.TotalX, e.TotalY);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                ProcessSwipe(_swipeStartPoint.X, _swipeStartPoint.Y);
                break;
        }
    }
}
