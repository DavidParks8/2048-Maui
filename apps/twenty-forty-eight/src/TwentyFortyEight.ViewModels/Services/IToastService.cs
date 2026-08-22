namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Service for displaying toast notifications.
/// Platform implementations can provide native or custom styled toasts.
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Shows a toast notification with the specified message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="durationMs">Duration in milliseconds (default: 2000).</param>
    Task ShowAsync(string message, int durationMs = 2000);
}
