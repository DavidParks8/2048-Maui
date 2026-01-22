using Godot;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Simple cross-platform haptics helper mirroring the MAUI implementation.
/// </summary>
public static class HapticsService
{
    public static void PlayMove()
    {
        var settings = GameSettings.Instance;
        if (settings?.HapticsEnabled != true)
        {
            return;
        }

        var platform = OS.GetName();
        if (platform is "iOS" or "Android")
        {
            Input.VibrateHandheld(30);
        }
    }
}
