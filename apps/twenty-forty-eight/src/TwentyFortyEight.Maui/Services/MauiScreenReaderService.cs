using Microsoft.Extensions.Logging;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI implementation of screen reader service using SemanticScreenReader.
/// </summary>
public partial class MauiScreenReaderService(ILogger<MauiScreenReaderService> logger)
    : IScreenReaderService
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
            try
            {
                SemanticScreenReader.Announce(message);
            }
            catch (Exception ex)
            {
                // Log but don't crash if announcement fails
                LogAnnounceFailed(logger, ex, message);
            }
        });
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to announce message to screen reader: {Message}"
    )]
    private static partial void LogAnnounceFailed(ILogger logger, Exception ex, string message);
}
