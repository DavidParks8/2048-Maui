namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Abstraction for displaying alerts and confirmation dialogs.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Displays a confirmation dialog and returns the user's choice.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <param name="accept">The accept button text.</param>
    /// <param name="cancel">The cancel button text.</param>
    /// <returns>True if user accepted, false if cancelled.</returns>
    Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);

    /// <summary>
    /// Displays an informational alert.
    /// </summary>
    /// <param name="title">The alert title.</param>
    /// <param name="message">The alert message.</param>
    /// <param name="cancel">The dismiss button text.</param>
    Task ShowAlertAsync(string title, string message, string cancel);
}
