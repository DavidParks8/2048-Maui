using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Behaviors;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for coordinating input from multiple sources (keyboard, gamepad, scroll).
/// Manages behavior lifecycle and centralizes input blocking logic.
/// </summary>
public class InputCoordinationService : IInputCoordinationService
{
    private KeyboardInputBehavior? _keyboardBehavior;
    private GamepadInputBehavior? _gamepadBehavior;
    private ScrollInputBehavior? _scrollBehavior;

    public bool IsInputBlocked { get; set; }

    public event EventHandler<Direction>? DirectionInputReceived;

    public void RegisterBehaviors(ContentPage page)
    {
        // Create and attach keyboard behavior
        _keyboardBehavior = new KeyboardInputBehavior();
        _keyboardBehavior.DirectionPressed += OnBehaviorDirectionPressed;
        page.Behaviors.Add(_keyboardBehavior);

        // Create and attach gamepad behavior
        _gamepadBehavior = new GamepadInputBehavior();
        _gamepadBehavior.DirectionPressed += OnBehaviorDirectionPressed;
        page.Behaviors.Add(_gamepadBehavior);

        // Create and attach scroll behavior (desktop only)
        _scrollBehavior = new ScrollInputBehavior();
        _scrollBehavior.DirectionPressed += OnBehaviorDirectionPressed;
        page.Behaviors.Add(_scrollBehavior);
    }

    public void UnregisterBehaviors(ContentPage page)
    {
        if (_keyboardBehavior is not null)
        {
            _keyboardBehavior.DirectionPressed -= OnBehaviorDirectionPressed;
            page.Behaviors.Remove(_keyboardBehavior);
            _keyboardBehavior = null;
        }

        if (_gamepadBehavior is not null)
        {
            _gamepadBehavior.DirectionPressed -= OnBehaviorDirectionPressed;
            page.Behaviors.Remove(_gamepadBehavior);
            _gamepadBehavior = null;
        }

        if (_scrollBehavior is not null)
        {
            _scrollBehavior.DirectionPressed -= OnBehaviorDirectionPressed;
            page.Behaviors.Remove(_scrollBehavior);
            _scrollBehavior = null;
        }
    }

    private void OnBehaviorDirectionPressed(object? sender, Direction direction)
    {
        if (IsInputBlocked)
            return;

        DirectionInputReceived?.Invoke(this, direction);
    }
}
