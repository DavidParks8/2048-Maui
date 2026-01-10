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
    /// Pre-cached font sizes for common tile values (powers of 2).
    /// Avoids Math.Log10 calls during animation setup.
    /// </summary>
    private static readonly Dictionary<int, double> s_fontSizeCache = BuildFontSizeCache();

    private static Dictionary<int, double> BuildFontSizeCache()
    {
        // Pre-cache all powers of 2 from 2 to 2^20 (covers 8x8 boards with high scores)
        Dictionary<int, double> cache = new() { [0] = 32 };
        for (int i = 1; i <= 20; i++)
        {
            int value = 1 << i; // 2^i
            cache[value] = CalculateFontSize(value);
        }
        return cache;
    }

    /// <summary>
    /// Gets the appropriate font size for a tile based on the number of digits.
    /// Uses cached values for common powers of 2.
    /// </summary>
    public static double GetTileFontSize(int value)
    {
        if (s_fontSizeCache.TryGetValue(value, out var cached))
            return cached;
        return CalculateFontSize(value);
    }

    private static double CalculateFontSize(int value)
    {
        if (value == 0)
            return 32;

        var digitCount = (int)Math.Floor(Math.Log10(value)) + 1;

        if (digitCount <= 2)
            return 32;

        if (digitCount <= 6)
        {
            return digitCount switch
            {
                3 => 28,
                4 => 24,
                5 => 20,
                6 => 16,
                _ => 32,
            };
        }

        // For large digit counts, keep shrinking so the full value remains visible.
        // (int max is 10 digits, but this also behaves reasonably for larger counts.)
        return 96.0 / digitCount;
    }

    #endregion

    /// <summary>
    /// Partial method hook called when Value property changes.
    /// Notifies dependent properties to update only if the value actually changed.
    /// </summary>
    partial void OnValueChanged(int oldValue, int newValue)
    {
        // Skip notifications if value didn't actually change
        if (oldValue == newValue)
            return;

        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));

        // Only notify FontSize if digit count changed
        if (GetDigitCount(oldValue) != GetDigitCount(newValue))
        {
            OnPropertyChanged(nameof(FontSize));
        }
    }

    private static int GetDigitCount(int value)
    {
        if (value == 0)
            return 0;
        return (int)Math.Floor(Math.Log10(value)) + 1;
    }

    /// <summary>
    /// Forces a refresh of the color properties.
    /// Useful when the app theme changes.
    /// </summary>
    public void RefreshColors()
    {
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
    }
}
