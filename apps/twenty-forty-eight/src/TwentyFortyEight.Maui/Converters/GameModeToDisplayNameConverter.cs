using System.Globalization;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Resources.Strings;

namespace TwentyFortyEight.Maui.Converters;

public sealed class GameModeToDisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not GameMode mode)
        {
            return AppStrings.ModernMode;
        }

        return mode switch
        {
            GameMode.Modern => AppStrings.ModernMode,
            GameMode.Classic => AppStrings.ClassicMode,
            GameMode.Walltastrophy => AppStrings.WalltastrophyMode,
            GameMode.Adversarial => AppStrings.AdversarialMode,
            _ => AppStrings.ModernMode,
        };
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
