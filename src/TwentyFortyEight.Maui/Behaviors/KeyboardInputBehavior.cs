using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Behaviors;

/// <summary>
/// Cross-platform behavior that enables keyboard input for game controls.
/// Uses platform-specific implementations for optimal keyboard handling on each platform.
/// </summary>
public partial class KeyboardInputBehavior : Behavior<ContentPage>
{
    /// <summary>
    /// Event raised when a direction key is pressed.
    /// </summary>
    public event EventHandler<Direction>? DirectionPressed;

    /// <summary>
    /// The page this behavior is attached to.
    /// </summary>
    protected ContentPage? AttachedPage { get; private set; }

    protected override void OnAttachedTo(ContentPage bindable)
    {
        base.OnAttachedTo(bindable);
        AttachedPage = bindable;
        AttachPlatformHandler(bindable);
    }

    protected override void OnDetachingFrom(ContentPage bindable)
    {
        DetachPlatformHandler(bindable);
        AttachedPage = null;
        base.OnDetachingFrom(bindable);
    }

    /// <summary>
    /// Raises the DirectionPressed event.
    /// </summary>
    protected void OnDirectionPressed(Direction direction)
    {
        DirectionPressed?.Invoke(this, direction);
    }

    /// <summary>
    /// Platform-specific implementation to attach keyboard handlers.
    /// </summary>
    partial void AttachPlatformHandler(ContentPage page);

    /// <summary>
    /// Platform-specific implementation to detach keyboard handlers.
    /// </summary>
    partial void DetachPlatformHandler(ContentPage page);
}
