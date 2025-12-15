namespace TwentyFortyEight.Core;

/// <summary>
/// Represents a move command that can be applied and reverted.
/// </summary>
public class MoveCommand
{
    public Direction Direction { get; }
    public int SpawnedTileIndex { get; set; }
    public int SpawnedTileValue { get; set; }

    public MoveCommand(Direction direction)
    {
        Direction = direction;
    }
}
