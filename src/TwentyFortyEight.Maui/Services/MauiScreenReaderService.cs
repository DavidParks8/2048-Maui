using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI implementation of screen reader service using SemanticScreenReader.
/// </summary>
public class MauiScreenReaderService : IScreenReaderService
{
    /// <summary>
    /// Announces a message to screen readers using MAUI's SemanticScreenReader.
    /// </summary>
    /// <param name="message">The message to announce.</param>
    public void Announce(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Use MainThread to ensure the announcement happens on the UI thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SemanticScreenReader.Announce(message);
        });
    }
}
