namespace TwentyFortyEight.ViewModels.Messages;

/// <summary>
/// Sent when a board size change is requested (e.g., from settings).
/// The receiver is responsible for applying the change.
/// </summary>
public sealed record BoardSizeChangeRequestedMessage(int NewSize);

/// <summary>
/// Sent after the board size has been applied.
/// Consumers (e.g., UI) can rebuild layouts.
/// </summary>
public sealed record BoardSizeChangedMessage(int OldSize, int NewSize);
