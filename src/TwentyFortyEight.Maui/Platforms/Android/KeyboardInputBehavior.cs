using Android.Views;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// Android-specific keyboard input handling using Android key events.
/// Supports arrow keys (DPAD) and WASD when a hardware keyboard is available.
/// </summary>
public partial class KeyboardInputBehavior
{
    private Android.Views.View? _nativeView;
    private Android.App.Activity? _activity;

    /// <summary>
    /// Cooldown period between direction inputs to prevent rapid-fire moves.
    /// </summary>
    private static readonly TimeSpan InputCooldown = TimeSpan.FromMilliseconds(150);

    private DateTime _lastInputTime = DateTime.MinValue;

    partial void AttachPlatformHandler(ContentPage page)
    {
        page.Loaded += OnPageLoaded;
    }

    partial void DetachPlatformHandler(ContentPage page)
    {
        page.Loaded -= OnPageLoaded;
        DetachFromActivity();
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        AttachToActivity();
    }

    private void AttachToActivity()
    {
        if (AttachedPage?.GetParentWindow()?.Handler?.PlatformView is Android.App.Activity activity)
        {
            _activity = activity;

            var decorView = activity.Window?.DecorView;
            if (decorView != null)
            {
                _nativeView = decorView;
                decorView.SetOnKeyListener(new KeyboardKeyListener(this));
            }
        }
    }

    private void DetachFromActivity()
    {
        if (_nativeView != null)
        {
            _nativeView.SetOnKeyListener(null);
            _nativeView = null;
        }

        _activity = null;
    }

    private bool ProcessKeyEvent(Keycode keyCode, KeyEvent? e)
    {
        // Only process key down events
        if (e?.Action != KeyEventActions.Down)
        {
            return false;
        }

        // Only handle events from physical keyboards.
        // (Soft keyboards typically don't generate these decor-view key events.)
        if (!IsKeyboardDevice(e.Device))
        {
            return false;
        }

        Direction? direction = keyCode switch
        {
            Keycode.DpadUp => Direction.Up,
            Keycode.DpadDown => Direction.Down,
            Keycode.DpadLeft => Direction.Left,
            Keycode.DpadRight => Direction.Right,
            Keycode.W => Direction.Up,
            Keycode.S => Direction.Down,
            Keycode.A => Direction.Left,
            Keycode.D => Direction.Right,
            _ => null,
        };

        if (!direction.HasValue)
        {
            return false;
        }

        if (DateTime.UtcNow - _lastInputTime <= InputCooldown)
        {
            return true;
        }

        _lastInputTime = DateTime.UtcNow;
        AttachedPage?.Dispatcher.Dispatch(() => OnDirectionPressed(direction.Value));
        return true;
    }

    private static bool IsKeyboardDevice(InputDevice? device)
    {
        if (device == null)
        {
            return false;
        }

        var sources = device.Sources;
        return (sources & InputSourceType.Keyboard) == InputSourceType.Keyboard;
    }

    private sealed class KeyboardKeyListener(KeyboardInputBehavior behavior)
        : Java.Lang.Object,
            Android.Views.View.IOnKeyListener
    {
        public bool OnKey(Android.Views.View? v, Keycode keyCode, KeyEvent? e)
        {
            return behavior.ProcessKeyEvent(keyCode, e);
        }
    }
}
