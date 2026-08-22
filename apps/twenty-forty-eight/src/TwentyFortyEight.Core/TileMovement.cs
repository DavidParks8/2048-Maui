namespace TwentyFortyEight.Core;

/// <summary>
/// Represents the movement of a tile from one position to another.
/// </summary>
public readonly record struct TileMovement(Position From, Position To, int Value, bool IsMerging);
