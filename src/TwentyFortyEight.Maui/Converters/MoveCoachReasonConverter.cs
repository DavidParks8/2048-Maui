using System.Globalization;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Resources.Strings;

namespace TwentyFortyEight.Maui.Converters;

/// <summary>
/// Converts <see cref="MoveCoachReason"/> values into localized UI text.
/// </summary>
public class MoveCoachReasonConverter : IValueConverter
{
    /// <summary>
    /// Gets the localized string for a <see cref="MoveCoachReason"/>.
    /// </summary>
    public static string GetLocalizedReason(MoveCoachReason reason) =>
        reason switch
        {
            MoveCoachReason.CreateSpace => AppStrings.CoachReasonCreateSpace,
            MoveCoachReason.MergeTiles => AppStrings.CoachReasonMergeTiles,
            MoveCoachReason.KeepLargestInCorner => AppStrings.CoachReasonKeepLargestInCorner,
            MoveCoachReason.ImproveOrder => AppStrings.CoachReasonImproveOrder,
            MoveCoachReason.AvoidDeadEnd => AppStrings.CoachReasonAvoidDeadEnd,
            _ => string.Empty,
        };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MoveCoachReason reason)
        {
            return string.Empty;
        }

        return GetLocalizedReason(reason);
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
