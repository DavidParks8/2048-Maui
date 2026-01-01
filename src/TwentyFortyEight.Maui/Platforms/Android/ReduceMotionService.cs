using Android.Provider;

namespace TwentyFortyEight.Maui.Services;

public class ReduceMotionService : IReduceMotionService
{
    public bool ShouldReduceMotion()
    {
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        if (context?.ContentResolver == null)
            return false;

        try
        {
            float scale = Settings.Global.GetFloat(
                context.ContentResolver,
                Settings.Global.AnimatorDurationScale,
                1f
            );
            return scale == 0f;
        }
        catch
        {
            return false;
        }
    }
}
