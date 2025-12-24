using System.Numerics;
using System.Runtime.InteropServices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

// NOTE: We intentionally avoid SKRuntimeEffect here to keep this working across
// SkiaSharp versions and platforms. The effect is implemented via Skia's
// displacement-map image filter plus a smooth animated vector field.

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Renders a photorealistic water ripple effect over the game board when the user double-taps.
/// The effect captures a snapshot, then distorts it with concentric traveling waves that
/// reflect off the edges twice before dissipating — like dropping a stone in a still pool.
/// </summary>
public sealed partial class BoardRippleService
{
    /// <summary>
    /// Total duration of the ripple animation in seconds.
    /// Tuned to allow two full edge reflections before fade-out.
    /// </summary>
    private const double TotalDurationSeconds = 3.2;

    // Reusable buffer to avoid per-frame allocation (static for use in static method)
    [ThreadStatic]
    private static byte[]? s_pixelBuffer;

    [ThreadStatic]
    private static int s_pixelBufferSize;

    // Debounce: track if a ripple is currently playing to prevent concurrent effects
    private int _isPlaying;

    public async Task<bool> TryPlayAsync(
        SKCanvasView rippleOverlay,
        VisualElement boardContainer,
        Point originInBoard,
        CancellationToken cancellationToken
    )
    {
        // Debounce: if already playing, skip this request
        if (Interlocked.CompareExchange(ref _isPlaying, 1, 0) != 0)
            return false;

        IDispatcherTimer? timer = null;
        SKBitmap? capturedBitmap = null;
        SKImage? capturedImage = null;
        EventHandler<SKPaintSurfaceEventArgs>? paintHandler = null;
        var handlerAttached = false;
        SKBitmap? displacementMap = null;

        // Flag to signal paint handler that resources are being disposed.
        // Using a class wrapper so the closure captures the reference, not the value.
        var disposedFlag = new DisposedFlag();
        // Counter to track in-flight paint operations
        var paintInFlight = new PaintInFlightCounter();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var boardWidth = boardContainer.Width;
            var boardHeight = boardContainer.Height;

            if (boardWidth <= 1 || boardHeight <= 1)
                return false;

            capturedBitmap = await TryCaptureBitmapAsync(boardContainer, cancellationToken);
            if (capturedBitmap is null)
                return false;

            capturedImage = SKImage.FromBitmap(capturedBitmap);
            if (capturedImage is null)
                return false;

            // Store captured image dimensions for proper scaling
            var capturedWidth = capturedBitmap.Width;
            var capturedHeight = capturedBitmap.Height;

            await rippleOverlay.Dispatcher.DispatchAsync(() => rippleOverlay.IsVisible = true);

            var startUtc = DateTimeOffset.UtcNow;
            var tcs = new TaskCompletionSource();

            // Capture DIPs once; convert to pixels each frame based on actual canvas size.
            var boardDipWidth = (float)boardWidth;
            var boardDipHeight = (float)boardHeight;
            var originDip = new Vector2((float)originInBoard.X, (float)originInBoard.Y);
            var durationSeconds = (float)TotalDurationSeconds;

            paintHandler = (_, e) =>
            {
                // Check disposed flag FIRST before any resource access.
                // This prevents using disposed native handles.
                if (disposedFlag.IsDisposed)
                    return;

                // Increment in-flight counter to signal we're actively painting.
                // The finally block will wait for this to reach zero before disposing.
                paintInFlight.Enter();
                try
                {
                    // Double-check after entering - disposal may have started
                    if (disposedFlag.IsDisposed)
                        return;

                    var info = e.Info;
                    var canvas = e.Surface.Canvas;
                    canvas.Clear(SKColors.Transparent);

                    // Check again before using capturedImage
                    if (capturedImage is null || disposedFlag.IsDisposed)
                        return;

                    // Source rect is the captured image size; dest rect is the canvas size
                    // Apply a manual scale factor to compensate for sizing mismatch
                    const float scaleFactor = 1.05f;
                    var srcRect = SKRect.Create(capturedWidth, capturedHeight);
                    var scaledW = info.Width * scaleFactor;
                    var scaledH = info.Height * scaleFactor;
                    var offsetX = (info.Width - scaledW) / 2f;
                    var offsetY = (info.Height - scaledH) / 2f;
                    var dstRect = SKRect.Create(offsetX, offsetY, scaledW, scaledH);

                    var elapsed = (float)(DateTimeOffset.UtcNow - startUtc).TotalSeconds;
                    var env = Envelope(elapsed, durationSeconds);
                    if (env <= 0.001f)
                    {
                        // Ensure we don't block board visibility if we're still painting.
                        if (!disposedFlag.IsDisposed)
                            canvas.DrawImage(capturedImage, srcRect, dstRect);
                        return;
                    }

                    // Displacement map resolution - balance between quality and performance.
                    // Lower resolution = faster but more aliasing. GPU upscales it smoothly.
                    var mapW = Math.Clamp(info.Width / 2, 240, 480);
                    var mapH = Math.Clamp(info.Height / 2, 240, 480);
                    if (
                        displacementMap is null
                        || displacementMap.Width != mapW
                        || displacementMap.Height != mapH
                    )
                    {
                        displacementMap?.Dispose();
                        displacementMap = new SKBitmap(
                            new SKImageInfo(mapW, mapH, SKColorType.Bgra8888, SKAlphaType.Premul)
                        );
                    }

                    // Convert origin in DIPs to pixels in the current canvas.
                    var dipToPxX = boardDipWidth > 0 ? (info.Width / boardDipWidth) : 1f;
                    var dipToPxY = boardDipHeight > 0 ? (info.Height / boardDipHeight) : 1f;
                    var originPx = new Vector2(originDip.X * dipToPxX, originDip.Y * dipToPxY);

                    // Update displacement map pixels.
                    UpdateDisplacementMap(
                        displacementMap,
                        originPx,
                        new Vector2(info.Width, info.Height),
                        elapsed,
                        durationSeconds
                    );

                    // SkiaSharp 2.88 docs: CreateDisplacementMapEffect expects SKImageFilter displacement,
                    // not a shader. We generate an image filter from the displacement bitmap.
                    using var displacementImage = SKImage.FromBitmap(displacementMap);
                    if (displacementImage is null || disposedFlag.IsDisposed)
                    {
                        if (!disposedFlag.IsDisposed)
                            canvas.DrawImage(capturedImage, srcRect, dstRect);
                        return;
                    }

                    using var displacementFilter = SKImageFilter.CreateImage(
                        displacementImage,
                        SKRect.Create(mapW, mapH),
                        dstRect,
                        SKFilterQuality.High
                    );

                    // AAA-level displacement amplitude (pixels) - scales with envelope
                    var scalePx = 28f * env;

                    using var rippleFilter = SKImageFilter.CreateDisplacementMapEffect(
                        SKColorChannel.R,
                        SKColorChannel.G,
                        scalePx,
                        displacementFilter,
                        null
                    );

                    using var paint = new SKPaint
                    {
                        IsAntialias = true,
                        ImageFilter = rippleFilter,
                    };
                    if (!disposedFlag.IsDisposed)
                        canvas.DrawImage(capturedImage, srcRect, dstRect, paint);
                }
                finally
                {
                    paintInFlight.Exit();
                }
            };

            rippleOverlay.PaintSurface += paintHandler;
            handlerAttached = true;

            timer = boardContainer.Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS

            timer.Tick += (_, _) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    timer.Stop();
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                rippleOverlay.InvalidateSurface();

                var elapsed = (DateTimeOffset.UtcNow - startUtc).TotalSeconds;
                if (elapsed >= TotalDurationSeconds)
                {
                    timer.Stop();
                    tcs.TrySetResult();
                }
            };

