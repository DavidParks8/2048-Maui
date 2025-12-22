namespace TwentyFortyEight.Maui.Models;

/// <summary>
/// Represents a tile in the 2048 game grid.
/// </summary>
public class TileViewModel : ViewModels.BaseViewModel
{
    private int _value;
    private int _row;
    private int _column;
    private int _previousRow;
    private int _previousColumn;
    private bool _isNewTile;
    private bool _isMerged;

    public int Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public int Row
    {
        get => _row;
        set => SetProperty(ref _row, value);
    }

    public int Column
    {
        get => _column;
        set => SetProperty(ref _column, value);
    }

    public int PreviousRow
    {
        get => _previousRow;
        set => SetProperty(ref _previousRow, value);
    }

    public int PreviousColumn
    {
        get => _previousColumn;
        set => SetProperty(ref _previousColumn, value);
    }

    public bool IsNewTile
    {
        get => _isNewTile;
        set => SetProperty(ref _isNewTile, value);
    }

    public bool IsMerged
    {
        get => _isMerged;
        set => SetProperty(ref _isMerged, value);
    }

    public string DisplayValue => Value == 0 ? "" : Value.ToString();

    public Color BackgroundColor => GetBackgroundColor(Value);

    public Color TextColor => Value > 4 ? Colors.White : Color.FromArgb("#776e65");

    private const int Log2Of2048 = 11;
    private const double GradientRange = 10.0;

    private Color GetBackgroundColor(int value)
    {
        if (value == 0) return Color.FromArgb("#cdc1b4");
        if (value == 2) return Color.FromArgb("#eee4da");
        if (value == 4) return Color.FromArgb("#ede0c8");
        if (value == 8) return Color.FromArgb("#f2b179");
        if (value == 16) return Color.FromArgb("#f59563");
        if (value == 32) return Color.FromArgb("#f67c5f");
        if (value == 64) return Color.FromArgb("#f65e3b");
        if (value == 128) return Color.FromArgb("#edcf72");
        if (value == 256) return Color.FromArgb("#edcc61");
        if (value == 512) return Color.FromArgb("#edc850");
        if (value == 1024) return Color.FromArgb("#edc53f");
        if (value == 2048) return Color.FromArgb("#edc22e");
        
        // For values > 2048, generate color based on the power of 2
        // Use a color gradient from gold to dark red
        var power = (int)Math.Log2(value);
        var normalizedPower = (power - Log2Of2048) / GradientRange;
        normalizedPower = Math.Clamp(normalizedPower, 0, 1);
        
        // Interpolate between gold (#edc22e) and dark red (#8b0000)
        var r = (byte)(0xed * (1 - normalizedPower) + 0x8b * normalizedPower);
        var g = (byte)(0xc2 * (1 - normalizedPower));
        var b = (byte)(0x2e * (1 - normalizedPower));
        
        return Color.FromRgb(r, g, b);
    }

    public void UpdateValue(int newValue)
    {
        Value = newValue;
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
    }
}
