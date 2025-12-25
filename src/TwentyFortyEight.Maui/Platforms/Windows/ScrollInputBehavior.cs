using System.Diagnostics;
using System.Threading;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// Windows-specific scroll input handling using native WinUI pointer wheel events.
/// Supports mouse wheel and trackpad two-finger scroll gestures.
/// Uses leading-edge debounce: fires on first event, ignores subsequent events during cooldown.
/// </summary>
public partial class ScrollInputBehavior
{
    private Microsoft.UI.Xaml.UIElement? _nativeElement;
    private const int ScrollCooldownMs = 700; // Cooldown after firing to ignore subsequent events
    private static readonly long ScrollCooldownTimestampTicks =
        (Stopwatch.Frequency * ScrollCooldownMs) / 1000;
    private long _lastScrollFireTimestamp;

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
            AttachedPage?.GetParentWindow()?.Handler?.PlatformView
                is Microsoft.UI.Xaml.Window nativeWindow
            && nativeWindow.Content is Microsoft.UI.Xaml.UIElement content
        )
        {
            _nativeElement = content;
            content.PointerWheelChanged += OnPointerWheelChanged;
        }
    }

    private void CleanupNativeHandler()
    {
        if (_nativeElement is not null)
        {
            _nativeElement.PointerWheelChanged -= OnPointerWheelChanged;
            _nativeElement = null;
        }
    }

    private void OnPointerWheelChanged(
        object sender,
        Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
    )
    {
        // Always mark as handled to prevent other gesture handlers from processing
        e.Handled = true;

        long nowTimestamp = Stopwatch.GetTimestamp();

        // Leading-edge debounce: ignore events during cooldown period
        long lastTimestamp = Interlocked.Read(ref _lastScrollFireTimestamp);
        if (lastTimestamp != 0 && nowTimestamp - lastTimestamp < ScrollCooldownTimestampTicks)
            return;

        var properties = e.GetCurrentPoint(sender as Microsoft.UI.Xaml.UIElement).Properties;
        int delta = properties.MouseWheelDelta;

        if (delta == 0)
            return;

        // Require minimum threshold
        const int MinDeltaThreshold = 30;

        if (Math.Abs(delta) < MinDeltaThreshold)
            return;

        Direction? direction;

        if (properties.IsHorizontalMouseWheel)
        {
            // Horizontal scroll - invert to match finger direction
            direction = delta > 0 ? Direction.Left : Direction.Right;
        }
        else
        {
            // Vertical scroll - invert to match finger direction
            direction = delta > 0 ? Direction.Down : Direction.Up;
        }

        if (direction.HasValue)
        {
            while (true)
            {
                long last = Interlocked.Read(ref _lastScrollFireTimestamp);

                if (last != 0 && nowTimestamp - last < ScrollCooldownTimestampTicks)
                    return;

                if (
                    Interlocked.CompareExchange(ref _lastScrollFireTimestamp, nowTimestamp, last)
                    == last
                )
                    break;
            }

            OnDirectionPressed(direction.Value);
        }
    }
}
