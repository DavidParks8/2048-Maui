namespace TwentyFortyEight.Core;

/// <summary>
/// Snapshot of the playable surface: tiles plus optional movement constraints.
/// </summary>
public readonly record struct PlayfieldSnapshot(Board Board, WallSegment? Wall = null);
