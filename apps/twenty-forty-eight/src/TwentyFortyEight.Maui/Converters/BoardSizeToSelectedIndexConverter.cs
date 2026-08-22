using System.Globalization;

namespace TwentyFortyEight.Maui.Converters;

public static class BoardSizePickerConstants
{
    public const int MinSize = 3;
    public const int MaxSize = 8;
}

public sealed class BoardSizeToSelectedIndexConverter : IValueConverter
{
    public static BoardSizeToSelectedIndexConverter Instance { get; } = new();

    private BoardSizeToSelectedIndexConverter() { }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int boardSize)
            return -1;

        return boardSize - BoardSizePickerConstants.MinSize;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is not int selectedIndex || selectedIndex < 0)
            return BoardSizePickerConstants.MinSize;

        return selectedIndex + BoardSizePickerConstants.MinSize;
    }
}
