namespace TwentyFortyEight.ViewModels;

public static class SwipePreviewAnimationTiming
{
    public static uint GetCompletionDuration(uint baseSlideDuration, double progress)
    {
        if (baseSlideDuration == 0)
            return 0;

        var clampedProgress = Math.Clamp(progress, 0, 1);
        var remaining = Math.Clamp(1 - clampedProgress, 0, 1);

        if (remaining <= 0)
            return 0;

        var requestedMinDuration = remaining >= 0.25 ? 180u : 120u;
        var minDuration = Math.Min(requestedMinDuration, baseSlideDuration);

        var rawDuration = baseSlideDuration * Math.Sqrt(remaining);
        return (uint)Math.Round(Math.Clamp(rawDuration, minDuration, baseSlideDuration));
    }
}
