using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// MacCatalyst-specific scroll input handling using native UIKit scroll events.
/// Supports trackpad two-finger scroll gestures and mouse wheel.
/// </summary>
public partial class ScrollInputBehavior
{
    private UIKit.UIView? _nativeView;
    private DateTime _lastScrollFireTime = DateTime.MinValue;
    private const int ScrollDebounceMs = 500; // Debounce to prevent rapid repeated inputs

    partial void AttachPlatformHandler(ContentPage page)
    {
        page.Loaded += OnPageLoaded;
    }

    partial void DetachPlatformHandler(ContentPage page)
    {
        page.Loaded -= OnPageLoaded;
        CleanupNativeHandler();
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        if (
            AttachedPage?.GetParentWindow()?.Handler?.PlatformView is UIKit.UIWindow nativeWindow
            && nativeWindow.RootViewController?.View is UIKit.UIView view
        )
        {
            _nativeView = view;

            // Create a custom UIScrollView gesture recognizer for scroll events
            var scrollRecognizer = new ScrollGestureRecognizer(this);
            view.AddGestureRecognizer(scrollRecognizer);
        }
    }

    private void CleanupNativeHandler()
    {
        if (_nativeView is not null)
        {
            _nativeView = null;
        }
    }

    private void HandleScrollGesture(nfloat deltaX, nfloat deltaY)
    {
        // Debounce rapid scroll events
        var now = DateTime.UtcNow;
        if ((now - _lastScrollFireTime).TotalMilliseconds < ScrollDebounceMs)
        {
            return;
        }

        // Determine dominant scroll direction
        Direction? direction = null;

        // Require minimum threshold
        const double MinDeltaThreshold = 25;

        if (Math.Abs(deltaX) > Math.Abs(deltaY) && Math.Abs(deltaX) >= MinDeltaThreshold)
        {
            // Horizontal scroll is dominant
            // Positive deltaX = fingers move right = game direction right
            direction = deltaX > 0 ? Direction.Right : Direction.Left;
        }
        else if (Math.Abs(deltaY) >= MinDeltaThreshold)
        {
            // Vertical scroll is dominant
            // Positive deltaY = fingers move down = game direction down
            direction = deltaY > 0 ? Direction.Down : Direction.Up;
        }

        if (direction.HasValue)
        {
            _lastScrollFireTime = now;
            OnDirectionPressed(direction.Value);
        }
    }

    private class ScrollGestureRecognizer : UIKit.UIPanGestureRecognizer
    {
        private readonly ScrollInputBehavior _behavior;
        private CoreGraphics.CGPoint _startPoint;

        public ScrollGestureRecognizer(ScrollInputBehavior behavior)
        {
            _behavior = behavior;
            MaximumNumberOfTouches = 2;
            MinimumNumberOfTouches = 2;
        }

        public override void TouchesBegan(Foundation.NSSet touches, UIKit.UIEvent evt)
        {
            base.TouchesBegan(touches, evt);
            if (evt.Type == UIKit.UIEventType.Scroll || NumberOfTouches == 2)
            {
                _startPoint = LocationInView(View);
            }
        }

        public override void TouchesMoved(Foundation.NSSet touches, UIKit.UIEvent evt)
        {
            base.TouchesMoved(touches, evt);

            if (evt.Type == UIKit.UIEventType.Scroll || NumberOfTouches == 2)
            {
                var currentPoint = LocationInView(View);
                var deltaX = currentPoint.X - _startPoint.X;
                var deltaY = currentPoint.Y - _startPoint.Y;

                // Only trigger if movement is significant enough
                if (Math.Abs(deltaX) > 20 || Math.Abs(deltaY) > 20)
                {
                    _behavior.HandleScrollGesture((nfloat)deltaX, (nfloat)deltaY);
                    _startPoint = currentPoint;
                }
            }
        }
    }
}
