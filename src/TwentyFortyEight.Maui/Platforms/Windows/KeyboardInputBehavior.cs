using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// Windows-specific keyboard input handling using native WinUI keyboard events.
/// </summary>
public partial class KeyboardInputBehavior
{
    private Microsoft.UI.Xaml.UIElement? _nativeElement;

    partial void AttachPlatformHandler(ContentPage page)
    {
        page.Loaded += OnPageLoaded;
    }

    partial void DetachPlatformHandler(ContentPage page)
    {
        page.Loaded -= OnPageLoaded;
        CleanupNativeHandler();
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        if (
            AttachedPage?.GetParentWindow()?.Handler?.PlatformView
                is Microsoft.UI.Xaml.Window nativeWindow
            && nativeWindow.Content is Microsoft.UI.Xaml.UIElement content
        )
        {
            _nativeElement = content;
            content.KeyDown += OnNativeKeyDown;
        }
    }

    private void CleanupNativeHandler()
    {
        if (_nativeElement is not null)
        {
            _nativeElement.KeyDown -= OnNativeKeyDown;
            _nativeElement = null;
        }
    }

    private void OnNativeKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
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
            OnDirectionPressed(direction.Value);
            e.Handled = true;
        }
    }
}
