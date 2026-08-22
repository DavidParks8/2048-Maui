using System.Globalization;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Resources.Strings;

namespace TwentyFortyEight.Maui.Converters;

/// <summary>
/// Converts a suggested <see cref="Direction"/> into a localized coach suggestion string.
/// </summary>
public class CoachSuggestionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Direction direction)
        {
            return string.Empty;
        }

        var directionText = direction switch
        {
            Direction.Up => AppStrings.DirectionUp,
            Direction.Down => AppStrings.DirectionDown,
            Direction.Left => AppStrings.DirectionLeft,
            Direction.Right => AppStrings.DirectionRight,
            _ => string.Empty,
        };

        if (string.IsNullOrEmpty(directionText))
        {
            return string.Empty;
        }

        return string.Format(culture, AppStrings.CoachSuggestionFormat, directionText);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotSupportedException();
    }
}
