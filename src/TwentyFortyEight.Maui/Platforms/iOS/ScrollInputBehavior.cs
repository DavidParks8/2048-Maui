namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// iOS stub implementation - scroll input not supported on mobile platforms.
/// </summary>
public partial class ScrollInputBehavior
{
    partial void AttachPlatformHandler(ContentPage page)
    {
        // No-op: Scroll gestures are not applicable on iOS
    }

    partial void DetachPlatformHandler(ContentPage page)
    {
        // No-op
    }
}
