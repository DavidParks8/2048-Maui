using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TwentyFortyEight.ViewModels.Helpers;

/// <summary>
/// Helper class for tile color calculations.
/// Provides MAUI-specific Color values for tiles based on their value and current theme.
/// </summary>
public static class TileColorHelper
{
    private const int DarkTextThreshold = 4;

    private static readonly Color TextColorDark = Color.FromArgb("#776e65");
    private static readonly Color TextColorLight = Color.FromArgb("#f9f6f2");

    /// <summary>
    /// Gets the background color for a tile based on its value and the current theme.
    /// </summary>
    public static Color GetTileBackgroundColor(int value)
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return GetTileColor(value, isDark);
    }

    /// <summary>
    /// Gets the text color for a tile based on its value and the current theme.
    /// </summary>
    public static Color GetTileTextColor(int value)
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        // In Dark Mode, we always use light text because:
        // 1. Low values (2, 4) have dark backgrounds in Dark Mode.
        // 2. High values (8+) have bright backgrounds that work well with white text.
        // Only use dark text in Light Mode for low values (2, 4)
        bool useDarkText = !isDark && value <= DarkTextThreshold;

        return useDarkText ? TextColorDark : TextColorLight;
    }

    private static Color GetTileColor(int value, bool isDarkTheme)
    {
        // Cap values above 1048576 to use the highest defined color
        if (value > 1048576)
            value = 1048576;

        if (isDarkTheme)
        {
            return value switch
            {
                0 => Color.FromArgb("#524b44"),
                2 => Color.FromArgb("#5c6b7a"),
                4 => Color.FromArgb("#7a6b5c"),
                8 => Color.FromArgb("#f2b179"),
                16 => Color.FromArgb("#f59563"),
                32 => Color.FromArgb("#f67c5f"),
                64 => Color.FromArgb("#f65e3b"),
                128 => Color.FromArgb("#edcf72"),
                256 => Color.FromArgb("#edcc61"),
                512 => Color.FromArgb("#edc850"),
                1024 => Color.FromArgb("#edc53f"),
                2048 => Color.FromArgb("#edc22e"),
                4096 => Color.FromArgb("#edb422"),
                8192 => Color.FromArgb("#e87e2c"),
                16384 => Color.FromArgb("#e04a38"),
                32768 => Color.FromArgb("#d42e55"),
                65536 => Color.FromArgb("#b82e8c"),
                131072 => Color.FromArgb("#8e2eb8"),
                262144 => Color.FromArgb("#5a2ed4"),
                524288 => Color.FromArgb("#2e4ae8"),
                1048576 => Color.FromArgb("#2e8ee8"),
                _ => Color.FromArgb("#524b44"), // fallback
            };
        }
        else // Light theme
        {
            return value switch
            {
                0 => Color.FromArgb("#cdc1b4"),
                2 => Color.FromArgb("#eee4da"),
                4 => Color.FromArgb("#ede0c8"),
                8 => Color.FromArgb("#f2b179"),
                16 => Color.FromArgb("#f59563"),
                32 => Color.FromArgb("#f67c5f"),
                64 => Color.FromArgb("#f65e3b"),
                128 => Color.FromArgb("#edcf72"),
                256 => Color.FromArgb("#edcc61"),
                512 => Color.FromArgb("#edc850"),
                1024 => Color.FromArgb("#edc53f"),
                2048 => Color.FromArgb("#edc22e"),
                4096 => Color.FromArgb("#edb422"),
                8192 => Color.FromArgb("#e87e2c"),
                16384 => Color.FromArgb("#e04a38"),
                32768 => Color.FromArgb("#d42e55"),
                65536 => Color.FromArgb("#b82e8c"),
                131072 => Color.FromArgb("#8e2eb8"),
                262144 => Color.FromArgb("#5a2ed4"),
                524288 => Color.FromArgb("#2e4ae8"),
                1048576 => Color.FromArgb("#2e8ee8"),
                _ => Color.FromArgb("#cdc1b4"), // fallback
            };
        }
    }
}
