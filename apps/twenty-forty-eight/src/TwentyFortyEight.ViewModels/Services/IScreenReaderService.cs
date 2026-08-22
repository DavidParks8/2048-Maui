namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Service for announcing messages to screen readers for accessibility.
/// </summary>
public interface IScreenReaderService
{
    /// <summary>
    /// Announces a message to screen readers.
    /// </summary>
    /// <param name="message">The message to announce.</param>
    void Announce(string message);
}
