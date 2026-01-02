using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SkiaSharp.Skottie;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace TwentyFortyEight.Maui.Components;

public sealed partial class LoopingLottieOverlay : SKCanvasView
{
    public static readonly BindableProperty AssetNameProperty = BindableProperty.Create(
        nameof(AssetName),
        typeof(string),
        typeof(LoopingLottieOverlay),
        string.Empty,
        propertyChanged: OnAssetNameChanged
    );

    public string AssetName
    {
        get => (string)GetValue(AssetNameProperty);
        set => SetValue(AssetNameProperty, value);
    }

    private static void OnAssetNameChanged(
        BindableObject bindable,
        object oldValue,
        object newValue
    )
    {
        if (
            bindable is LoopingLottieOverlay overlay
            && newValue is string assetName
            && !string.IsNullOrEmpty(assetName)
        )
        {
            overlay._animation = null; // Reset so it reloads
            _ = overlay.EnsureAnimationLoadedAsync();
        }
    }

    private SkiaSharp.Skottie.Animation? _animation;
    private bool _isLoading;
    private bool _isLoaded;
    private IDispatcherTimer? _timer;
    private readonly Stopwatch _clock = new();
    private readonly ILogger? _logger;

    public LoopingLottieOverlay()
    {
        InputTransparent = true;

        _logger = ResolveLogger();

        PaintSurface += OnPaintSurface;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static ILogger? ResolveLogger()
    {
        if (Application.Current is global::TwentyFortyEight.Maui.App app)
        {
            return app.Services.GetService<ILogger<LoopingLottieOverlay>>();
        }

        return null;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Failed to read Lottie animation data '{AssetName}'"
    )]
    private static partial void LogFailedToReadLottieData(ILogger logger, string assetName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to parse Lottie animation '{AssetName}': unsupported features or invalid format"
    )]
    private static partial void LogFailedToParseLottieAnimation(ILogger logger, string assetName);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Failed to load Lottie animation '{AssetName}'"
    )]
    private static partial void LogFailedToLoadLottieAnimation(
        ILogger logger,
        string assetName,
        Exception exception
    );

    public LoopingLottieOverlay(string assetName)
        : this()
    {
        AssetName = assetName;
    }

    public void Start()
    {
        IsVisible = true;
        EnsureTimerRunning();
    }

    public void Stop()
    {
        IsVisible = false;
        StopTimer();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _isLoaded = true;
        _ = EnsureAnimationLoadedAsync();
        if (IsVisible)
        {
            EnsureTimerRunning();
        }
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _isLoaded = false;
        StopTimer();
    }

    private async Task EnsureAnimationLoadedAsync()
    {
        if (_animation is not null || _isLoading || string.IsNullOrEmpty(AssetName))
            return;

        _isLoading = true;
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(AssetName);
            var data = SKData.Create(stream);
            if (data is null)
            {
                if (_logger is ILogger logger)
                {
                    LogFailedToReadLottieData(logger, AssetName);
                }
                return;
            }

            if (
                !SkiaSharp.Skottie.Animation.TryCreate(data, out var animation) || animation is null
            )
            {
                if (_logger is ILogger logger)
                {
                    LogFailedToParseLottieAnimation(logger, AssetName);
                }
                return;
            }

            _animation = animation;
            _clock.Reset();
            _clock.Start();
        }
        catch (Exception ex)
        {
            if (_logger is ILogger logger)
            {
                LogFailedToLoadLottieAnimation(logger, AssetName, ex);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void EnsureTimerRunning()
    {
        if (!_isLoaded)
            return;

        _timer ??= Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;

        if (!_timer.IsRunning)
        {
            _timer.Start();
        }
    }

    private void StopTimer()
    {
        if (_timer is not null && _timer.IsRunning)
        {
            _timer.Stop();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!IsVisible)
        {
            StopTimer();
            return;
        }

        InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_animation is null)
            return;

        double durationSeconds = _animation.Duration.TotalSeconds;
        if (durationSeconds <= 0)
            return;

        double t = _clock.Elapsed.TotalSeconds % durationSeconds;
        _animation.SeekFrameTime(t);

        SKRect viewRect = new(0, 0, e.Info.Width, e.Info.Height);

        var animSize = _animation.Size;
        if (animSize.Width <= 0 || animSize.Height <= 0)
            return;

        float scale = Math.Min(viewRect.Width / animSize.Width, viewRect.Height / animSize.Height);
        float width = animSize.Width * scale;
        float height = animSize.Height * scale;

        var dest = SKRect.Create(
            x: (viewRect.MidX - (width / 2f)),
            y: (viewRect.MidY - (height / 2f)),
            width: width,
            height: height
        );

        _animation.Render(canvas, dest);
    }
}
