using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.ViewModels;

namespace TwentyFortyEight.Maui;

public partial class MainPage : ContentPage
{
    private GameViewModel ViewModel => (GameViewModel)BindingContext;
    private Point _swipeStartPoint;

    public MainPage()
    {
        InitializeComponent();
        
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
        
        // Add keyboard handler when page appears
        if (Window != null)
        {
            Window.AddKeyboardAccelerator(KeyboardAcceleratorModifiers.None, Key.Up, OnKeyUp);
            Window.AddKeyboardAccelerator(KeyboardAcceleratorModifiers.None, Key.Down, OnKeyDown);
            Window.AddKeyboardAccelerator(KeyboardAcceleratorModifiers.None, Key.Left, OnKeyLeft);
            Window.AddKeyboardAccelerator(KeyboardAcceleratorModifiers.None, Key.Right, OnKeyRight);
            
            // WASD keys
            Window.AddKeyboardAccelerator(KeyboardAcceleratorModifiers.None, Key.W, OnKeyUp);
            Window.AddKeyboardAccelerator(KeyboardAcceleratorModifiers.None, Key.S, OnKeyDown);
            Window.AddKeyboardAccelerator(KeyboardAcceleratorModifiers.None, Key.A, OnKeyLeft);
            Window.AddKeyboardAccelerator(KeyboardAcceleratorModifiers.None, Key.D, OnKeyRight);
        }
    }

    private void CreateTiles()
    {
        for (int i = 0; i < ViewModel.Tiles.Count; i++)
        {
            var tile = ViewModel.Tiles[i];
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

            border.StrokeShape = new RoundRectangle { CornerRadius = 5 };

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

        ViewModel.MoveCommand.Execute(direction);
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
                        ViewModel.MoveCommand.Execute(direction);
                    }
                }
                else
                {
                    // Vertical swipe
                    if (Math.Abs(deltaY) > 50) // Minimum swipe distance
                    {
                        var direction = deltaY > 0 ? Direction.Down : Direction.Up;
                        ViewModel.MoveCommand.Execute(direction);
                    }
                }
                break;
        }
    }

    private void OnKeyUp() => ViewModel.MoveCommand.Execute(Direction.Up);
    private void OnKeyDown() => ViewModel.MoveCommand.Execute(Direction.Down);
    private void OnKeyLeft() => ViewModel.MoveCommand.Execute(Direction.Left);
    private void OnKeyRight() => ViewModel.MoveCommand.Execute(Direction.Right);
}
