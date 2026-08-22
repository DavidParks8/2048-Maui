using CommunityToolkit.Mvvm.ComponentModel;

namespace GoodMovies.ViewModels;

/// <summary>
/// A word in the detail overview with its character range in the text being
/// spoken. Only the highlight changes while the immutable text and offsets do
/// not.
/// </summary>
public partial class WordTokenViewModel : ObservableObject
{
    public WordTokenViewModel(string text, int start, int length)
    {
        Text = text ?? string.Empty;
        Start = Math.Max(0, start);
        Length = Math.Max(0, length);
    }

    public string Text { get; }

    public string Word => Text;

    public int Start { get; }

    public int CharacterStart => Start;

    public int Offset => Start;

    public int CharacterIndex => Start;

    public int Length { get; }

    public int CharacterLength => Length;

    public int End => Start + Length;

    [ObservableProperty]
    private bool _isHighlighted;

    public bool IsSpoken => IsHighlighted;

    partial void OnIsHighlightedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSpoken));
    }
}
