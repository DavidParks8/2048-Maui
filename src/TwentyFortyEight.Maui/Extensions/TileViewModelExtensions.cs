using TwentyFortyEight.Maui.Helpers;
using TwentyFortyEight.ViewModels.Models;

namespace TwentyFortyEight.Maui.Extensions;

/// <summary>
/// Extension methods for TileViewModel to provide MAUI-specific Color values.
/// </summary>
public static class TileViewModelExtensions
{
    /// <summary>
    /// Gets the background color for this tile.
    /// </summary>
    public static Color GetBackgroundColor(this TileViewModel tile) =>
        TileColorHelper.GetTileBackgroundColor(tile.Value);

    /// <summary>
    /// Gets the text color for this tile.
    /// </summary>
    public static Color GetTextColor(this TileViewModel tile) =>
        TileColorHelper.GetTileTextColor(tile.Value);

    /// <summary>
    /// Gets the font size for this tile.
    /// </summary>
    public static double GetFontSize(this TileViewModel tile) =>
        TileColorHelper.GetTileFontSize(tile.Value);
}
