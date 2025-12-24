namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Abstraction for localized string resources.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the localized restart confirmation title.
    /// </summary>
    string RestartConfirmTitle { get; }

    /// <summary>
    /// Gets the localized restart confirmation message.
    /// </summary>
    string RestartConfirmMessage { get; }

    /// <summary>
    /// Gets the localized "Start New" button text.
    /// </summary>
    string StartNew { get; }

    /// <summary>
    /// Gets the localized "Cancel" button text.
    /// </summary>
    string Cancel { get; }

    /// <summary>
    /// Gets the localized "You Win!" status text.
    /// </summary>
    string YouWin { get; }

    /// <summary>
    /// Gets the localized reset statistics title.
    /// </summary>
    string ResetStatisticsTitle { get; }

    /// <summary>
    /// Gets the localized reset statistics message.
    /// </summary>
    string ResetStatisticsMessage { get; }

    /// <summary>
    /// Gets the localized "Reset" button text.
    /// </summary>
    string Reset { get; }
}
