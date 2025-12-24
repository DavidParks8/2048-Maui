using Microsoft.Extensions.Logging;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI implementation of screen reader service using SemanticScreenReader.
/// </summary>
public class MauiScreenReaderService : IScreenReaderService
{
    private readonly ILogger<MauiScreenReaderService> _logger;

    public MauiScreenReaderService(ILogger<MauiScreenReaderService> logger)
    {
        _logger = logger;
    }

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
                _logger.LogWarning(
                    ex,
                    "Failed to announce message to screen reader: {Message}",
                    message
                );
            }
        });
    }
}
