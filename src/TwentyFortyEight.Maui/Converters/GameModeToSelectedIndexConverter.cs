using System.Globalization;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Converters;

public sealed class GameModeToSelectedIndexConverter : IValueConverter
{
    public static GameModeToSelectedIndexConverter Instance { get; } = new();

    private GameModeToSelectedIndexConverter() { }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not GameMode mode)
            return 0;

        return (int)mode;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is not int selectedIndex || selectedIndex < 0)
            return GameMode.Classic;

        return (GameMode)selectedIndex;
    }
}
