namespace TwentyFortyEight.Core;

/// <summary>
/// JSON-friendly representation of an in-progress game session.
/// Includes full undo/redo history for the duration of the game.
/// </summary>
public sealed class GameSave
{
    /// <summary>
    /// Snapshot of the session start state (after initial spawns).
    /// </summary>
    public GameStateDto? InitialState { get; set; }

    /// <summary>
    /// Recorded move history (including any redo moves beyond <see cref="CurrentMoveIndex"/>).
    /// </summary>
    public MoveRecord[] MoveHistory { get; set; } = [];

    /// <summary>
    /// The index of the next move to apply from <see cref="MoveHistory"/>.
    /// This matches the engine's internal cursor for undo/redo.
    /// </summary>
    public int CurrentMoveIndex { get; set; }

    /// <summary>
    /// Latch for victory event emission; persists across app restarts.
    /// </summary>
    public bool VictoryEventRaised { get; set; }

    /// <summary>
    /// Total number of undos performed in the current game session.
    /// </summary>
    public int UndoCount { get; set; }
}
