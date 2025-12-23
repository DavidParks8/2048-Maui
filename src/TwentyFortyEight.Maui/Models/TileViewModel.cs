using System.Collections.Frozen;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TwentyFortyEight.Maui.Models;

/// <summary>
/// Represents a tile in the 2048 game grid.
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

    public Color BackgroundColor => GetTileBackgroundColor(Value);

    public Color TextColor => GetTileTextColor(Value);

    #region Constants

    /// <summary>
    /// The log base 2 of 2048 (used for high-value color gradient calculations).
    /// </summary>
    private const int Log2Of2048 = 11;

    /// <summary>
    /// The range of power values over which the high-value gradient transitions.
    /// </summary>
    private const double GradientRange = 10.0;

    /// <summary>
    /// Tile values at or below this threshold use the dark text color.
    /// </summary>
    private const int DarkTextThreshold = 4;

    /// <summary>
    /// Red component of the gold color used as gradient start (#edc22e).
    /// </summary>
    private const byte GoldRed = 0xed;

    /// <summary>
    /// Green component of the gold color used as gradient start (#edc22e).
    /// </summary>
    private const byte GoldGreen = 0xc2;

    /// <summary>
    /// Blue component of the gold color used as gradient start (#edc22e).
    /// </summary>
    private const byte GoldBlue = 0x2e;

    /// <summary>
    /// Red component of the dark red color used as gradient end (#8b0000).
    /// </summary>
    private const byte DarkRedRed = 0x8b;

    #endregion

    #region Theme Caching

    private static bool s_isDarkModeCached;
    private static bool s_themeCacheInitialized;

    /// <summary>
    /// Static constructor to set up theme change monitoring.
    /// </summary>
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

    // Pre-computed colors to avoid parsing hex strings on every access
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
        [2] = Color.FromArgb("#5c6b7a"), // Blue-gray tint
        [4] = Color.FromArgb("#7a6b5c"), // Warm tan/brown tint
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
        // For values > 2048, generate color based on the power of 2
        // Use a color gradient from gold to dark red
        var power = (int)Math.Log2(value);
        var normalizedPower = (power - Log2Of2048) / GradientRange;
        normalizedPower = Math.Clamp(normalizedPower, 0, 1);

        // Interpolate between gold (#edc22e) and dark red (#8b0000)
        var r = (byte)(GoldRed * (1 - normalizedPower) + DarkRedRed * normalizedPower);
        var g = (byte)(GoldGreen * (1 - normalizedPower));
        var b = (byte)(GoldBlue * (1 - normalizedPower));

        return Color.FromRgb(r, g, b);
    }

    /// <summary>
    /// Partial method hook called when Value property changes.
    /// Notifies dependent properties to update.
    /// </summary>
    partial void OnValueChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
    }

    public void UpdateValue(int newValue)
    {
        Value = newValue;
    }
}
