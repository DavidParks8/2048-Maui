using System.Collections.Concurrent;
using System.Numerics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TwentyFortyEight.ViewModels.Helpers;

/// <summary>
/// Helper class for tile color calculations.
/// Provides MAUI-specific Color values for tiles based on their value and current theme.
/// Colors are pre-cached to avoid repeated parsing overhead.
/// </summary>
public static class TileColorHelper
{
    private const int DarkTextThreshold = 4;

    private static readonly int[] TileValueCycle;
    private static readonly int MaxDefinedTileValue;
    private static readonly ConcurrentDictionary<int, int> NormalizedTileValueCache = new();

    private static readonly Color TextColorDark = Color.FromArgb("#776e65");
    private static readonly Color TextColorLight = Color.FromArgb("#f9f6f2");

    // Pre-cached colors for all tile values (light theme)
    private static readonly Dictionary<int, Color> LightThemeColors = new()
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
        [4096] = Color.FromArgb("#edb422"),
        [8192] = Color.FromArgb("#e87e2c"),
        [16384] = Color.FromArgb("#e04a38"),
        [32768] = Color.FromArgb("#d42e55"),
        [65536] = Color.FromArgb("#b82e8c"),
        [131072] = Color.FromArgb("#8e2eb8"),
        [262144] = Color.FromArgb("#5a2ed4"),
        [524288] = Color.FromArgb("#2e4ae8"),
        [1048576] = Color.FromArgb("#2e8ee8"),
    };

    // Pre-cached colors for all tile values (dark theme)
    private static readonly Dictionary<int, Color> DarkThemeColors = new()
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
        [4096] = Color.FromArgb("#edb422"),
        [8192] = Color.FromArgb("#e87e2c"),
        [16384] = Color.FromArgb("#e04a38"),
        [32768] = Color.FromArgb("#d42e55"),
        [65536] = Color.FromArgb("#b82e8c"),
        [131072] = Color.FromArgb("#8e2eb8"),
        [262144] = Color.FromArgb("#5a2ed4"),
        [524288] = Color.FromArgb("#2e4ae8"),
        [1048576] = Color.FromArgb("#2e8ee8"),
    };

    // Pre-cached SolidColorBrush instances (light theme)
    private static readonly Dictionary<int, SolidColorBrush> LightThemeBrushes;

    // Pre-cached SolidColorBrush instances (dark theme)
    private static readonly Dictionary<int, SolidColorBrush> DarkThemeBrushes;

    static TileColorHelper()
    {
        TileValueCycle = [.. LightThemeColors.Keys.OrderBy(x => x)];
        MaxDefinedTileValue = TileValueCycle[^1];

        LightThemeBrushes = new Dictionary<int, SolidColorBrush>(LightThemeColors.Count);
        foreach (var (value, color) in LightThemeColors)
        {
            LightThemeBrushes[value] = new SolidColorBrush(color);
        }

        DarkThemeBrushes = new Dictionary<int, SolidColorBrush>(DarkThemeColors.Count);
        foreach (var (value, color) in DarkThemeColors)
        {
            DarkThemeBrushes[value] = new SolidColorBrush(color);
        }
    }

    /// <summary>
    /// Gets the wrapped key for values above the precomputed range.
    /// Only called for values > MaxDefinedTileValue.
    /// </summary>
    private static int GetWrappedTileValue(int value)
    {
        if (NormalizedTileValueCache.TryGetValue(value, out int cached))
            return cached;

        // Only wrap true powers of two. If something unexpected comes in (e.g. during animation),
        // fall back to the empty tile color.
        int normalized;
        if (!BitOperations.IsPow2(value))
        {
            normalized = 0;
        }
        else
        {
            // Wrap higher powers of two through the defined key cycle.
            // With keys [0, 2, 4, 8, ...], 2^(N+1) maps back to 0.
            int exponent = BitOperations.Log2((uint)value);
            int index = exponent % TileValueCycle.Length;
            normalized = TileValueCycle[index];
        }

        NormalizedTileValueCache.TryAdd(value, normalized);
        return normalized;
    }

    private static T GetFromMap<T>(int value, Dictionary<int, T> map)
    {
        // Hot path: values within precomputed range — single lookup, use result directly.
        if (value <= MaxDefinedTileValue)
            return map.TryGetValue(value, out var result) ? result : map[0];

        // Cold path: wrap higher values then lookup.
        return map[GetWrappedTileValue(value)];
    }

    /// <summary>
    /// Gets the background color for a tile based on its value and the current theme.
    /// </summary>
    public static Color GetTileBackgroundColor(int value)
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return GetFromMap(value, isDark ? DarkThemeColors : LightThemeColors);
    }

    /// <summary>
    /// Gets a cached SolidColorBrush for a tile based on its value and the current theme.
    /// </summary>
    public static SolidColorBrush GetTileBackgroundBrush(int value)
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return GetFromMap(value, isDark ? DarkThemeBrushes : LightThemeBrushes);
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
}
