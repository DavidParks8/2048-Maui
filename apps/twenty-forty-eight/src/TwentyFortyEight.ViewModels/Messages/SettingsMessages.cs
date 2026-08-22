namespace TwentyFortyEight.ViewModels.Messages;

/// <summary>
/// Sent when a settings toggle is changed that the game should react to immediately.
/// </summary>
public sealed record CoachEnabledChangedMessage(bool IsEnabled);

/// <summary>
/// Sent when the Coach nudge setting is changed.
/// </summary>
public sealed record CoachNudgesEnabledChangedMessage(bool IsEnabled);

/// <summary>
/// Sent when the Undo button visibility setting is changed.
/// </summary>
public sealed record UndoButtonVisibilityChangedMessage(bool IsVisible);
