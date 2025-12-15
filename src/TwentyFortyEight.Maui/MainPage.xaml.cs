using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.ViewModels;

namespace TwentyFortyEight.Maui;

public partial class MainPage : ContentPage
{
    private readonly GameViewModel _viewModel;
    private Point _swipeStartPoint;

    public MainPage(GameViewModel viewModel)
    {
        InitializeComponent();
        
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Add tiles to the grid
        CreateTiles();
        
        // Add swipe gesture
        var swipeGesture = new SwipeGestureRecognizer();
        swipeGesture.Swiped += OnSwiped;
        GameBoard.GestureRecognizers.Add(swipeGesture);

        // Add pan gesture for better swipe detection
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GameBoard.GestureRecognizers.Add(panGesture);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Keyboard handling is done via HotKey support in .NET MAUI
        // For Windows: Keyboard accelerators work automatically via menu items/shortcuts
        // For mobile: Touch gestures are the primary input method
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
            
            GameBoard.Children.Add(border);
        }
    }

    private void OnSwiped(object? sender, SwipedEventArgs e)
    {
        var direction = e.Direction switch
        {
            SwipeDirection.Up => Direction.Up,
            SwipeDirection.Down => Direction.Down,
            SwipeDirection.Left => Direction.Left,
            SwipeDirection.Right => Direction.Right,
            _ => Direction.Up
        };

        _viewModel.MoveCommand.Execute(direction);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _swipeStartPoint = new Point(e.TotalX, e.TotalY);
                break;

            case GestureStatus.Completed:
                var deltaX = e.TotalX - _swipeStartPoint.X;
                var deltaY = e.TotalY - _swipeStartPoint.Y;

                // Determine direction based on larger delta
                if (Math.Abs(deltaX) > Math.Abs(deltaY))
                {
                    // Horizontal swipe
                    if (Math.Abs(deltaX) > 50) // Minimum swipe distance
                    {
                        var direction = deltaX > 0 ? Direction.Right : Direction.Left;
                        _viewModel.MoveCommand.Execute(direction);
                    }
                    // Do nothing if swipe distance is too small
                }
                else
                {
                    // Vertical swipe
                    if (Math.Abs(deltaY) > 50) // Minimum swipe distance
                    {
                        var direction = deltaY > 0 ? Direction.Down : Direction.Up;
                        _viewModel.MoveCommand.Execute(direction);
                    }
                    // Do nothing if swipe distance is too small
                }
                break;
        }
    }
}
