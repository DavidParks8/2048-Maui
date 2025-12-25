using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace TwentyFortyEight.ViewModels.Helpers;

/// <summary>
/// Helper class for tile color calculations.
/// Provides MAUI-specific Color values for tiles based on their value.
/// </summary>
public static class TileColorHelper
{
    private const int DarkTextThreshold = 4;

    // Fallback colors in case resources are missing
    private static readonly Color FallbackColor = Color.FromArgb("#cdc1b4");
    private static readonly Color FallbackTextDark = Color.FromArgb("#776e65");
    private static readonly Color FallbackTextLight = Color.FromArgb("#f9f6f2");

    public static Color GetTileBackgroundColor(int value)
    {
        if (Application.Current == null)
            return FallbackColor;

        bool isDark = Application.Current.RequestedTheme == AppTheme.Dark;
        string suffix = isDark ? "Dark" : "Light";
        string key = $"TileColor{value}{suffix}";

        // Handle values > 1048576 by capping at 1048576 for color lookup
        if (value > 1048576)
        {
            key = $"TileColor1048576{suffix}";
        }

        return GetColorFromResource(key, FallbackColor);
    }

    public static Color GetTileTextColor(int value)
    {
        if (Application.Current == null)
            return FallbackTextDark;

        // In Dark Mode, we always use light text because:
        // 1. Low values (2, 4) have dark backgrounds in Dark Mode.
        // 2. High values (8+) have bright backgrounds that work well with white text.
        bool isDark = Application.Current.RequestedTheme == AppTheme.Dark;

        // Only use dark text in Light Mode for low values (2, 4)
        bool useDarkText = !isDark && value <= DarkTextThreshold;

        string key = useDarkText ? "TileTextDark" : "TileTextLight";

        return GetColorFromResource(key, useDarkText ? FallbackTextDark : FallbackTextLight);
    }

    private static Color GetColorFromResource(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var resource) == true)
        {
            if (resource is Color color)
            {
                return color;
            }
        }

        return fallback;
    }
}
