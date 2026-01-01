using TwentyFortyEight.ViewModels.Services;
using Windows.UI.ViewManagement;

namespace TwentyFortyEight.Maui.Services;

public class ReduceMotionService : IReduceMotionService
{
    public bool ShouldReduceMotion()
    {
        try
        {
            return !new UISettings().AnimationsEnabled;
        }
        catch
        {
            return false;
        }
    }
}
