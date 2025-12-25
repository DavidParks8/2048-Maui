using System.Globalization;

namespace TwentyFortyEight.Maui.Converters;

/// <summary>
/// Converts a boolean value to its inverse.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return true;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return false;
    }
}
