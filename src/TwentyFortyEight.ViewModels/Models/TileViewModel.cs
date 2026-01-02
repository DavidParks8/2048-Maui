using CommunityToolkit.Mvvm.ComponentModel;
using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.ViewModels.Models;

/// <summary>
/// Represents a tile in the 2048 game grid.
/// This version includes platform-agnostic properties and MAUI-specific Color properties when targeting MAUI.
/// </summary>
public partial class TileViewModel : ObservableObject
{
    [ObservableProperty]
    private int _value;

    [ObservableProperty]
    private int _row;

    [ObservableProperty]
    private int _column;

    [ObservableProperty]
    private bool _isNewTile;

    [ObservableProperty]
    private bool _isMerged;

    public string DisplayValue => Value == 0 ? "" : Value.ToString();

    #region MAUI Color Properties

    /// <summary>
    /// Gets the background color for this tile (MAUI-specific).
    /// </summary>
    public Color BackgroundColor => TileColorHelper.GetTileBackgroundColor(Value);

    /// <summary>
    /// Gets the text color for this tile (MAUI-specific).
    /// </summary>
    public Color TextColor => TileColorHelper.GetTileTextColor(Value);

    /// <summary>
    /// Gets the font size for this tile (MAUI-specific).
    /// </summary>
    public double FontSize => GetTileFontSize(Value);

    /// <summary>
    /// Gets the appropriate font size for a tile based on the number of digits.
    /// </summary>
    public static double GetTileFontSize(int value)
    {
        if (value == 0)
            return 32;

        var digitCount = (int)Math.Floor(Math.Log10(value)) + 1;

        return digitCount switch
        {
            1 => 32,
            2 => 32,
            3 => 28,
            4 => 24,
            5 => 20,
            6 => 16,
            7 => 14,
            _ => 12,
        };
    }

    #endregion

    /// <summary>
    /// Partial method hook called when Value property changes.
    /// Notifies dependent properties to update.
    /// </summary>
    partial void OnValueChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(FontSize));
    }

    /// <summary>
    /// Forces a refresh of the color properties.
    /// Useful when the app theme changes.
    /// </summary>
    public void RefreshColors()
    {
#if ANDROID || IOS || MACCATALYST || WINDOWS
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
#endif
    }
}
