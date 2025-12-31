using System.Globalization;

namespace TwentyFortyEight.Maui.Converters;

/// <summary>
/// Converts a boolean value to an opacity value.
/// True = 1.0 (fully visible), False = 0.4 (dimmed).
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? 1.0 : 0.4;
        }
        return 1.0;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotImplementedException();
    }
}
