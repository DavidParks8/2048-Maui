using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for handling swipe gesture recognition.
/// Manages pointer and pan gesture tracking for cross-platform compatibility.
/// </summary>
public class GestureRecognizerService : IGestureRecognizerService
{
    private const double MinSwipeDistance = 30;

    // Track gesture recognizers per view
    private readonly Dictionary<
        View,
        (PanGestureRecognizer Pan, PointerGestureRecognizer Pointer)
    > _recognizers = [];

    // Touch/pointer tracking for swipe detection
    private Point? _pointerStartPoint;
    private Point _panAccumulator;

    public event EventHandler<Direction>? SwipeDetected;

    public void AttachSwipeRecognizers(View view)
    {
        if (_recognizers.ContainsKey(view))
            return; // Already attached

        // Pan gesture for touch swipes (works on mobile)
        PanGestureRecognizer panGesture = new();
        panGesture.PanUpdated += OnPanUpdated;

        // Pointer gesture for better mouse/touch support (especially on Windows)
        PointerGestureRecognizer pointerGesture = new();
        pointerGesture.PointerPressed += OnPointerPressed;
        pointerGesture.PointerReleased += OnPointerReleased;

        view.GestureRecognizers.Add(panGesture);
        view.GestureRecognizers.Add(pointerGesture);

        _recognizers[view] = (panGesture, pointerGesture);
    }

    public void DetachSwipeRecognizers(View view)
    {
        if (!_recognizers.TryGetValue(view, out var recognizers))
            return;

        recognizers.Pan.PanUpdated -= OnPanUpdated;
        recognizers.Pointer.PointerPressed -= OnPointerPressed;
        recognizers.Pointer.PointerReleased -= OnPointerReleased;

        view.GestureRecognizers.Remove(recognizers.Pan);
        view.GestureRecognizers.Remove(recognizers.Pointer);

        _recognizers.Remove(view);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is not View view)
            return;

        _pointerStartPoint = e.GetPosition(view);
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (_pointerStartPoint is null || sender is not View view)
        {
            _pointerStartPoint = null;
            return;
        }

        var endPoint = e.GetPosition(view);
        if (endPoint is null)
        {
            _pointerStartPoint = null;
            return;
        }

        var deltaX = endPoint.Value.X - _pointerStartPoint.Value.X;
        var deltaY = endPoint.Value.Y - _pointerStartPoint.Value.Y;

        ProcessSwipe(deltaX, deltaY);

        _pointerStartPoint = null;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panAccumulator = new Point(0, 0);
                break;

            case GestureStatus.Running:
                // Track the cumulative pan distance
                _panAccumulator = new Point(e.TotalX, e.TotalY);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                ProcessSwipe(_panAccumulator.X, _panAccumulator.Y);
                break;
        }
    }

    private void ProcessSwipe(double deltaX, double deltaY)
    {
        Direction? direction = null;

        if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            if (Math.Abs(deltaX) > MinSwipeDistance)
            {
                direction = deltaX > 0 ? Direction.Right : Direction.Left;
            }
        }
        else
        {
            if (Math.Abs(deltaY) > MinSwipeDistance)
            {
                direction = deltaY > 0 ? Direction.Down : Direction.Up;
            }
        }

        if (direction.HasValue)
        {
            SwipeDetected?.Invoke(this, direction.Value);
        }
    }
}
