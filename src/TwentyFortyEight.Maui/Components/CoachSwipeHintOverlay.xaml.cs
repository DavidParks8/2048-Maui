using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Converters;
using TwentyFortyEight.Maui.Resources.Strings;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Components;

public partial class CoachSwipeHintOverlay : ContentView
{
    public static readonly BindableProperty DirectionProperty = BindableProperty.Create(
        nameof(Direction),
        typeof(Direction?),
        typeof(CoachSwipeHintOverlay),
        null,
        propertyChanged: static (bindable, _, _) =>
            ((CoachSwipeHintOverlay)bindable).OnHintChanged()
    );

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive),
        typeof(bool),
        typeof(CoachSwipeHintOverlay),
        false,
        propertyChanged: static (bindable, _, _) =>
            ((CoachSwipeHintOverlay)bindable).OnHintChanged()
    );

    public static readonly BindableProperty ReasonProperty = BindableProperty.Create(
        nameof(Reason),
        typeof(MoveCoachReason?),
        typeof(CoachSwipeHintOverlay),
        null,
        propertyChanged: static (bindable, _, _) =>
            ((CoachSwipeHintOverlay)bindable).OnHintChanged()
    );

    public static readonly BindableProperty MoveCounterProperty = BindableProperty.Create(
        nameof(MoveCounter),
        typeof(int),
        typeof(CoachSwipeHintOverlay),
        0,
        propertyChanged: static (bindable, oldValue, newValue) =>
            ((CoachSwipeHintOverlay)bindable).OnMoveCounterChanged((int)oldValue, (int)newValue)
    );

    public Direction? Direction
    {
        get => (Direction?)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public MoveCoachReason? Reason
    {
        get => (MoveCoachReason?)GetValue(ReasonProperty);
        set => SetValue(ReasonProperty, value);
    }

    public int MoveCounter
    {
        get => (int)GetValue(MoveCounterProperty);
        set => SetValue(MoveCounterProperty, value);
    }

    private readonly IScreenReaderService _screenReaderService;
    private CancellationTokenSource? _animationCts;
    private CancellationTokenSource? _announceCts;
    private (Direction direction, MoveCoachReason reason)? _lastAnnouncedSuggestion;
    private bool _wasReasonVisible;

    public CoachSwipeHintOverlay()
    {
        InitializeComponent();

        Loaded += (_, _) => OnHintChanged();
        Unloaded += (_, _) => StopAnimation();
        _screenReaderService = ((App)Application.Current!).Services.GetRequiredService<IScreenReaderService>();
    }

    private void OnHintChanged()
    {
        UpdateArrowGlyph();
        UpdateReasonVisibility();
        ScheduleAnnouncementIfNeeded();

        if (!IsLoaded)
        {
            return;
        }

        if (IsActive && Direction is not null)
        {
            StartAnimation();
        }
        else
        {
            StopAnimation();
        }
    }

    private void UpdateReasonVisibility()
    {
        if (ReasonContainer is null)
        {
            return;
        }

        var isVisibleNow = IsActive && Reason is not null;
        ReasonContainer.IsVisible = isVisibleNow;

        // Only force opacity when the label is newly shown.
        // Otherwise, don't fight the move-triggered fade animation.
        if (isVisibleNow && !_wasReasonVisible)
        {
            ReasonContainer.Opacity = 1;
        }

        _wasReasonVisible = isVisibleNow;
    }

    private void ScheduleAnnouncementIfNeeded()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (!IsActive || Direction is null || Reason is null)
        {
            _lastAnnouncedSuggestion = null;
            CancelAnnouncement();
            return;
        }

        CancelAnnouncement();

        _announceCts = new CancellationTokenSource();
        var token = _announceCts.Token;

        // Debounce slightly so if Direction/Reason update separately we announce once.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(60, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await Dispatcher.DispatchAsync(() =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (!IsActive || Direction is null || Reason is null)
                {
                    return;
                }

                var current = (Direction.Value, Reason.Value);
                if (_lastAnnouncedSuggestion == current)
                {
                    return;
                }

                _lastAnnouncedSuggestion = current;
                AnnounceToScreenReader(current.Item1, current.Item2);
            });
        });
    }

    private void CancelAnnouncement()
    {
        try
        {
            _announceCts?.Cancel();
        }
        catch
        {
            // ignore
        }

        _announceCts?.Dispose();
        _announceCts = null;
    }

    private void AnnounceToScreenReader(Direction direction, MoveCoachReason reason)
    {
        var directionText = direction switch
        {
            Core.Direction.Up => AppStrings.DirectionUp,
            Core.Direction.Down => AppStrings.DirectionDown,
            Core.Direction.Left => AppStrings.DirectionLeft,
            Core.Direction.Right => AppStrings.DirectionRight,
            _ => string.Empty,
        };

        var reasonText = MoveCoachReasonConverter.GetLocalizedReason(reason);
        var announcement =
            string.Format(AppStrings.CoachSuggestionFormat, directionText) + ". " + reasonText;

        _screenReaderService.Announce(announcement);
    }

    private static void OnMoveCounterChanged(int oldValue, int newValue)
    {
        // Reason chip stays visible without fading.
        // This callback is kept in case we want to trigger screen reader announcements in the future.
    }

    private void UpdateArrowGlyph()
    {
        Arrow.Text = Direction switch
        {
            Core.Direction.Up => "↑",
            Core.Direction.Down => "↓",
            Core.Direction.Left => "←",
            Core.Direction.Right => "→",
            _ => string.Empty,
        };
    }

    private void StartAnimation()
    {
        StopAnimation();

        _animationCts = new CancellationTokenSource();
        var token = _animationCts.Token;

        _ = RunAnimationLoopAsync(token);
    }

    private void StopAnimation()
    {
        try
        {
            _animationCts?.Cancel();
        }
        catch
        {
            // ignore
        }

        _animationCts?.Dispose();
        _animationCts = null;

        CancelAnnouncement();

        Arrow.Opacity = 0;
        Arrow.TranslationX = 0;
        Arrow.TranslationY = 0;
    }

    private async Task RunAnimationLoopAsync(CancellationToken cancellationToken)
    {
        // Keep it subtle: fade in, slide, fade out, repeat.
        // Using built-in MAUI animations respects OS accessibility settings.
        const uint fadeInMs = 120;
        const uint slideMs = 420;
        const uint fadeOutMs = 180;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!IsActive || Direction is null)
            {
                Arrow.Opacity = 0;
                return;
            }

            var (dx, dy) = GetOffset(Direction.Value);

            Arrow.TranslationX = 0;
            Arrow.TranslationY = 0;
            Arrow.Opacity = 0;

            // Ensure UI thread
            await Dispatcher.DispatchAsync(async () =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var fadeInTasks = new List<Task>(2)
                {
                    Arrow.FadeToAsync(1, fadeInMs, Easing.CubicOut),
                };

                await Task.WhenAll(fadeInTasks);

                await Task.WhenAll(
                    Arrow.TranslateToAsync(dx, dy, slideMs, Easing.CubicInOut),
                    Arrow.FadeToAsync(0.05, slideMs, Easing.CubicIn)
                );

                var fadeOutTasks = new List<Task>(2)
                {
                    Arrow.FadeToAsync(0, fadeOutMs, Easing.CubicOut),
                };

                await Task.WhenAll(fadeOutTasks);
            });

            // Small gap between loops
            await Task.Delay(250, cancellationToken);
        }
    }

    private static (double dx, double dy) GetOffset(Direction direction)
    {
        const double distance = 64;

        return direction switch
        {
            Core.Direction.Up => (0, -distance),
            Core.Direction.Down => (0, distance),
            Core.Direction.Left => (-distance, 0),
            Core.Direction.Right => (distance, 0),
            _ => (0, 0),
        };
    }
}
