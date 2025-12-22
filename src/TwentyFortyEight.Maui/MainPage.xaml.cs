using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Models;
using TwentyFortyEight.Maui.ViewModels;

namespace TwentyFortyEight.Maui;

public partial class MainPage : ContentPage
{
    private readonly GameViewModel _viewModel;
    private Point _swipeStartPoint;
    private bool _isPanning;
    private readonly Dictionary<TileViewModel, Border> _tileBorders = new();

    public MainPage(GameViewModel viewModel)
    {
        InitializeComponent();
        
        _viewModel = viewModel;
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
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
        {
            nativeWindow.Content.KeyDown += OnWindowsKeyDown;
            
            // Set up manipulation events for better touch support
            if (nativeWindow.Content is Microsoft.UI.Xaml.UIElement content)
            {
                content.ManipulationMode = Microsoft.UI.Xaml.Input.ManipulationModes.TranslateX | 
                                           Microsoft.UI.Xaml.Input.ManipulationModes.TranslateY;
                content.ManipulationStarted += OnManipulationStarted;
                content.ManipulationCompleted += OnManipulationCompleted;
            }
        }
    }

    private Windows.Foundation.Point _manipulationStart;

    private void OnManipulationStarted(object sender, Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
    {
        _manipulationStart = e.Position;
    }

    private void OnManipulationCompleted(object sender, Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
    {
        var deltaX = e.Cumulative.Translation.X;
        var deltaY = e.Cumulative.Translation.Y;
        
        const double minSwipeDistance = 30;
        
        if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            if (Math.Abs(deltaX) > minSwipeDistance)
            {
                var direction = deltaX > 0 ? Direction.Right : Direction.Left;
                _viewModel.MoveCommand.Execute(direction);
            }
        }
        else
        {
            if (Math.Abs(deltaY) > minSwipeDistance)
            {
                var direction = deltaY > 0 ? Direction.Down : Direction.Up;
                _viewModel.MoveCommand.Execute(direction);
            }
        }
        
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
            _ => null
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

    private void CreateTiles()
    {
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
                    VerticalOptions = LayoutOptions.Center
                }
            };

            border.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 5 };

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

    private async void OnTilesUpdated(object? sender, TileUpdateEventArgs e)
    {
        // Animate sliding tiles first (moved tiles that received new values)
        var slideTileTasks = e.MovedTiles.Select(async tile =>
        {
            if (_tileBorders.TryGetValue(tile, out var border))
            {
                // Calculate translation offset based on move direction
                // Tiles appear to slide FROM the direction they came from
                double translateX = 0;
                double translateY = 0;
                const double slideDistance = 20; // Subtle slide distance

                switch (e.MoveDirection)
                {
                    case Direction.Up:
                        translateY = slideDistance; // Came from below
                        break;
                    case Direction.Down:
                        translateY = -slideDistance; // Came from above
                        break;
                    case Direction.Left:
                        translateX = slideDistance; // Came from right
                        break;
                    case Direction.Right:
                        translateX = -slideDistance; // Came from left
                        break;
                }

                // Set initial translation
                border.TranslationX = translateX;
                border.TranslationY = translateY;

                // Animate back to original position
                await Task.WhenAll(
                    border.TranslateToAsync(0, 0, 100, Easing.CubicOut)
                );
            }
        });

        // Animate new tiles (pop-in effect)
        var newTileTasks = e.NewTiles.Select(async tile =>
        {
            if (_tileBorders.TryGetValue(tile, out var border))
            {
                // Start invisible and scaled down
                border.Opacity = 0;
                border.Scale = 0;
                
                // Animate pop-in: scale 0 -> 1.1 -> 1.0 with fade in
                await Task.WhenAll(
                    border.FadeToAsync(1, 75, Easing.CubicOut),
                    border.ScaleToAsync(1.1, 75, Easing.CubicOut)
                );
                await border.ScaleToAsync(1.0, 75, Easing.CubicIn);
            }
        });

        // Animate merged tiles (pulse effect)
        var mergedTileTasks = e.MergedTiles.Select(async tile =>
        {
            if (_tileBorders.TryGetValue(tile, out var border))
            {
                // Pulse: scale 1.0 -> 1.2 -> 1.0
                await border.ScaleToAsync(1.2, 75, Easing.CubicOut);
                await border.ScaleToAsync(1.0, 75, Easing.CubicIn);
            }
        });

        // Wait for slide animations to complete first, then do pop-in and merge
        await Task.WhenAll(slideTileTasks);
        await Task.WhenAll(newTileTasks.Concat(mergedTileTasks));
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        const double minSwipeDistance = 30;
        
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isPanning = true;
                _swipeStartPoint = new Point(0, 0);
                break;

            case GestureStatus.Running:
                // Track the cumulative pan distance
                if (_isPanning)
                {
                    _swipeStartPoint = new Point(e.TotalX, e.TotalY);
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_isPanning)
                {
                    _isPanning = false;
                    var deltaX = _swipeStartPoint.X;
                    var deltaY = _swipeStartPoint.Y;

                    // Determine direction based on larger delta
                    if (Math.Abs(deltaX) > Math.Abs(deltaY))
                    {
                        // Horizontal swipe
                        if (Math.Abs(deltaX) > minSwipeDistance)
                        {
                            var direction = deltaX > 0 ? Direction.Right : Direction.Left;
                            _viewModel.MoveCommand.Execute(direction);
                        }
                    }
                    else
                    {
                        // Vertical swipe
                        if (Math.Abs(deltaY) > minSwipeDistance)
                        {
                            var direction = deltaY > 0 ? Direction.Down : Direction.Up;
                            _viewModel.MoveCommand.Execute(direction);
                        }
                    }
                }
                break;
        }
    }
}
