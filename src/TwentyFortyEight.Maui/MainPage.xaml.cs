using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Models;
using TwentyFortyEight.Maui.ViewModels;

namespace TwentyFortyEight.Maui;

public partial class MainPage : ContentPage
{
    private readonly GameViewModel _viewModel;
    private Point _swipeStartPoint;
    private bool _isPanning;
    private bool _isAnimating;
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
                content.ManipulationMode =
                    Microsoft.UI.Xaml.Input.ManipulationModes.TranslateX
                    | Microsoft.UI.Xaml.Input.ManipulationModes.TranslateY;
                content.ManipulationStarted += OnManipulationStarted;
                content.ManipulationCompleted += OnManipulationCompleted;
            }
        }
    }

    private Windows.Foundation.Point _manipulationStart;

    private void OnManipulationStarted(
        object sender,
        Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e
    )
    {
        _manipulationStart = e.Position;
    }

    private void OnManipulationCompleted(
        object sender,
        Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e
    )
    {
        // Ignore input while animations are running
        if (_isAnimating)
        {
            e.Handled = true;
            return;
        }

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
        // Ignore input while animations are running
        if (_isAnimating)
        {
            e.Handled = true;
            return;
        }

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

    private async void OnTilesUpdated(object? sender, TileUpdateEventArgs e)
    {
        // Set animation flag to block input during animations
        _isAnimating = true;

        try
        {
            // Calculate cell step size (distance between adjacent cell centers)
            // For a grid with N columns, spacing S, and total width W:
            // Step = (W + S) / N (accounts for cell width plus one spacing gap)
            const double spacing = 10;
            const int gridSize = 4;
            var cellStepX = (GameBoard.Width + spacing) / gridSize;
            var cellStepY = (GameBoard.Height + spacing) / gridSize;

            // If we can't get valid dimensions, use defaults
            if (cellStepX <= 0 || cellStepY <= 0)
            {
                // Default: (400 + 10) / 4 = 102.5
                cellStepX = 102.5;
                cellStepY = 102.5;
            }

            // Create overlay tiles for sliding animations
            var overlayTiles = new List<Border>();
            var slideAnimationTasks = new List<Task>();

            // Hide new tiles immediately (they will appear after all other animations)
            // Only hide visually - DO NOT change the tile value to avoid race conditions
            foreach (var tile in e.NewTiles)
            {
                // Only hide the border completely during slide animation
                // This prevents visual glitches when spawning into a position a tile is leaving
                if (_tileBorders.TryGetValue(tile, out var border))
                {
                    border.Opacity = 0;
                    border.Scale = 0;
                }
            }

            // Hide merged tiles initially (they will appear after slide animation)
            foreach (var tile in e.MergedTiles)
            {
                if (_tileBorders.TryGetValue(tile, out var border))
                {
                    border.Opacity = 0;
                    border.Scale = 1;
                }
            }

            // Animate all tile movements using overlay tiles
            foreach (var movement in e.TileMovements)
            {
                // Create an overlay tile at the source position
                var overlayBorder = CreateOverlayTile(movement.Value);
                overlayTiles.Add(overlayBorder);

                // Position the overlay at the source location
                Grid.SetRow(overlayBorder, movement.From.Row);
                Grid.SetColumn(overlayBorder, movement.From.Column);
                GameBoard.Children.Add(overlayBorder);

                // Calculate the translation needed to move from source to destination
                var translateX = (movement.To.Column - movement.From.Column) * cellStepX;
                var translateY = (movement.To.Row - movement.From.Row) * cellStepY;

                // Animate the overlay tile sliding to the destination
                slideAnimationTasks.Add(
                    Task.WhenAll(
                        overlayBorder.TranslateToAsync(translateX, translateY, 220, Easing.CubicOut)
                    )
                );
            }

            // Wait for all slide animations to complete
            if (slideAnimationTasks.Count > 0)
            {
                await Task.WhenAll(slideAnimationTasks);
            }

            // Remove overlay tiles
            foreach (var overlay in overlayTiles)
            {
                GameBoard.Children.Remove(overlay);
            }

            // Now show the final tiles at their destinations
            // Animate merged tiles (pulse effect) - they appear now after sliding
            var mergedTileTasks = e
                .MergedTiles.Select(async tile =>
                {
                    if (_tileBorders.TryGetValue(tile, out var border))
                    {
                        // Show the merged tile and pulse
                        border.Opacity = 1;
                        border.Scale = 0.8;
                        await border.ScaleToAsync(1.2, 100, Easing.CubicOut);
                        await border.ScaleToAsync(1.0, 75, Easing.CubicIn);
                    }
                })
                .ToList(); // Materialize to start all tasks immediately

            // Wait for merged tile animations to complete first
            await Task.WhenAll(mergedTileTasks);

            // Then animate new tiles - scale up from small
            var newTileTasks = e
                .NewTiles.Select(async tile =>
                {
                    if (_tileBorders.TryGetValue(tile, out var border))
                    {
                        // Ensure scale is 0 and make visible
                        border.Scale = 0;
                        border.Opacity = 1;

                        // Small delay to ensure the UI has updated before animating
                        await Task.Delay(10);

                        // Animate scale from 0 to 1
                        await border.ScaleToAsync(1.0, 100, Easing.CubicOut);
                    }
                })
                .ToList(); // Materialize to start all tasks immediately

            await Task.WhenAll(newTileTasks);
        }
        finally
        {
            // Always clear the animation flag when done
            _isAnimating = false;
        }
    }

    private static Border CreateOverlayTile(int value)
    {
        var backgroundColor = TileViewModel.GetTileBackgroundColor(value);
        var textColor = TileViewModel.GetTileTextColor(value);

        var border = new Border
        {
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            Padding = 0,
            BackgroundColor = backgroundColor,
            ZIndex = 100, // Ensure overlay is on top
            Content = new Label
            {
                Text = value.ToString(),
                FontSize = 32,
                FontAttributes = FontAttributes.Bold,
                TextColor = textColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            },
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 5 },
        };

        return border;
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

                    // Ignore input while animations are running
                    if (_isAnimating)
                    {
                        break;
                    }

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
