#if ANDROID
using Android.Content.Res;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AndroidSwitch = AndroidX.AppCompat.Widget.SwitchCompat;

namespace TwentyFortyEight.Maui.Platforms.Android.Handlers;

/// <summary>
/// Custom Switch handler for Android to properly apply OnColor and ThumbColor.
/// Android switches require both track and thumb tint to be set correctly.
/// </summary>
public class CustomSwitchHandler : SwitchHandler
{
    public static new PropertyMapper<Switch, CustomSwitchHandler> Mapper = new(SwitchHandler.Mapper)
    {
        [nameof(Switch.OnColor)] = MapOnColor,
        [nameof(Switch.ThumbColor)] = MapThumbColor,
    };

    public CustomSwitchHandler()
        : base(Mapper) { }

    protected override void ConnectHandler(AndroidSwitch platformView)
    {
        base.ConnectHandler(platformView);
        UpdateColors();
    }

    private static void MapOnColor(CustomSwitchHandler handler, Switch switchControl)
    {
        handler.UpdateColors();
    }

    private static void MapThumbColor(CustomSwitchHandler handler, Switch switchControl)
    {
        handler.UpdateColors();
    }

    private void UpdateColors()
    {
        if (
            PlatformView is not AndroidSwitch androidSwitch
            || VirtualView is not Switch switchControl
        )
            return;

        var onColor = switchControl.OnColor;
        var thumbColor = switchControl.ThumbColor;

        // Set track tint (the background rail of the switch)
        if (onColor != null)
        {
            var trackColor = onColor.ToInt();
            var trackOffColor = GetOffTrackColor();

            var trackStates = new[]
            {
                new[] { global::Android.Resource.Attribute.StateChecked },
                new[] { -global::Android.Resource.Attribute.StateChecked },
            };

            var trackColors = new[] { trackColor, trackOffColor };

            androidSwitch.TrackTintList = new ColorStateList(trackStates, trackColors);
        }

        // Set thumb tint (the circular button that slides)
        if (thumbColor != null)
        {
            var thumbOnColor = thumbColor.ToInt();
            var thumbOffColor = GetOffThumbColor();

            var thumbStates = new[]
            {
                new[] { global::Android.Resource.Attribute.StateChecked },
                new[] { -global::Android.Resource.Attribute.StateChecked },
            };

            var thumbColors = new[] { thumbOnColor, thumbOffColor };

            androidSwitch.ThumbTintList = new ColorStateList(thumbStates, thumbColors);
        }
    }

    private static int GetOffTrackColor()
    {
        // Use a neutral gray for the off state track
        // This matches the default Material Design behavior
        return global::Android.Graphics.Color.Argb(77, 0, 0, 0); // ~30% opacity black
    }

    private static int GetOffThumbColor()
    {
        // Use a light gray for the off state thumb
        // This matches the default Material Design behavior
        return global::Android.Graphics.Color.Argb(255, 245, 245, 245); // Very light gray
    }
}
#endif
