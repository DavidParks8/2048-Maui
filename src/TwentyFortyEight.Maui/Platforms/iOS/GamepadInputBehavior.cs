using GameController;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// iOS-specific gamepad input handling using Apple's GameController framework.
/// Supports MFi controllers, Xbox controllers, PlayStation controllers, etc.
/// </summary>
public partial class GamepadInputBehavior
{
    private GCController? _controller;
    private Foundation.NSObject? _connectObserver;
    private Foundation.NSObject? _disconnectObserver;

    /// <summary>
    /// Cooldown period between direction inputs to prevent rapid-fire moves.
    /// </summary>
    private static readonly TimeSpan InputCooldown = TimeSpan.FromMilliseconds(200);

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
        CleanupController();
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        // Subscribe to controller connection notifications
        _connectObserver = Foundation.NSNotificationCenter.DefaultCenter.AddObserver(
            GCController.DidConnectNotification,
            OnControllerConnected
        );

        _disconnectObserver = Foundation.NSNotificationCenter.DefaultCenter.AddObserver(
            GCController.DidDisconnectNotification,
            OnControllerDisconnected
        );

        // Check for already connected controllers
        var controllers = GCController.Controllers;
        if (controllers.Length > 0)
        {
            SetupController(controllers[0]);
        }
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
    {
        CleanupController();
    }

    private void CleanupController()
    {
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

        _controller = null;
    }

    private void OnControllerConnected(Foundation.NSNotification notification)
    {
        if (_controller == null && notification.Object is GCController controller)
        {
            SetupController(controller);
        }
    }

    private void OnControllerDisconnected(Foundation.NSNotification notification)
    {
        if (notification.Object is GCController controller && controller == _controller)
        {
            _controller = null;

            // Try to use another connected controller
            var controllers = GCController.Controllers;
            if (controllers.Length > 0)
            {
                SetupController(controllers[0]);
            }
        }
    }

    private void SetupController(GCController controller)
    {
        _controller = controller;

        // Set up input handlers for extended gamepad (most common)
        if (controller.ExtendedGamepad != null)
        {
            SetupExtendedGamepad(controller.ExtendedGamepad);
        }
        else if (controller.MicroGamepad != null)
        {
            // For Siri Remote and similar simple controllers
            SetupMicroGamepad(controller.MicroGamepad);
        }
    }

    private void SetupExtendedGamepad(GCExtendedGamepad gamepad)
    {
        // D-pad handlers
        gamepad.Dpad.Up.PressedChangedHandler = (_, _, pressed) =>
        {
            if (pressed)
            {
                HandleDirection(Direction.Up);
            }
        };

        gamepad.Dpad.Down.PressedChangedHandler = (_, _, pressed) =>
        {
            if (pressed)
            {
                HandleDirection(Direction.Down);
            }
        };

        gamepad.Dpad.Left.PressedChangedHandler = (_, _, pressed) =>
        {
            if (pressed)
            {
                HandleDirection(Direction.Left);
            }
        };

        gamepad.Dpad.Right.PressedChangedHandler = (_, _, pressed) =>
        {
            if (pressed)
            {
                HandleDirection(Direction.Right);
            }
        };

        // Left thumbstick handler
        gamepad.LeftThumbstick.ValueChangedHandler = (_, xValue, yValue) =>
        {
            var direction = GetThumbstickDirection(xValue, yValue);
            if (direction.HasValue)
            {
                HandleDirection(direction.Value);
            }
        };
    }

    private void SetupMicroGamepad(GCMicroGamepad gamepad)
    {
        // D-pad handlers for micro gamepad (Siri Remote)
        gamepad.Dpad.Up.PressedChangedHandler = (_, _, pressed) =>
        {
            if (pressed)
            {
                HandleDirection(Direction.Up);
            }
        };

        gamepad.Dpad.Down.PressedChangedHandler = (_, _, pressed) =>
        {
            if (pressed)
            {
                HandleDirection(Direction.Down);
            }
        };

        gamepad.Dpad.Left.PressedChangedHandler = (_, _, pressed) =>
        {
            if (pressed)
            {
                HandleDirection(Direction.Left);
            }
        };

        gamepad.Dpad.Right.PressedChangedHandler = (_, _, pressed) =>
        {
            if (pressed)
            {
                HandleDirection(Direction.Right);
            }
        };
    }

    private void HandleDirection(Direction direction)
    {
        if (DateTime.UtcNow - _lastInputTime > InputCooldown)
        {
            _lastInputTime = DateTime.UtcNow;

            // Dispatch to main thread
            AttachedPage?.Dispatcher.Dispatch(() => OnDirectionPressed(direction));
        }
    }

    private static Direction? GetThumbstickDirection(float x, float y)
    {
        const float threshold = 0.5f;

        // Determine primary direction based on thumbstick position
        if (Math.Abs(x) > Math.Abs(y))
        {
            if (x > threshold)
            {
                return Direction.Right;
            }

            if (x < -threshold)
            {
                return Direction.Left;
            }
        }
        else
        {
            // Y-axis: positive is up on iOS
            if (y > threshold)
            {
                return Direction.Up;
            }

            if (y < -threshold)
            {
                return Direction.Down;
            }
        }

        return null;
    }
}
