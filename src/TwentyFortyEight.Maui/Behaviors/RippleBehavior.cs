using SkiaSharp.Views.Maui.Controls;
using TwentyFortyEight.Maui.Services;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// A behavior that renders a water-ripple effect when the user double-taps the attached element.
/// Attach to a container (e.g., a Border) to enable the effect. The behavior automatically
/// creates and manages the overlay SKCanvasView.
/// </summary>
public sealed class RippleBehavior : Behavior<View>
{
    private View? _attachedElement;
    private SKCanvasView? _rippleOverlay;
    private PointerGestureRecognizer? _pointerGesture;
    private CancellationTokenSource? _rippleCts;
    private BoardRippleService? _rippleService;
    private Task? _activeRippleTask;
    private readonly Lock _rippleLock = new();
    private readonly SemaphoreSlim _rippleGate = new(1, 1);

    // Double-tap detection
    private DateTimeOffset _lastPointerPressTimeUtc;
    private Point? _lastPointerPressPoint;
    private const int DoubleTapMaxIntervalMs = 320;
    private const double DoubleTapMaxDistance = 30;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);

        _attachedElement = bindable;

        // Resolve the ripple service from DI via the handler's MauiContext
        bindable.HandlerChanged += OnHandlerChanged;

        // Wire up double-tap detection
        _pointerGesture = new PointerGestureRecognizer();
        _pointerGesture.PointerPressed += OnPointerPressed;
        bindable.GestureRecognizers.Add(_pointerGesture);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        // Best-effort cancel any in-progress ripple (without blocking UI thread)
        CancellationTokenSource? cts;
        lock (_rippleLock)
        {
            cts = _rippleCts;
            _rippleCts = null;
            _activeRippleTask = null;
        }
        cts?.Cancel();
        cts?.Dispose();

        // Unwire gesture
        if (_pointerGesture is not null)
        {
            _pointerGesture.PointerPressed -= OnPointerPressed;
            bindable.GestureRecognizers.Remove(_pointerGesture);
            _pointerGesture = null;
        }

        // Remove the overlay we injected
        if (_rippleOverlay is not null && _rippleOverlay.Parent is Layout parentLayout)
        {
            parentLayout.Children.Remove(_rippleOverlay);
        }
        _rippleOverlay = null;

        bindable.HandlerChanged -= OnHandlerChanged;
        _attachedElement = null;

        base.OnDetachingFrom(bindable);
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (_attachedElement?.Handler?.MauiContext is null)
            return;

        // Resolve service from DI
        _rippleService =
            _attachedElement.Handler.MauiContext.Services.GetService<BoardRippleService>();

        // Inject overlay as sibling. The attached element should be inside a Grid/Layout.
        EnsureOverlayInjected();
    }

    private void EnsureOverlayInjected()
    {
        if (_rippleOverlay is not null)
            return;

        if (_attachedElement?.Parent is not Layout parentLayout)
            return;

        _rippleOverlay = new SKCanvasView
        {
            IsVisible = false,
            InputTransparent = true,
            ZIndex = 50,
        };

        // If attached to a Border with a Grid content, inject into that Grid so it overlays the content.
        if (_attachedElement is Border border && border.Content is Grid innerGrid)
        {
            // Find the GameBoard grid inside and match its size
            var gameBoard = innerGrid.Children.OfType<Grid>().FirstOrDefault();
            if (gameBoard is not null)
            {
                _rippleOverlay.WidthRequest = gameBoard.WidthRequest;
                _rippleOverlay.HeightRequest = gameBoard.HeightRequest;
            }
            else
            {
                // Fallback: fill the grid
                _rippleOverlay.HorizontalOptions = LayoutOptions.Fill;
                _rippleOverlay.VerticalOptions = LayoutOptions.Fill;
            }
            innerGrid.Children.Add(_rippleOverlay);
        }
        else
        {
            // Fall back to sibling injection
            parentLayout.Children.Add(_rippleOverlay);
        }
    }

    private void OnPointerPressed(object? sender, PointerEventArgs e)
    {
        if (_attachedElement is null)
            return;

        var positionInElement = e.GetPosition(_attachedElement);
        if (positionInElement is null)
            return;

        var nowUtc = DateTimeOffset.UtcNow;
        var elapsedMs = (nowUtc - _lastPointerPressTimeUtc).TotalMilliseconds;
        var lastPoint = _lastPointerPressPoint;

        if (lastPoint is not null && elapsedMs <= DoubleTapMaxIntervalMs)
        {
            var dx = positionInElement.Value.X - lastPoint.Value.X;
            var dy = positionInElement.Value.Y - lastPoint.Value.Y;
            var distance = Math.Sqrt((dx * dx) + (dy * dy));

            if (distance <= DoubleTapMaxDistance)
            {
                _lastPointerPressPoint = null;
                _ = StartRippleAsync(positionInElement.Value);
                return;
            }
        }

        _lastPointerPressTimeUtc = nowUtc;
        _lastPointerPressPoint = positionInElement.Value;
    }

    private async Task StartRippleAsync(Point originInElement)
    {
        if (_rippleService is null || _rippleOverlay is null || _attachedElement is null)
            return;

        // Non-blocking check: if a ripple is already playing, ignore this request entirely
        if (!_rippleGate.Wait(0))
            return;

        CancellationTokenSource? oldCts = null;
        Task? oldTask = null;
        try
        {
            // Cancel any in-progress ripple and allow it to wind down before starting a new one.
            lock (_rippleLock)
            {
                oldCts = _rippleCts;
                oldTask = _activeRippleTask;
                _rippleCts = null;
                _activeRippleTask = null;
            }

            if (oldCts is not null)
            {
                oldCts.Cancel();
                if (oldTask is not null)
                {
                    try
                    {
                        // Wait briefly for cleanup to finish to avoid overlapping handlers.
                        await oldTask
                            .WaitAsync(TimeSpan.FromMilliseconds(750))
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore cancellation/timeout/errors; we still proceed to start a fresh ripple.
                    }
                }
                oldCts.Dispose();
            }

            var newCts = new CancellationTokenSource();
            var task = _rippleService.TryPlayAsync(
                rippleOverlay: _rippleOverlay,
                boardContainer: _attachedElement,
                originInBoard: originInElement,
                cancellationToken: newCts.Token
            );

            lock (_rippleLock)
            {
                _rippleCts = newCts;
                _activeRippleTask = task;
            }

            try
            {
                await task.ConfigureAwait(false);
            }
            finally
            {
                lock (_rippleLock)
                {
                    if (_rippleCts == newCts)
                    {
                        _rippleCts = null;
                        _activeRippleTask = null;
                    }
                }
                newCts.Dispose();
            }
        }
        finally
        {
            _rippleGate.Release();
        }
    }
}
