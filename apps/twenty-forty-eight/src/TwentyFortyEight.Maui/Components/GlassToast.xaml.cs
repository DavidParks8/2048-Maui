namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A toast notification with liquid glass styling.
/// Animates in from below, displays the message, then fades out.
/// </summary>
public partial class GlassToast : Border
{
    private const uint FadeInDurationMs = 200;
    private const uint FadeOutDurationMs = 150;
    private const double SlideDistance = 20;

    public GlassToast()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the toast message.
    /// </summary>
    public string Message
    {
        get => MessageLabel.Text;
        set => MessageLabel.Text = value;
    }

    /// <summary>
    /// Animates the toast into view.
    /// </summary>
    public Task ShowAsync()
    {
        // Start below and invisible
        TranslationY = SlideDistance;
        Opacity = 0;

        // Animate in: slide up and fade in simultaneously
        return Task.WhenAll(
            this.TranslateToAsync(0, 0, FadeInDurationMs, Easing.CubicOut),
            this.FadeToAsync(1, FadeInDurationMs, Easing.CubicOut)
        );
    }

    /// <summary>
    /// Animates the toast out of view.
    /// </summary>
    public Task HideAsync()
    {
        // Fade out only (no slide)
        return this.FadeToAsync(0, FadeOutDurationMs, Easing.CubicIn);
    }
}
