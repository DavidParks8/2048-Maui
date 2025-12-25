using System.Collections.Frozen;
using CommunityToolkit.Mvvm.ComponentModel;
using TwentyFortyEight.ViewModels.Helpers;
#if ANDROID || IOS || MACCATALYST || WINDOWS
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;
#endif

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

    /// <summary>
    /// Gets the tile value category for styling purposes.
    /// </summary>
    public TileValueCategory ValueCategory => GetValueCategory(Value);

    /// <summary>
    /// Gets the power of 2 for this tile value (e.g., 2=1, 4=2, 8=3, etc.).
    /// Returns 0 for empty tiles.
    /// </summary>
    public int PowerOf2 => Value == 0 ? 0 : (int)Math.Log2(Value);

    #region Constants

    /// <summary>
    /// Tile values at or below this threshold use the dark text color.
    /// </summary>
    private const int DarkTextThreshold = 4;

    #endregion

    /// <summary>
    /// Determines if this tile should use dark text (for light-colored tiles).
    /// </summary>
    public bool UsesDarkText => Value <= DarkTextThreshold;

    /// <summary>
    /// Gets the appropriate font size category for a tile based on the number of digits.
    /// </summary>
    public FontSizeCategory FontSizeCategoryValue => GetFontSizeCategory(Value);

#if ANDROID || IOS || MACCATALYST || WINDOWS
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
#endif

    /// <summary>
    /// Partial method hook called when Value property changes.
    /// Notifies dependent properties to update.
    /// </summary>
    partial void OnValueChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(ValueCategory));
        OnPropertyChanged(nameof(PowerOf2));
        OnPropertyChanged(nameof(UsesDarkText));
        OnPropertyChanged(nameof(FontSizeCategoryValue));
#if ANDROID || IOS || MACCATALYST || WINDOWS
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(FontSize));
#endif
    }

    public void UpdateValue(int newValue)
    {
        Value = newValue;
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

    private static TileValueCategory GetValueCategory(int value)
    {
        return value switch
        {
            0 => TileValueCategory.Empty,
            2 => TileValueCategory.Value2,
            4 => TileValueCategory.Value4,
            8 => TileValueCategory.Value8,
            16 => TileValueCategory.Value16,
            32 => TileValueCategory.Value32,
            64 => TileValueCategory.Value64,
            128 => TileValueCategory.Value128,
            256 => TileValueCategory.Value256,
            512 => TileValueCategory.Value512,
            1024 => TileValueCategory.Value1024,
            2048 => TileValueCategory.Value2048,
            _ => TileValueCategory.HighValue,
        };
    }

    private static FontSizeCategory GetFontSizeCategory(int value)
    {
        if (value == 0)
            return FontSizeCategory.Large;

        var digitCount = (int)Math.Floor(Math.Log10(value)) + 1;

        return digitCount switch
        {
            1 or 2 => FontSizeCategory.Large,
            3 => FontSizeCategory.Medium,
            4 => FontSizeCategory.Small,
            5 => FontSizeCategory.ExtraSmall,
            6 => FontSizeCategory.Tiny,
            _ => FontSizeCategory.Micro,
        };
    }
}

/// <summary>
/// Categorizes tile values for styling purposes.
/// </summary>
public enum TileValueCategory
{
    Empty,
    Value2,
    Value4,
    Value8,
    Value16,
    Value32,
    Value64,
    Value128,
    Value256,
    Value512,
    Value1024,
    Value2048,
    HighValue,
}

/// <summary>
/// Font size categories for tile display.
/// </summary>
public enum FontSizeCategory
{
    Large,
    Medium,
    Small,
    ExtraSmall,
    Tiny,
    Micro,
}
