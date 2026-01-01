using UIKit;

namespace TwentyFortyEight.Maui.Services;

public class ReduceMotionService : IReduceMotionService
{
    public bool ShouldReduceMotion() => UIAccessibility.IsReduceMotionEnabled;
}
