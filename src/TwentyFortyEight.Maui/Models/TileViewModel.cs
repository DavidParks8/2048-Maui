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

    private const int Log2Of2048 = 11;
    private const double GradientRange = 10.0;

    private static bool IsDarkMode => Application.Current?.RequestedTheme == AppTheme.Dark;

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
        if (value > 4)
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
        var r = (byte)(0xed * (1 - normalizedPower) + 0x8b * normalizedPower);
        var g = (byte)(0xc2 * (1 - normalizedPower));
        var b = (byte)(0x2e * (1 - normalizedPower));

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
