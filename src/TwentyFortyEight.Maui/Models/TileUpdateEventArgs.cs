using System.Collections.Generic;

namespace TwentyFortyEight.Maui.Models;

/// <summary>
/// Event args for tile update events that require animations.
/// </summary>
public class TileUpdateEventArgs : EventArgs
{
    /// <summary>
    /// Tiles that moved to a new position.
    /// </summary>
    public List<TileViewModel> MovedTiles { get; set; } = new();

    /// <summary>
    /// Tiles that are newly spawned.
    /// </summary>
    public List<TileViewModel> NewTiles { get; set; } = new();

    /// <summary>
    /// Tiles that resulted from a merge.
    /// </summary>
    public List<TileViewModel> MergedTiles { get; set; } = new();
}
