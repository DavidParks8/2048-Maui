using GameController;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// iOS-specific keyboard input handling using Apple's GameController framework (GCKeyboard).
/// Supports arrow keys and WASD when a hardware keyboard is connected.
/// </summary>
public partial class KeyboardInputBehavior
{
    private GCKeyboard? _keyboard;
    private Foundation.NSObject? _connectObserver;
    private Foundation.NSObject? _disconnectObserver;

    /// <summary>
    /// Cooldown period between direction inputs to prevent rapid-fire moves.
    /// </summary>
    private static readonly TimeSpan InputCooldown = TimeSpan.FromMilliseconds(150);

    private DateTime _lastInputTime = DateTime.MinValue;

    partial void AttachPlatformHandler(ContentPage page)
    {
        page.Loaded += OnPageLoaded;
        page.Unloaded += OnPageUnloaded;
    }

    partial void DetachPlatformHandler(ContentPage page)
    {
        page.Loaded -= OnPageLoaded;
        page.Unloaded -= OnPageUnloaded;
        CleanupKeyboard();
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        _connectObserver = Foundation.NSNotificationCenter.DefaultCenter.AddObserver(
            GCKeyboard.DidConnectNotification,
            OnKeyboardConnected
        );

        _disconnectObserver = Foundation.NSNotificationCenter.DefaultCenter.AddObserver(
            GCKeyboard.DidDisconnectNotification,
            OnKeyboardDisconnected
        );

        // CoalescedKeyboard is non-null when a hardware keyboard is available.
        if (GCKeyboard.CoalescedKeyboard != null)
        {
            SetupKeyboard(GCKeyboard.CoalescedKeyboard);
        }
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
    {
        CleanupKeyboard();
    }

    private void OnKeyboardConnected(Foundation.NSNotification notification)
    {
        if (notification.Object is GCKeyboard keyboard)
        {
            SetupKeyboard(keyboard);
        }
        else if (GCKeyboard.CoalescedKeyboard != null)
        {
            SetupKeyboard(GCKeyboard.CoalescedKeyboard);
        }
    }

    private void OnKeyboardDisconnected(Foundation.NSNotification notification)
    {
        if (notification.Object is GCKeyboard keyboard && keyboard == _keyboard)
        {
            CleanupKeyboard();
        }
    }

    private void SetupKeyboard(GCKeyboard keyboard)
    {
        _keyboard = keyboard;

        if (_keyboard.KeyboardInput == null)
        {
            return;
        }

        _keyboard.KeyboardInput.KeyChangedHandler = OnKeyChanged;
    }

    private void CleanupKeyboard()
    {
        if (_keyboard?.KeyboardInput != null)
        {
            _keyboard.KeyboardInput.KeyChangedHandler = null;
        }

        _keyboard = null;

        if (_connectObserver != null)
        {
            Foundation.NSNotificationCenter.DefaultCenter.RemoveObserver(_connectObserver);
            _connectObserver = null;
        }

        if (_disconnectObserver != null)
        {
            Foundation.NSNotificationCenter.DefaultCenter.RemoveObserver(_disconnectObserver);
            _disconnectObserver = null;
        }
    }

    private void OnKeyChanged(
        GCKeyboardInput keyboard,
        GCControllerButtonInput key,
        nint keyCode,
        bool pressed
    )
    {
        if (!pressed)
        {
            return;
        }

        Direction? direction = keyCode switch
        {
            var code when code == GCKeyCode.UpArrow => Direction.Up,
            var code when code == GCKeyCode.DownArrow => Direction.Down,
            var code when code == GCKeyCode.LeftArrow => Direction.Left,
            var code when code == GCKeyCode.RightArrow => Direction.Right,
            var code when code == GCKeyCode.KeyW => Direction.Up,
            var code when code == GCKeyCode.KeyS => Direction.Down,
            var code when code == GCKeyCode.KeyA => Direction.Left,
            var code when code == GCKeyCode.KeyD => Direction.Right,
            _ => null,
        };

        if (!direction.HasValue)
        {
            return;
        }

        if (DateTime.UtcNow - _lastInputTime <= InputCooldown)
        {
            return;
        }

        _lastInputTime = DateTime.UtcNow;
        AttachedPage?.Dispatcher.Dispatch(() => OnDirectionPressed(direction.Value));
    }
}
