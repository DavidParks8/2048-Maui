namespace TwentyFortyEight.Maui.Models;

/// <summary>
/// Represents a tile in the 2048 game grid.
/// </summary>
public class TileViewModel : ViewModels.BaseViewModel
{
    private int _value;
    private int _row;
    private int _column;

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

    public string DisplayValue => Value == 0 ? "" : Value.ToString();

    public Color BackgroundColor => GetBackgroundColor(Value);

    public Color TextColor => Value > 4 ? Colors.White : Color.FromArgb("#776e65");

    private Color GetBackgroundColor(int value) => value switch
    {
        0 => Color.FromArgb("#cdc1b4"),
        2 => Color.FromArgb("#eee4da"),
        4 => Color.FromArgb("#ede0c8"),
        8 => Color.FromArgb("#f2b179"),
        16 => Color.FromArgb("#f59563"),
        32 => Color.FromArgb("#f67c5f"),
        64 => Color.FromArgb("#f65e3b"),
        128 => Color.FromArgb("#edcf72"),
        256 => Color.FromArgb("#edcc61"),
        512 => Color.FromArgb("#edc850"),
        1024 => Color.FromArgb("#edc53f"),
        2048 => Color.FromArgb("#edc22e"),
        _ => Color.FromArgb("#3c3a32")
    };

    public void UpdateValue(int newValue)
    {
        Value = newValue;
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
    }
}
