namespace TwentyFortyEight.Core;

/// <summary>
/// Records data about a move for undo/replay functionality.
/// </summary>
/// <param name="Direction">The direction of the move.</param>
/// <param name="SpawnedTileIndex">The flat index where a new tile was spawned, or -1 if none.</param>
/// <param name="SpawnedTileValue">The value of the spawned tile (2 or 4).</param>
public record MoveRecord(Direction Direction, int SpawnedTileIndex = -1, int SpawnedTileValue = 0);
