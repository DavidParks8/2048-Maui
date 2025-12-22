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

    /// <summary>
    /// Gets the text color for a tile with the specified value.
    /// </summary>
    public static Color GetTileTextColor(int value)
    {
        if (value > 4)
            return Colors.White;

        return IsDarkMode ? Color.FromArgb("#f9f6f2") : Color.FromArgb("#776e65");
    }

    /// <summary>
    /// Gets the background color for a tile with the specified value.
    /// </summary>
    public static Color GetTileBackgroundColor(int value)
    {
        if (IsDarkMode)
            return GetDarkModeBackgroundColor(value);

        return GetLightModeBackgroundColor(value);
    }

    private static Color GetLightModeBackgroundColor(int value)
    {
        if (value == 0)
            return Color.FromArgb("#cdc1b4");
        if (value == 2)
            return Color.FromArgb("#eee4da");
        if (value == 4)
            return Color.FromArgb("#ede0c8");
        if (value == 8)
            return Color.FromArgb("#f2b179");
        if (value == 16)
            return Color.FromArgb("#f59563");
        if (value == 32)
            return Color.FromArgb("#f67c5f");
        if (value == 64)
            return Color.FromArgb("#f65e3b");
        if (value == 128)
            return Color.FromArgb("#edcf72");
        if (value == 256)
            return Color.FromArgb("#edcc61");
        if (value == 512)
            return Color.FromArgb("#edc850");
        if (value == 1024)
            return Color.FromArgb("#edc53f");
        if (value == 2048)
            return Color.FromArgb("#edc22e");

        return GetHighValueColor(value);
    }

    private static Color GetDarkModeBackgroundColor(int value)
    {
        // Dark mode: empty tiles should be darker to contrast with the board
        if (value == 0)
            return Color.FromArgb("#524b44");
        // Use distinct hues for better colorblind accessibility
        if (value == 2)
            return Color.FromArgb("#5c6b7a"); // Blue-gray tint
        if (value == 4)
            return Color.FromArgb("#7a6b5c"); // Warm tan/brown tint
        // Higher value tiles keep their vibrant colors for visibility
        if (value == 8)
            return Color.FromArgb("#f2b179");
        if (value == 16)
            return Color.FromArgb("#f59563");
        if (value == 32)
            return Color.FromArgb("#f67c5f");
        if (value == 64)
            return Color.FromArgb("#f65e3b");
        if (value == 128)
            return Color.FromArgb("#edcf72");
        if (value == 256)
            return Color.FromArgb("#edcc61");
        if (value == 512)
            return Color.FromArgb("#edc850");
        if (value == 1024)
            return Color.FromArgb("#edc53f");
        if (value == 2048)
            return Color.FromArgb("#edc22e");

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