            timer.Start();
            rippleOverlay.InvalidateSurface();
            await tcs.Task.ConfigureAwait(false);
            return true;
        }
        finally
        {
            // ALWAYS hide overlay and clean up, even on cancellation or exception.
            timer?.Stop();

            // Signal that we're disposing - paint handlers will check this flag
            // and exit early before accessing any resources.
            disposedFlag.MarkDisposed();

            if (handlerAttached && paintHandler is not null)
                rippleOverlay.PaintSurface -= paintHandler;

            // Wait for any in-flight paint operations to complete.
            // This ensures no paint handler is actively using the resources we're about to dispose.
            // Use a timeout to prevent deadlocks in edge cases.
            paintInFlight.WaitForZero(TimeSpan.FromMilliseconds(100));

            rippleOverlay.Dispatcher.Dispatch(() => rippleOverlay.IsVisible = false);

            capturedImage?.Dispose();
            capturedBitmap?.Dispose();
            displacementMap?.Dispose();

            // Reset debounce flag to allow future ripple effects
            Interlocked.Exchange(ref _isPlaying, 0);
        }
    }

    /// <summary>
    /// Platform-specific implementation to capture the board container as a bitmap.
    /// </summary>
    private static partial Task<SKBitmap?> TryCaptureBitmapAsync(
        VisualElement boardContainer,
        CancellationToken cancellationToken
    );

    private static float Envelope(float t, float duration)
    {
        if (duration <= 0.001f)
            return 0f;

        var nt = Math.Clamp(t / duration, 0f, 1f);

        // AAA-quality envelope: fast attack, sustained plateau, smooth exponential decay
        var attack = SmoothStep(0f, 0.04f, nt);
        var sustain = 1f - SmoothStep(0.15f, 0.35f, nt) * 0.3f; // slight dip in middle
        var decay = 1f - SmoothStep(0.45f, 1f, nt);

        // Exponential tail for natural dissipation
        var expDecay = MathF.Exp(-3f * MathF.Max(0f, nt - 0.5f));

        // Lerp(1, expDecay, 0.4) = 1 + 0.4*(expDecay - 1) = 0.6 + 0.4*expDecay
        return attack * sustain * decay * (0.6f + (0.4f * expDecay));
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - (2f * t));
    }

    private static void UpdateDisplacementMap(
        SKBitmap map,
        Vector2 originPx,
        Vector2 canvasSizePx,
        float time,
        float duration
    )
    {
        // Encode displacement into RG as [0..255] where 128 means 0 displacement.
        // We write into a managed buffer and Marshal.Copy into the bitmap to avoid /unsafe.

        var env = Envelope(time, duration);
        var width = map.Width;
        var height = map.Height;
        if (width <= 0 || height <= 0)
            return;

        var dstPtr = map.GetPixels();
        if (dstPtr == IntPtr.Zero)
            return;

        // Map pixels to canvas pixels.
        var sx = canvasSizePx.X / Math.Max(1, width);
        var sy = canvasSizePx.Y / Math.Max(1, height);

        // Tunables (in pixels) - balanced for performance
        var maxDisp = 20f * env;
        var decayDist = 400f;
        var damp = 0.70f;

        // 3-band harmonic wave system (reduced from 5 for performance)
        const float speed1 = 180f; // Primary wave (slower, more relaxed)
        const float speed2 = 120f; // Secondary
        const float speed3 = 70f; // Tertiary
        const float wl1 = 50f; // Slightly longer wavelengths for smoother look
        const float wl2 = 90f;
        const float wl3 = 160f;

        var res = canvasSizePx;
        var rowBytes = map.RowBytes;
        var totalBytes = rowBytes * height;

        // Reuse buffer if possible
        if (s_pixelBuffer is null || s_pixelBufferSize < totalBytes)
        {
            s_pixelBuffer = new byte[totalBytes];
            s_pixelBufferSize = totalBytes;
        }
        var bytes = s_pixelBuffer;

        for (var y = 0; y < height; y++)
        {
            var py = (y + 0.5f) * sy;
            var rowBase = y * rowBytes;

            for (var x = 0; x < width; x++)
            {
                var px = (x + 0.5f) * sx;
                var p = new Vector2(px, py);

                var disp = Vector2.Zero;

                // 1 bounce via mirrored image sources (reduced from 2 for performance)
                for (var nx = -1; nx <= 1; nx++)
                {
                    for (var ny = -1; ny <= 1; ny++)
                    {
                        var reflections = Math.Abs(nx) + Math.Abs(ny);
                        var rf = MathF.Pow(damp, reflections);
                        var shift = new Vector2(2 * nx * res.X, 2 * ny * res.Y);

                        disp += Contribution(
                            p,
                            originPx + shift,
                            time,
                            rf,
                            maxDisp,
                            decayDist,
                            speed1,
                            wl1,
                            speed2,
                            wl2,
                            speed3,
                            wl3
                        );
                        disp += Contribution(
                            p,
                            new Vector2(-originPx.X, originPx.Y) + shift,
                            time,
                            rf * 0.80f,
                            maxDisp,
                            decayDist,
                            speed1,
                            wl1,
                            speed2,
                            wl2,
                            speed3,
                            wl3
                        );
                        disp += Contribution(
                            p,
                            new Vector2(originPx.X, -originPx.Y) + shift,
                            time,
                            rf * 0.80f,
                            maxDisp,
                            decayDist,
                            speed1,
                            wl1,
                            speed2,
                            wl2,
                            speed3,
                            wl3
                        );
                        disp += Contribution(
                            p,
                            new Vector2(-originPx.X, -originPx.Y) + shift,
                            time,
                            rf * 0.65f,
                            maxDisp,
                            decayDist,
                            speed1,
                            wl1,
                            speed2,
                            wl2,
                            speed3,
                            wl3
                        );
                    }
                }

                // Clamp displacement with larger range for AAA effect
                disp.X = Math.Clamp(disp.X, -20f, 20f);
                disp.Y = Math.Clamp(disp.Y, -20f, 20f);

                // Encode RG with 128-centered values (larger range)
                var r = (byte)Math.Clamp(128f + (disp.X * (127f / 20f)), 0f, 255f);
                var g = (byte)Math.Clamp(128f + (disp.Y * (127f / 20f)), 0f, 255f);

                // Subtle caustic brightening encoded in blue channel
                // Based on displacement magnitude - brighter where waves converge
                var dispMag = disp.Length();
                var caustic = Math.Clamp(128f + (dispMag * 4f * env), 0f, 255f);
                var b = (byte)caustic;

                // BGRA in memory for Bgra8888.
                var idx = rowBase + (x * 4);
                bytes[idx + 0] = b; // B (caustic intensity)
                bytes[idx + 1] = g; // G (y displacement)
                bytes[idx + 2] = r; // R (x displacement)
                bytes[idx + 3] = 255; // A
            }
        }

        Marshal.Copy(bytes, 0, dstPtr, totalBytes);
    }

    private static Vector2 Contribution(
        Vector2 p,
        Vector2 origin,
        float time,
        float rf,
        float amp,
        float decayDist,
        float speed1,
        float wl1,
        float speed2,
        float wl2,
        float speed3,
        float wl3
    )
    {
        var v = p - origin;
        var d = v.Length();
        if (d < 0.001f)
            return Vector2.Zero;

        var dir = v / d;

        // Exponential spatial falloff for natural energy dissipation
        var spatial = MathF.Exp(-d / decayDist);

        // 3-band harmonic synthesis
        var h1 = Wave(d, time, speed1, wl1) * 1.0f;
        var h2 = Wave(d, time, speed2, wl2) * 0.6f;
        var h3 = Wave(d, time, speed3, wl3) * 0.35f;

        var height = (h1 + h2 + h3) * spatial;
        return dir * (amp * rf * height);
    }

    private static float Wave(float d, float time, float speed, float wl)
    {
        var k = MathF.Tau / wl;
        var omega = k * speed;
        var phase = (k * d) - (omega * time);

        var front = speed * time;
        if (d > front + wl * 1.5f)
            return 0f;

        // Smoother leading edge with cosine-squared envelope
        var lead = 1f;
        if (d > front - wl)
        {
            var t = (d - (front - wl)) / (wl * 2.5f);
            t = Math.Clamp(t, 0f, 1f);
            lead = 0.5f * (1f + MathF.Cos(MathF.PI * t));
        }

        // Subtle amplitude modulation for more organic feel
        var modulation = 1f + 0.15f * MathF.Sin(phase * 0.3f);

        return MathF.Sin(phase) * lead * modulation;
    }

    /// <summary>
    /// Thread-safe flag to signal that resources are being disposed.
    /// Paint handlers check this before accessing any native resources.
    /// </summary>
    private sealed class DisposedFlag
    {
        private int _disposed;

        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        public void MarkDisposed() => Interlocked.Exchange(ref _disposed, 1);
    }

    /// <summary>
    /// Thread-safe counter to track in-flight paint operations.
    /// The cleanup code waits for this to reach zero before disposing resources.
    /// </summary>
    private sealed class PaintInFlightCounter
    {
        private int _count;
        private readonly object _lock = new();

        public void Enter() => Interlocked.Increment(ref _count);

        public void Exit()
        {
            var newCount = Interlocked.Decrement(ref _count);
            if (newCount == 0)
            {
                lock (_lock)
                {
                    Monitor.PulseAll(_lock);
                }
            }
        }

        public void WaitForZero(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            lock (_lock)
            {
                while (Volatile.Read(ref _count) > 0)
                {
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break;

                    Monitor.Wait(_lock, remaining);
                }
            }
        }
    }
}
