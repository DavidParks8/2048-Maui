using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Lightweight swipe attempt detector for adversarial mode.
/// Uses a minimal pan gesture recognizer to detect swipe-like movements
/// without the full preview/animation machinery of GestureRecognizerService.
/// </summary>
public class SwipeAttemptDetector : ISwipeAttemptDetector
{
    private readonly Dictionary<View, PanGestureRecognizer> _recognizers = [];
    private double _panTotalX;
    private double _panTotalY;

    public event EventHandler? SwipeAttempted;

    public void Attach(View view)
    {
        if (_recognizers.ContainsKey(view))
            return; // Already attached

        // Use a simple pan gesture that accumulates movement
        PanGestureRecognizer panGesture = new();
        panGesture.PanUpdated += OnPanUpdated;

        view.GestureRecognizers.Add(panGesture);
        _recognizers[view] = panGesture;
    }

    public void Detach(View view)
    {
        if (!_recognizers.TryGetValue(view, out var recognizer))
            return;

        recognizer.PanUpdated -= OnPanUpdated;
        view.GestureRecognizers.Remove(recognizer);
        _recognizers.Remove(view);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panTotalX = 0;
                _panTotalY = 0;
                break;

            case GestureStatus.Running:
                _panTotalX = e.TotalX;
                _panTotalY = e.TotalY;
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                // Check if this was a swipe-like gesture (exceeded minimum threshold)
                // Use the same threshold as the main swipe detector for consistency
                var direction = SwipeDirectionDetector.GetSwipeDirection(_panTotalX, _panTotalY);
                if (direction.HasValue)
                {
                    SwipeAttempted?.Invoke(this, EventArgs.Empty);
                }

                _panTotalX = 0;
                _panTotalY = 0;
                break;
        }
    }
}
