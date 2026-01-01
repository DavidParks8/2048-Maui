using System;
using System.Diagnostics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using TwentyFortyEight.Maui.Victory.Phases;

namespace TwentyFortyEight.Maui.Victory;

/// <summary>
/// SkiaSharp canvas that orchestrates the cinematic victory animation phases.
/// </summary>
public sealed class CinematicOverlayView : SKCanvasView
{
    private const float MaxDeltaSeconds = 0.05f;

    private readonly IVictoryPhaseDrawer[] _phases;
    private readonly float[] _phaseEndTimesMs;

    private readonly Stopwatch _stopwatch = new();

    private double _lastTimestampSeconds;
    private bool _hasLastTimestamp;

    private VictoryAnimationContext? _ctx;

    private int _currentPhaseIndex = -1;
    private bool _modalRequested;
    private bool _isRunning;

    private IDispatcherTimer? _renderTimer;

    public event EventHandler? ShowModalRequested;

    public event EventHandler? AnimationCompleted;

    public CinematicOverlayView(
        ImpactPhaseDrawer impactPhase,
        WarpTransitionPhaseDrawer warpTransitionPhase,
        WarpSustainPhaseDrawer warpSustainPhase
    )
    {
        // Must be invisible and non-interactive by default.
        IsVisible = false;
        ZIndex = 200;
        EnableTouchEvents = false;
        InputTransparent = true;

        // Start directly at impact (skip lift + rotate).
        _phases = [impactPhase, warpTransitionPhase, warpSustainPhase];

        // Pre-calculate cumulative phase end times (milliseconds).
        _phaseEndTimesMs = new float[_phases.Length];
        float cumulative = 0f;
        for (int i = 0; i < _phases.Length; i++)
        {
            cumulative += _phases[i].Duration;
            _phaseEndTimesMs[i] = cumulative;
        }
    }

    public void StartAnimation(
        SKImage boardSnapshot,
        SKImage tileSnapshot,
        SKPoint tileCenter,
        SKSize tileSize
    )
    {
        // If already running, clean up without raising completion.
        if (_isRunning)
        {
            StopAnimationCore(raiseCompleted: false);
        }

        _ctx = new VictoryAnimationContext
        {
            BoardSnapshot = boardSnapshot,
            TileSnapshot = tileSnapshot,
            TileCenter = tileCenter,
            TileSize = tileSize,
        };

        _currentPhaseIndex = -1;
        _modalRequested = false;

        _hasLastTimestamp = false;
        _lastTimestampSeconds = 0;

        _stopwatch.Restart();
        _isRunning = true;

        IsVisible = true;
        InputTransparent = false;

        // Create and start timer for ~60 FPS rendering
        _renderTimer = Dispatcher.CreateTimer();
        _renderTimer.Interval = TimeSpan.FromMilliseconds(16);
        _renderTimer.Tick += OnRenderTimerTick;
        _renderTimer.Start();

        // Initial paint
        InvalidateSurface();
    }

    private void OnRenderTimerTick(object? sender, EventArgs e)
    {
        if (_isRunning)
        {
            InvalidateSurface();
        }
    }

    public void StopAnimation()
    {
        if (!_isRunning)
        {
            return;
        }

        StopAnimationCore(raiseCompleted: true);
    }

    public void EnterSustainMode()
    {
        if (_ctx == null)
        {
            return;
        }

        _ctx.WarpSpeed = CinematicTimingConstants.WarpSustainSpeedMultiplier;
        _ctx.WarpOpacity = CinematicTimingConstants.WarpSustainOpacity;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        if (!_isRunning || _ctx == null || _phases.Length == 0)
        {
            return;
        }

        SKCanvas canvas = e.Surface.Canvas;
        SKImageInfo info = e.Info;

        // Keep the render loop allocation-free.
        // Stopwatch.Elapsed and TimeSpan are structs (no managed allocations).
        TimeSpan elapsed = _stopwatch.Elapsed;
        float elapsedMs = (float)elapsed.TotalMilliseconds;

        // Delta-time for time-based effects (warp movement).
        double nowSeconds = elapsed.TotalSeconds;
        float deltaSeconds;
        if (!_hasLastTimestamp)
        {
            deltaSeconds = 0f;
            _hasLastTimestamp = true;
        }
        else
        {
            double delta = nowSeconds - _lastTimestampSeconds;
            if (delta < 0)
            {
                delta = 0;
            }
            else if (delta > MaxDeltaSeconds)
            {
                delta = MaxDeltaSeconds;
            }

            deltaSeconds = (float)delta;
        }

        _lastTimestampSeconds = nowSeconds;
        _ctx.DeltaSeconds = deltaSeconds;

        canvas.Clear(SKColors.Transparent);

        int newPhaseIndex = GetPhaseIndex(elapsedMs);
        if (newPhaseIndex != _currentPhaseIndex)
        {
            _currentPhaseIndex = newPhaseIndex;

            // When transitioning into sustain phase, request the modal exactly once.
            if (!_modalRequested && _currentPhaseIndex == _phases.Length - 1)
            {
                _modalRequested = true;
                ShowModalRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        float phaseStartMs =
            _currentPhaseIndex <= 0 ? 0f : _phaseEndTimesMs[_currentPhaseIndex - 1];
        float phaseDurationMs = _phases[_currentPhaseIndex].Duration;

        float progress;
        if (
            phaseDurationMs <= 0f
            || float.IsInfinity(phaseDurationMs)
            || phaseDurationMs >= float.MaxValue / 2f
        )
        {
            progress = 1f;
        }
        else
        {
            progress = (elapsedMs - phaseStartMs) / phaseDurationMs;
            if (progress < 0f)
            {
                progress = 0f;
            }
            else if (progress > 1f)
            {
                progress = 1f;
            }
        }

        _phases[_currentPhaseIndex].Draw(canvas, info, progress, _ctx);

        // Timer-based rendering - don't call InvalidateSurface here
    }

    private int GetPhaseIndex(float elapsedMs)
    {
        // Sustain is always the last phase. Scan only the finite phases.
        int lastIndex = _phaseEndTimesMs.Length - 1;
        for (int i = 0; i < lastIndex; i++)
        {
            if (elapsedMs < _phaseEndTimesMs[i])
            {
                return i;
            }
        }

        return lastIndex;
    }

    private void StopAnimationCore(bool raiseCompleted)
    {
        _isRunning = false;
        _stopwatch.Stop();

        // Stop and dispose timer
        if (_renderTimer != null)
        {
            _renderTimer.Stop();
            _renderTimer.Tick -= OnRenderTimerTick;
            _renderTimer = null;
        }

        IsVisible = false;
        InputTransparent = true;

        if (_ctx != null)
        {
            _ctx.BoardSnapshot.Dispose();
            _ctx.TileSnapshot.Dispose();
            _ctx = null;
        }

        _currentPhaseIndex = -1;
        _modalRequested = false;
        _hasLastTimestamp = false;
        _lastTimestampSeconds = 0;

        if (raiseCompleted)
        {
            AnimationCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
