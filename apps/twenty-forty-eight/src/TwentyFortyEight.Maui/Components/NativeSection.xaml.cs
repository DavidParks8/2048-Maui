using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A reusable component for displaying a native-styled section with a header and bordered content area.
/// Follows iOS design patterns with section headers outside the card borders.
/// </summary>
public partial class NativeSection : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    /// <summary>
    /// Gets or sets the section header text. Set to empty or null to hide the header.
    /// </summary>
    [AutoBindable(OnChanged = nameof(OnHeaderChanged))]
    private readonly string _header = string.Empty;

    /// <summary>
    /// Gets or sets the padding for the content area inside the border.
    /// Default is "16,0" for standard sections with row content.
    /// </summary>
    [AutoBindable(OnChanged = nameof(OnContentPaddingChanged))]
    private readonly Thickness _contentPadding = new Thickness(16, 0);

    /// <summary>
    /// Gets or sets the content to display inside the bordered section.
    /// </summary>
    [AutoBindable(OnChanged = nameof(OnSectionContentChanged))]
    private readonly View? _sectionContent;

#pragma warning restore CS0169

    /// <summary>
    /// Gets whether the header should be visible.
    /// </summary>
    public bool HasHeader => !string.IsNullOrWhiteSpace(Header);

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeSection"/> class.
    /// </summary>
    public NativeSection()
    {
        InitializeComponent();
        ContentBorder.Padding = ContentPadding;
    }

    private void OnHeaderChanged(string? oldValue, string? newValue)
    {
        HeaderLabel.Text = newValue ?? string.Empty;
        HeaderLabel.IsVisible = !string.IsNullOrWhiteSpace(newValue);
    }

    private void OnContentPaddingChanged(Thickness oldValue, Thickness newValue)
    {
        ContentBorder.Padding = newValue;
    }

    private void OnSectionContentChanged(View? oldValue, View? newValue)
    {
        ContentBorder.Content = newValue;
    }
}
