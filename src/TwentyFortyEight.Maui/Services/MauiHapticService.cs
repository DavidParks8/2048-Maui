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
        if (IsSupported)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
    }
}
