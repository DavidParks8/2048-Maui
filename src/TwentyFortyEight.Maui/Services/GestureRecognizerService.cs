using System.Diagnostics;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Service for handling swipe gesture recognition.
/// Manages pointer and pan gesture tracking for cross-platform compatibility.
/// </summary>
public class GestureRecognizerService : IGestureRecognizerService
{
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

    // Track the number of active pointers to handle multitouch scenarios.
    // We only process gestures from the first pointer and ignore additional touches.
    private int _activePointerCount;

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

        // Increment the active pointer count to track multitouch scenarios
        _activePointerCount++;

        // Only process the first pointer. Ignore additional touches while a gesture is active.
        if (_activePointerCount > 1)
        {
            return;
        }

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
        // Only process movement for the initial pointer
        if (_activeInput != ActiveInput.Pointer || _activePointerCount != 1)
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
        // Decrement the active pointer count
        if (_activePointerCount > 0)
        {
            _activePointerCount--;
        }

        // Only complete the gesture when the last (first) pointer is released
        if (_activeInput != ActiveInput.Pointer || _activePointerCount > 0)
        {
            return;
        }

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
            // Reset pointer count when switching views since we're abandoning the gesture
            // on the old view. New touches on the new view will start fresh.
            _activePointerCount = 0;
            return;
        }

        var endPoint = e.GetPosition(view);

        // Use last known position if pointer ended outside view bounds to handle fast swipes
        if (endPoint is null)
        {
            if (_pointerLastKnownPoint is null)
            {
                _pointerStartPoint = null;
                _pointerLastKnownPoint = null;
                _pointerStopwatch = null;
                _activeInput = ActiveInput.None;
                _activeView = null;
                // Reset pointer count in error state - we have no position data so the
                // gesture is invalid. New touches will start fresh.
                _activePointerCount = 0;
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
        // Pointer count is already 0 here (enforced by check at line 155),
        // but reset explicitly to ensure clean state for next gesture.
        _activePointerCount = 0;
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
                // Reset pointer count when Pan completes. Pan and Pointer are mutually
                // exclusive based on the _activeInput state machine (each handler ignores
                // events while the other input type is active), so pointer count should be
                // 0 during Pan. Reset ensures a clean state if we switch to Pointer gestures later.
                _activePointerCount = 0;
                break;
        }
    }

    private static SwipePanEventArgs BuildPanArgs(
        GestureStatus status,
        double totalX,
        double totalY,
        TimeSpan elapsed
    )
    {
        return new SwipePanEventArgs
        {
            Status = status,
            TotalX = totalX,
            TotalY = totalY,
            PreviewDirection = SwipeDirectionDetector.GetPreviewDirection(totalX, totalY),
            SwipeDirection = SwipeDirectionDetector.GetSwipeDirection(totalX, totalY),
            Elapsed = elapsed,
            IsFast = SwipeDirectionDetector.IsFastSwipe(totalX, totalY, elapsed.TotalMilliseconds),
        };
    }

    private void ProcessSwipe(double deltaX, double deltaY)
    {
        Direction? direction = SwipeDirectionDetector.GetSwipeDirection(deltaX, deltaY);

        if (direction.HasValue)
        {
            SwipeDetected?.Invoke(this, direction.Value);
        }
    }
}
