using System.Collections.Frozen;
using System.Collections.Generic;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Models;

/// <summary>
/// Event args for tile update events that require animations.
/// </summary>
public class TileUpdateEventArgs : EventArgs
{
    /// <summary>
    /// Tiles that moved to a new position.
    /// </summary>
    public required FrozenSet<TileViewModel> MovedTiles { get; init; }

    /// <summary>
    /// Tiles that are newly spawned.
    /// </summary>
    public required FrozenSet<TileViewModel> NewTiles { get; init; }

    /// <summary>
    /// Tiles that resulted from a merge.
    /// </summary>
    public required FrozenSet<TileViewModel> MergedTiles { get; init; }

    /// <summary>
    /// Direction of the move that triggered these updates.
    /// </summary>
    public required Direction MoveDirection { get; init; }

    /// <summary>
    /// List of all tile movements with source and destination positions.
    /// This enables proper slide animations from origin to destination.
    /// </summary>
    public required IReadOnlyList<TileMovement> TileMovements { get; init; }
}
