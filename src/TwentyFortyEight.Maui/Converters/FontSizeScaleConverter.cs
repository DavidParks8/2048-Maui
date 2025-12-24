using System.Globalization;

namespace TwentyFortyEight.Maui.Converters;

/// <summary>
/// Converter that scales a font size value by the board scale factor.
/// </summary>
public class FontSizeScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double fontSize)
            return 32.0;

        if (parameter is not double scaleFactor)
            return fontSize;

        return fontSize * scaleFactor;
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
