using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of IHapticService using HapticFeedback.
/// </summary>
public class MauiHapticService : IHapticService
{
    /// <inheritdoc />
    public bool IsSupported => HapticFeedback.Default.IsSupported;

    /// <inheritdoc />
    public void PerformHaptic()
    {
        PerformHaptic(HapticPattern.Move);
    }

    /// <inheritdoc />
    public void PerformHaptic(HapticPattern pattern)
    {
        if (!IsSupported)
        {
            return;
        }

        // Map semantic patterns to platform built-ins.
        HapticFeedbackType type = pattern switch
        {
            HapticPattern.Victory => HapticFeedbackType.LongPress,
            _ => HapticFeedbackType.Click,
        };

        HapticFeedback.Default.Perform(type);
    }
}
