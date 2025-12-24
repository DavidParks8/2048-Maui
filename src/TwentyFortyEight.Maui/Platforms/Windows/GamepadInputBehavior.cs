using TwentyFortyEight.Core;
using Windows.Gaming.Input;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// Windows-specific gamepad input handling using Windows.Gaming.Input APIs.
/// Supports Xbox controllers and other XInput-compatible gamepads.
/// </summary>
public partial class GamepadInputBehavior
{
    private Gamepad? _gamepad;
    private IDispatcherTimer? _pollingTimer;
    private GamepadReading _lastReading;

    /// <summary>
    /// Threshold for D-pad and thumbstick input to register as a direction.
    /// </summary>
    private const double ThumbstickThreshold = 0.5;

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
        StopPolling();
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        // Subscribe to gamepad added/removed events
        Gamepad.GamepadAdded += OnGamepadAdded;
        Gamepad.GamepadRemoved += OnGamepadRemoved;

        // Check if a gamepad is already connected
        if (Gamepad.Gamepads.Count > 0)
        {
            _gamepad = Gamepad.Gamepads[0];
            StartPolling();
        }
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
    {
        Gamepad.GamepadAdded -= OnGamepadAdded;
        Gamepad.GamepadRemoved -= OnGamepadRemoved;
        StopPolling();
    }

    private void OnGamepadAdded(object? sender, Gamepad e)
    {
        if (_gamepad == null)
        {
            _gamepad = e;
            AttachedPage?.Dispatcher.Dispatch(StartPolling);
        }
    }

    private void OnGamepadRemoved(object? sender, Gamepad e)
    {
        if (_gamepad == e)
        {
            _gamepad = null;
            AttachedPage?.Dispatcher.Dispatch(StopPolling);

            // Try to use another connected gamepad
            if (Gamepad.Gamepads.Count > 0)
            {
                _gamepad = Gamepad.Gamepads[0];
                AttachedPage?.Dispatcher.Dispatch(StartPolling);
            }
        }
    }

    private void StartPolling()
    {
        if (_pollingTimer != null)
        {
            return;
        }

        _pollingTimer = AttachedPage?.Dispatcher.CreateTimer();
        if (_pollingTimer != null)
        {
            _pollingTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS polling
            _pollingTimer.Tick += OnPollingTick;
            _pollingTimer.Start();
        }
    }

    private void StopPolling()
    {
        if (_pollingTimer != null)
        {
            _pollingTimer.Stop();
            _pollingTimer.Tick -= OnPollingTick;
            _pollingTimer = null;
        }
    }

    private void OnPollingTick(object? sender, EventArgs e)
    {
        if (_gamepad == null)
        {
            return;
        }

        var reading = _gamepad.GetCurrentReading();
        var direction = ProcessInput(reading);

        if (direction.HasValue && DateTime.UtcNow - _lastInputTime > InputCooldown)
        {
            _lastInputTime = DateTime.UtcNow;
            OnDirectionPressed(direction.Value);
        }

        _lastReading = reading;
    }

    private Direction? ProcessInput(GamepadReading reading)
    {
        // Check D-pad first (higher priority)
        var dpadDirection = GetDpadDirection(reading.Buttons, _lastReading.Buttons);
        if (dpadDirection.HasValue)
        {
            return dpadDirection;
        }

        // Check left thumbstick
        return GetThumbstickDirection(reading.LeftThumbstickX, reading.LeftThumbstickY);
    }

    private static Direction? GetDpadDirection(
        GamepadButtons currentButtons,
        GamepadButtons previousButtons
    )
    {
        // Only trigger on button press (not held)
        var newlyPressed = currentButtons & ~previousButtons;

        if ((newlyPressed & GamepadButtons.DPadUp) != 0)
        {
            return Direction.Up;
        }

        if ((newlyPressed & GamepadButtons.DPadDown) != 0)
        {
            return Direction.Down;
        }

        if ((newlyPressed & GamepadButtons.DPadLeft) != 0)
        {
            return Direction.Left;
        }

        if ((newlyPressed & GamepadButtons.DPadRight) != 0)
        {
            return Direction.Right;
        }

        return null;
    }

    private static Direction? GetThumbstickDirection(double x, double y)
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
            // Note: Y-axis is inverted (up is positive)
            if (y > ThumbstickThreshold)
            {
                return Direction.Up;
            }

            if (y < -ThumbstickThreshold)
            {
                return Direction.Down;
            }
        }

        return null;
    }
}
