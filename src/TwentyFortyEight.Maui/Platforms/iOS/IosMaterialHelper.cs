#if IOS || MACCATALYST
using UIKit;
using TwentyFortyEight.Maui.Components;

namespace TwentyFortyEight.Maui.Platforms.iOS;

/// <summary>
/// Helper class for converting IosMaterialStyle to UIBlurEffectStyle.
/// </summary>
public static class IosMaterialHelper
{
    /// <summary>
    /// Converts an IosMaterialStyle to the corresponding UIBlurEffectStyle.
    /// </summary>
    /// <param name="style">The IosMaterialStyle to convert.</param>
    /// <returns>The corresponding UIBlurEffectStyle.</returns>
    public static UIBlurEffectStyle ToBlurStyle(IosMaterialStyle style)
    {
        // iOS 13+ materials
        if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
        {
            return style switch
            {
                IosMaterialStyle.SystemUltraThinMaterial =>
                    UIBlurEffectStyle.SystemUltraThinMaterial,
                IosMaterialStyle.SystemThinMaterial => UIBlurEffectStyle.SystemThinMaterial,
                IosMaterialStyle.SystemMaterial => UIBlurEffectStyle.SystemMaterial,
                IosMaterialStyle.SystemThickMaterial => UIBlurEffectStyle.SystemThickMaterial,
                IosMaterialStyle.SystemChromeMaterial => UIBlurEffectStyle.SystemChromeMaterial,
                _ => UIBlurEffectStyle.SystemThickMaterial,
            };
        }

        return UIBlurEffectStyle.Light;
    }
}
#endif
