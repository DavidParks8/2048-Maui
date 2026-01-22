using Godot;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Detects swipe gestures on touch devices.
/// </summary>
public partial class SwipeDetector : Control
{
    private const float MinSwipeDistance = 50f;
    private const float MaxSwipeTime = 0.5f;

    private Vector2 _touchStartPosition;
    private double _touchStartTime;
    private bool _isTracking;

    public event Action<Direction>? SwipeDetected;

    public override void _Ready()
    {
        // Make sure we receive input
        MouseFilter = MouseFilterEnum.Pass;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touchEvent)
        {
            HandleTouch(touchEvent);
        }
        else if (@event is InputEventScreenDrag dragEvent)
        {
            HandleDrag(dragEvent);
        }
        else if (@event is InputEventMouseButton mouseButton)
        {
            HandleMouseButton(mouseButton);
        }
        else if (@event is InputEventMouseMotion mouseMotion && _isTracking)
        {
            HandleMouseMotion(mouseMotion);
        }
    }

    private void HandleTouch(InputEventScreenTouch touchEvent)
    {
        if (touchEvent.Pressed)
        {
            StartTracking(touchEvent.Position);
        }
        else if (_isTracking)
        {
            EndTracking(touchEvent.Position);
        }
    }

    private void HandleDrag(InputEventScreenDrag dragEvent)
    {
        if (_isTracking)
        {
            CheckSwipe(dragEvent.Position);
        }
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                StartTracking(mouseButton.Position);
            }
            else if (_isTracking)
            {
                EndTracking(mouseButton.Position);
            }
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        CheckSwipe(mouseMotion.Position);
    }

    private void StartTracking(Vector2 position)
    {
        _touchStartPosition = position;
        _touchStartTime = Time.GetTicksMsec() / 1000.0;
        _isTracking = true;
    }

    private void EndTracking(Vector2 endPosition)
    {
        if (!_isTracking)
            return;

        _isTracking = false;
        CheckSwipe(endPosition, isEndOfGesture: true);
    }

    private void CheckSwipe(Vector2 currentPosition, bool isEndOfGesture = false)
    {
        if (!_isTracking && !isEndOfGesture)
            return;

        double elapsedTime = Time.GetTicksMsec() / 1000.0 - _touchStartTime;

        // Check if too much time has passed
        if (elapsedTime > MaxSwipeTime && !isEndOfGesture)
        {
            _isTracking = false;
            return;
        }

        Vector2 delta = currentPosition - _touchStartPosition;
        float distance = delta.Length();

        if (distance < MinSwipeDistance)
            return;

        // Determine direction
        Direction? direction = null;

        if (Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
        {
            // Horizontal swipe
            direction = delta.X > 0 ? Direction.Right : Direction.Left;
        }
        else
        {
            // Vertical swipe
            direction = delta.Y > 0 ? Direction.Down : Direction.Up;
        }

        if (direction.HasValue)
        {
            _isTracking = false;
            SwipeDetected?.Invoke(direction.Value);
        }
    }
}
