using System.Diagnostics;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for handling swipe gesture recognition.
/// Manages pointer and pan gesture tracking for cross-platform compatibility.
/// </summary>
public class GestureRecognizerService : IGestureRecognizerService
{
    private const double MinSwipeDistance = 30;
    private const double MinPreviewDistance = 8;

    // Rough heuristic: treat as "fast" once movement exceeds this speed.
    // Units: px/ms (e.g., 0.8 => ~800 px/s).
    // Lowered from 2.0 to make normal swipes less likely to be treated as preview.
    private const double FastSwipeSpeedThreshold = 0.8;

    // Track gesture recognizers per view
    private readonly Dictionary<
        View,
        (PanGestureRecognizer Pan, PointerGestureRecognizer Pointer)
    > _recognizers = [];

    // Touch/pointer tracking for swipe detection
    private Point? _pointerStartPoint;
    private Point? _pointerLastKnownPoint;
    private Point _panAccumulator;

    private Stopwatch? _panStopwatch;
    private Stopwatch? _pointerStopwatch;

    private enum ActiveInput
    {
        None,
        Pan,
        Pointer,
    }

    private ActiveInput _activeInput = ActiveInput.None;
    private View? _activeView;

    public event EventHandler<Direction>? SwipeDetected;
    public event EventHandler<SwipePanEventArgs>? SwipePanUpdated;

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
        pointerGesture.PointerMoved += OnPointerMoved;
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
        recognizers.Pointer.PointerMoved -= OnPointerMoved;
        recognizers.Pointer.PointerReleased -= OnPointerReleased;

        view.GestureRecognizers.Remove(recognizers.Pan);
        view.GestureRecognizers.Remove(recognizers.Pointer);

