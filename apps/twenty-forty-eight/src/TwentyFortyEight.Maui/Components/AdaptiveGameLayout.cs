namespace TwentyFortyEight.Maui.Components;

public class AdaptiveGameLayout : AbsoluteLayout
{
    public event EventHandler? NativeLayoutUpdated;

    internal void NotifyNativeLayoutUpdated() => NativeLayoutUpdated?.Invoke(this, EventArgs.Empty);
}
