using Godot;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Color definitions for the 2048 game tiles.
/// Matches the MAUI implementation's TileColorHelper.
/// </summary>
public static class TileColors
{
    // Text colors
    public static readonly Color TextColorDark = new("776e65");
    public static readonly Color TextColorLight = new("f9f6f2");

    // Light theme tile colors
    private static readonly Dictionary<int, Color> LightThemeColors = new()
    {
        [0] = new Color("cdc1b4"),
        [2] = new Color("eee4da"),
        [4] = new Color("ede0c8"),
        [8] = new Color("f2b179"),
        [16] = new Color("f59563"),
        [32] = new Color("f67c5f"),
        [64] = new Color("f65e3b"),
        [128] = new Color("edcf72"),
        [256] = new Color("edcc61"),
        [512] = new Color("edc850"),
        [1024] = new Color("edc53f"),
        [2048] = new Color("edc22e"),
        [4096] = new Color("edb422"),
        [8192] = new Color("e87e2c"),
        [16384] = new Color("e04a38"),
        [32768] = new Color("d42e55"),
        [65536] = new Color("b82e8c"),
        [131072] = new Color("8e2eb8"),
        [262144] = new Color("5a2ed4"),
        [524288] = new Color("2e4ae8"),
        [1048576] = new Color("2e8ee8"),
    };

    // Dark theme tile colors
    private static readonly Dictionary<int, Color> DarkThemeColors = new()
    {
        [0] = new Color("524b44"),
        [2] = new Color("5c6b7a"),
        [4] = new Color("7a6b5c"),
        [8] = new Color("f2b179"),
        [16] = new Color("f59563"),
        [32] = new Color("f67c5f"),
        [64] = new Color("f65e3b"),
        [128] = new Color("edcf72"),
        [256] = new Color("edcc61"),
        [512] = new Color("edc850"),
        [1024] = new Color("edc53f"),
        [2048] = new Color("edc22e"),
        [4096] = new Color("edb422"),
        [8192] = new Color("e87e2c"),
        [16384] = new Color("e04a38"),
        [32768] = new Color("d42e55"),
        [65536] = new Color("b82e8c"),
        [131072] = new Color("8e2eb8"),
        [262144] = new Color("5a2ed4"),
        [524288] = new Color("2e4ae8"),
        [1048576] = new Color("2e8ee8"),
    };

    // Page backgrounds
    public static readonly Color PageBackgroundLight = new("faf8ef");
    public static readonly Color PageBackgroundDark = new("1a1a2e");

    // Panel backgrounds
    public static readonly Color PanelBackgroundLight = new("bbada0");
    public static readonly Color PanelBackgroundDark = new("3d3d5c");

    // Wall color
    public static readonly Color WallColorLight = new("776e65");
    public static readonly Color WallColorDark = new("a0a0a0");

    /// <summary>
    /// Gets the background color for a tile based on its value.
    /// </summary>
    public static Color GetTileBackgroundColor(int value, bool isDarkTheme = false)
    {
        var colors = isDarkTheme ? DarkThemeColors : LightThemeColors;

        if (colors.TryGetValue(value, out var color))
            return color;

        // For very large values, wrap around
        if (value > 0)
        {
            int normalizedValue = NormalizeValue(value);
            if (colors.TryGetValue(normalizedValue, out color))
                return color;
        }

        return colors[0];
    }

    /// <summary>
    /// Gets the text color for a tile based on its value.
    /// </summary>
    public static Color GetTileTextColor(int value, bool isDarkTheme = false)
    {
        // In Dark Mode, always use light text
        // In Light Mode, use dark text for low values (2, 4)
        bool useDarkText = !isDarkTheme && value <= 4;
        return useDarkText ? TextColorDark : TextColorLight;
    }

    /// <summary>
    /// Gets the appropriate font size for a tile based on the number of digits.
    /// </summary>
    public static int GetTileFontSize(int value, float tileSize)
    {
        if (value == 0)
            return (int)(tileSize * 0.4f);

        int digitCount = (int)Math.Floor(Math.Log10(value)) + 1;

        float baseFontSize = tileSize * 0.4f;

        return digitCount switch
        {
            1 or 2 => (int)baseFontSize,
            3 => (int)(baseFontSize * 0.875f),
            4 => (int)(baseFontSize * 0.75f),
            5 => (int)(baseFontSize * 0.625f),
            6 => (int)(baseFontSize * 0.5f),
            _ => (int)(baseFontSize * 3.0f / digitCount),
        };
    }

    private static int NormalizeValue(int value)
    {
        // Find the closest power of 2
        if (value <= 0)
            return 0;

        int exp = (int)Math.Log2(value);
        int[] knownValues =
        [
            2,
            4,
            8,
            16,
            32,
            64,
            128,
            256,
            512,
            1024,
            2048,
            4096,
            8192,
            16384,
            32768,
            65536,
            131072,
            262144,
            524288,
            1048576,
        ];

        int index = exp % knownValues.Length;
        return knownValues[index];
    }
}
