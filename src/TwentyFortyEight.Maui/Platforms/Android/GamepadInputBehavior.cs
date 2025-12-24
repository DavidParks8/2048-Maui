using Android.Views;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// Android-specific gamepad input handling using Android's InputDevice API.
/// Supports game controllers via key events (D-pad, buttons) and motion events (thumbsticks).
/// </summary>
public partial class GamepadInputBehavior
{
    private Android.Views.View? _nativeView;
    private Android.App.Activity? _activity;

    /// <summary>
    /// Threshold for thumbstick input to register as a direction.
    /// </summary>
    private const float ThumbstickThreshold = 0.5f;

    /// <summary>
    /// Cooldown period between direction inputs to prevent rapid-fire moves.
    /// </summary>
    private static readonly TimeSpan InputCooldown = TimeSpan.FromMilliseconds(200);

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

            // Get the main content view and attach key/motion listeners
            var decorView = activity.Window?.DecorView;
            if (decorView != null)
            {
                _nativeView = decorView;
                decorView.SetOnKeyListener(new GamepadKeyListener(this));
                decorView.SetOnGenericMotionListener(new GamepadMotionListener(this));
            }
        }
    }

    private void DetachFromActivity()
    {
        if (_nativeView != null)
        {
            _nativeView.SetOnKeyListener(null);
            _nativeView.SetOnGenericMotionListener(null);
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

        // Check if this is from a gamepad
        if (!IsGamepadDevice(e.Device))
        {
            return false;
        }

        var direction = keyCode switch
        {
            Keycode.DpadUp => Direction.Up,
            Keycode.DpadDown => Direction.Down,
            Keycode.DpadLeft => Direction.Left,
            Keycode.DpadRight => Direction.Right,
            _ => (Direction?)null,
        };

        if (direction.HasValue && DateTime.UtcNow - _lastInputTime > InputCooldown)
        {
            _lastInputTime = DateTime.UtcNow;

            // Dispatch to main thread
            AttachedPage?.Dispatcher.Dispatch(() => OnDirectionPressed(direction.Value));
            return true;
        }

        return false;
    }

    private bool ProcessMotionEvent(MotionEvent? e)
    {
        if (e == null)
        {
            return false;
        }

        // Check if this is from a gamepad joystick
        if ((e.Source & InputSourceType.Joystick) == 0)
        {
            return false;
        }

        if (e.Action != MotionEventActions.Move)
        {
            return false;
        }

        // Get left thumbstick values
        var x = e.GetAxisValue(Axis.X);
        var y = e.GetAxisValue(Axis.Y);

        var direction = GetThumbstickDirection(x, y);

        if (direction.HasValue && DateTime.UtcNow - _lastInputTime > InputCooldown)
        {
            _lastInputTime = DateTime.UtcNow;

            // Dispatch to main thread
            AttachedPage?.Dispatcher.Dispatch(() => OnDirectionPressed(direction.Value));
            return true;
        }

        return false;
    }

    private static Direction? GetThumbstickDirection(float x, float y)
    {
        // Determine primary direction based on thumbstick position
        if (Math.Abs(x) > Math.Abs(y))
        {
            if (x > ThumbstickThreshold)
            {
                return Direction.Right;
            }

            if (x < -ThumbstickThreshold)
            {
                return Direction.Left;
            }
        }
        else
        {
            // Note: Y-axis is typically positive downward on Android
            if (y < -ThumbstickThreshold)
            {
                return Direction.Up;
            }

            if (y > ThumbstickThreshold)
            {
                return Direction.Down;
            }
        }

        return null;
    }

    private static bool IsGamepadDevice(InputDevice? device)
    {
        if (device == null)
        {
            return false;
        }

        var sources = device.Sources;
        return (sources & InputSourceType.Gamepad) == InputSourceType.Gamepad
            || (sources & InputSourceType.Joystick) == InputSourceType.Joystick;
    }

    /// <summary>
    /// Key listener for handling D-pad and button presses.
    /// </summary>
    private sealed class GamepadKeyListener(GamepadInputBehavior behavior)
        : Java.Lang.Object,
            Android.Views.View.IOnKeyListener
    {
        public bool OnKey(Android.Views.View? v, Keycode keyCode, KeyEvent? e)
        {
            return behavior.ProcessKeyEvent(keyCode, e);
        }
    }

    /// <summary>
    /// Motion listener for handling thumbstick movements.
    /// </summary>
    private sealed class GamepadMotionListener(GamepadInputBehavior behavior)
        : Java.Lang.Object,
            Android.Views.View.IOnGenericMotionListener
    {
        public bool OnGenericMotion(Android.Views.View? v, MotionEvent? e)
        {
            return behavior.ProcessMotionEvent(e);
        }
    }
}
