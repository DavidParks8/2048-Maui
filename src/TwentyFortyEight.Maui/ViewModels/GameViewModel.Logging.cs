using Microsoft.Extensions.Logging;

namespace TwentyFortyEight.Maui.ViewModels;

/// <summary>
/// Logger message definitions for GameViewModel.
/// </summary>
public partial class GameViewModel
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Failed to save game state")]
    partial void LogSaveGameError(Exception ex);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to load game state")]
    partial void LogLoadGameError(Exception ex);
}
