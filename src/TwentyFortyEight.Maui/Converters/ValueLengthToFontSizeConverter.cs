using System.Globalization;

namespace TwentyFortyEight.Maui.Converters;

/// <summary>
/// Converter that scales font size based on the length of the displayed value.
/// Useful for score displays where larger numbers should use smaller fonts.
/// </summary>
public class ValueLengthToFontSizeConverter : IValueConverter
{
    /// <summary>
    /// The base font size to use for short values.
    /// </summary>
    public double BaseFontSize { get; set; } = 20;

    /// <summary>
    /// The minimum font size to use for very long values.
    /// </summary>
    public double MinFontSize { get; set; } = 12;

    /// <summary>
    /// The character count threshold above which font scaling begins.
    /// </summary>
    public int ScaleThreshold { get; set; } = 4;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString() ?? string.Empty;
        var length = text.Length;

        if (length <= ScaleThreshold)
            return BaseFontSize;

        // Scale down by 2 points for each character over the threshold
        var reduction = (length - ScaleThreshold) * 2;
        var fontSize = Math.Max(MinFontSize, BaseFontSize - reduction);

        return fontSize;
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
