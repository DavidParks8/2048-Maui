namespace TwentyFortyEight.Maui.Models;

/// <summary>
/// Represents the movement of a tile from one position to another.
/// </summary>
public readonly record struct TileMovement(
    int FromRow,
    int FromColumn,
    int ToRow,
    int ToColumn,
    int Value,
    bool IsMerging);
