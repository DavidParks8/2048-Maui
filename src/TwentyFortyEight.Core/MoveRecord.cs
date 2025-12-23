namespace TwentyFortyEight.Core;

/// <summary>
/// Records data about a move for undo/replay functionality.
/// </summary>
public class MoveRecord(Direction direction)
{
    public Direction Direction { get; } = direction;
    public int SpawnedTileIndex { get; set; }
    public int SpawnedTileValue { get; set; }
}