        _recognizers.Remove(view);
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is not View view)
            return;

        // Avoid double-reporting when both PanGestureRecognizer and PointerGestureRecognizer
        // are firing for the same interaction (common on desktop).
        if (_activeInput == ActiveInput.Pan)
            return;

        _activeInput = ActiveInput.Pointer;
        _activeView = view;

        _pointerStartPoint = e.GetPosition(view);
        _pointerLastKnownPoint = _pointerStartPoint;
        _pointerStopwatch = Stopwatch.StartNew();

        SwipePanUpdated?.Invoke(
            this,
            BuildPanArgs(GestureStatus.Started, 0, 0, elapsed: TimeSpan.Zero)
        );
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_activeInput != ActiveInput.Pointer)
            return;

        if (_pointerStartPoint is null || sender is not View view)
            return;

        if (!ReferenceEquals(_activeView, view))
            return;

        var position = e.GetPosition(view);
        if (position is null)
            return;

        // Track the last known valid position for use if pointer ends outside view bounds
        _pointerLastKnownPoint = position;

        var deltaX = position.Value.X - _pointerStartPoint.Value.X;
        var deltaY = position.Value.Y - _pointerStartPoint.Value.Y;
        var elapsed = _pointerStopwatch?.Elapsed ?? TimeSpan.Zero;

        SwipePanUpdated?.Invoke(this, BuildPanArgs(GestureStatus.Running, deltaX, deltaY, elapsed));
    }

    private void OnPointerReleased(object? sender, PointerEventArgs e)
    {
        if (_activeInput != ActiveInput.Pointer)
            return;

        if (_pointerStartPoint is null || sender is not View view)
        {
            _pointerStartPoint = null;
            _pointerLastKnownPoint = null;
            return;
        }

        if (!ReferenceEquals(_activeView, view))
        {
            _pointerStartPoint = null;
            _pointerLastKnownPoint = null;
            _pointerStopwatch = null;
            _activeInput = ActiveInput.None;
            _activeView = null;
            return;
        }

        var endPoint = e.GetPosition(view);
        
        // If the pointer ended outside the view bounds, use the last known valid position
        // This ensures fast swipes that start inside and end outside the view are still processed
        if (endPoint is null)
        {
            if (_pointerLastKnownPoint is null)
            {
                _pointerStartPoint = null;
                _pointerLastKnownPoint = null;
                _pointerStopwatch = null;
                _activeInput = ActiveInput.None;
                _activeView = null;
                return;
            }
            endPoint = _pointerLastKnownPoint;
        }

        var deltaX = endPoint.Value.X - _pointerStartPoint.Value.X;
        var deltaY = endPoint.Value.Y - _pointerStartPoint.Value.Y;

        var elapsed = _pointerStopwatch?.Elapsed ?? TimeSpan.Zero;
        SwipePanUpdated?.Invoke(
            this,
            BuildPanArgs(GestureStatus.Completed, deltaX, deltaY, elapsed)
        );

        ProcessSwipe(deltaX, deltaY);

        _pointerStartPoint = null;
        _pointerLastKnownPoint = null;
        _pointerStopwatch = null;

        _activeInput = ActiveInput.None;
        _activeView = null;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_activeInput == ActiveInput.Pointer)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _activeInput = ActiveInput.Pan;
                _activeView = sender as View;

                _panAccumulator = new Point(0, 0);
                _panStopwatch = Stopwatch.StartNew();

                SwipePanUpdated?.Invoke(
                    this,
                    BuildPanArgs(GestureStatus.Started, 0, 0, elapsed: TimeSpan.Zero)
                );
                break;

            case GestureStatus.Running:
                // Track the cumulative pan distance
                _panAccumulator = new Point(e.TotalX, e.TotalY);

                SwipePanUpdated?.Invoke(
                    this,
                    BuildPanArgs(
                        GestureStatus.Running,
                        _panAccumulator.X,
                        _panAccumulator.Y,
                        elapsed: _panStopwatch?.Elapsed ?? TimeSpan.Zero
                    )
                );
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                SwipePanUpdated?.Invoke(
                    this,
                    BuildPanArgs(
                        e.StatusType,
                        _panAccumulator.X,
                        _panAccumulator.Y,
                        elapsed: _panStopwatch?.Elapsed ?? TimeSpan.Zero
                    )
                );
                ProcessSwipe(_panAccumulator.X, _panAccumulator.Y);
                _panStopwatch = null;

                _activeInput = ActiveInput.None;
                _activeView = null;
                break;
        }
    }

    private SwipePanEventArgs BuildPanArgs(
        GestureStatus status,
        double totalX,
        double totalY,
        TimeSpan elapsed
    )
    {
        var distance = Math.Sqrt((totalX * totalX) + (totalY * totalY));
        var elapsedMs = Math.Max(1, elapsed.TotalMilliseconds);
        var speed = distance / elapsedMs;

        var previewDirection = GetDirection(totalX, totalY, MinPreviewDistance);
        var swipeDirection = GetDirection(totalX, totalY, MinSwipeDistance);

        return new SwipePanEventArgs
        {
            Status = status,
            TotalX = totalX,
            TotalY = totalY,
            PreviewDirection = previewDirection,
            SwipeDirection = swipeDirection,
            Elapsed = elapsed,
            IsFast = speed >= FastSwipeSpeedThreshold,
        };
    }

    private static Direction? GetDirection(double deltaX, double deltaY, double threshold)
    {
        Direction? direction = null;

        if (Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            if (Math.Abs(deltaX) > threshold)
            {
                direction = deltaX > 0 ? Direction.Right : Direction.Left;
            }
        }
        else
        {
            if (Math.Abs(deltaY) > threshold)
            {
                direction = deltaY > 0 ? Direction.Down : Direction.Up;
            }
        }

        return direction;
    }

    private void ProcessSwipe(double deltaX, double deltaY)
    {
        Direction? direction = GetDirection(deltaX, deltaY, MinSwipeDistance);

        if (direction.HasValue)
        {
            SwipeDetected?.Invoke(this, direction.Value);
        }
    }
}
