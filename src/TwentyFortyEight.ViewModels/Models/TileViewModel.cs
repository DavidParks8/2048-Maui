using System.Collections.Frozen;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public Color BackgroundColor => GetTileBackgroundColor(Value);

    /// <summary>
    /// Gets the text color for this tile (MAUI-specific).
    /// </summary>
    public Color TextColor => GetTileTextColor(Value);

    /// <summary>
    /// Gets the font size for this tile (MAUI-specific).
    /// </summary>
    public double FontSize => GetTileFontSize(Value);

    #region Theme Caching

    private static bool s_isDarkModeCached;
    private static bool s_themeCacheInitialized;

    static TileViewModel()
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged += OnAppThemeChanged;
            UpdateThemeCache();
        }
    }

    private static void OnAppThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        UpdateThemeCache();
    }

    private static void UpdateThemeCache()
    {
        s_isDarkModeCached = Application.Current?.RequestedTheme == AppTheme.Dark;
        s_themeCacheInitialized = true;
    }

    private static bool IsDarkMode
    {
        get
        {
            if (!s_themeCacheInitialized)
            {
                UpdateThemeCache();
            }
            return s_isDarkModeCached;
        }
    }

    #endregion

    #region Cached Colors

    private static readonly Color TextColorDark = Color.FromArgb("#776e65");
    private static readonly Color TextColorLight = Color.FromArgb("#f9f6f2");

    private static readonly FrozenDictionary<int, Color> LightModeColors = new Dictionary<
        int,
        Color
    >
    {
        [0] = Color.FromArgb("#cdc1b4"),
        [2] = Color.FromArgb("#eee4da"),
        [4] = Color.FromArgb("#ede0c8"),
        [8] = Color.FromArgb("#f2b179"),
        [16] = Color.FromArgb("#f59563"),
        [32] = Color.FromArgb("#f67c5f"),
        [64] = Color.FromArgb("#f65e3b"),
        [128] = Color.FromArgb("#edcf72"),
        [256] = Color.FromArgb("#edcc61"),
        [512] = Color.FromArgb("#edc850"),
        [1024] = Color.FromArgb("#edc53f"),
        [2048] = Color.FromArgb("#edc22e"),
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<int, Color> DarkModeColors = new Dictionary<int, Color>
    {
        [0] = Color.FromArgb("#524b44"),
        [2] = Color.FromArgb("#5c6b7a"),
        [4] = Color.FromArgb("#7a6b5c"),
        [8] = Color.FromArgb("#f2b179"),
        [16] = Color.FromArgb("#f59563"),
        [32] = Color.FromArgb("#f67c5f"),
        [64] = Color.FromArgb("#f65e3b"),
        [128] = Color.FromArgb("#edcf72"),
        [256] = Color.FromArgb("#edcc61"),
        [512] = Color.FromArgb("#edc850"),
        [1024] = Color.FromArgb("#edc53f"),
        [2048] = Color.FromArgb("#edc22e"),
    }.ToFrozenDictionary();

    #endregion

    /// <summary>
    /// Gets the text color for a tile with the specified value.
    /// </summary>
    public static Color GetTileTextColor(int value)
    {
        if (value > DarkTextThreshold)
            return Colors.White;

        return IsDarkMode ? TextColorLight : TextColorDark;
    }

    /// <summary>
    /// Gets the background color for a tile with the specified value.
    /// </summary>
    public static Color GetTileBackgroundColor(int value)
    {
        var colorMap = IsDarkMode ? DarkModeColors : LightModeColors;

        if (colorMap.TryGetValue(value, out var color))
            return color;

        return GetHighValueColor(value);
    }

    private static Color GetHighValueColor(int value)
    {
        var power = (int)Math.Log2(value);

        return power switch
        {
            12 => Color.FromRgb(0xed, 0xb4, 0x22),
            13 => Color.FromRgb(0xe8, 0x7e, 0x2c),
            14 => Color.FromRgb(0xe0, 0x4a, 0x38),
            15 => Color.FromRgb(0xd4, 0x2e, 0x55),
            16 => Color.FromRgb(0xb8, 0x2e, 0x8c),
            17 => Color.FromRgb(0x8e, 0x2e, 0xb8),
            18 => Color.FromRgb(0x5a, 0x2e, 0xd4),
            19 => Color.FromRgb(0x2e, 0x4a, 0xe8),
            20 => Color.FromRgb(0x2e, 0x8e, 0xe8),
            21 => Color.FromRgb(0x2e, 0xc4, 0xd4),
            _ => Color.FromRgb(0x2e, 0xd4, 0x8e),
        };
    }

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
