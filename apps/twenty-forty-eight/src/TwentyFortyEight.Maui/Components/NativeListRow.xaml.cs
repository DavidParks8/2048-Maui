using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A reusable component for displaying a two-column row with a label and value.
/// Follows native iOS list styling with standard padding and typography.
/// </summary>
public partial class NativeListRow : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    /// <summary>
    /// Gets or sets the label text displayed on the left side.
    /// </summary>
    [AutoBindable]
    private readonly string _label = string.Empty;

    /// <summary>
    /// Gets or sets the value text displayed on the right side in bold.
    /// </summary>
    [AutoBindable]
    private readonly string _value = string.Empty;

#pragma warning restore CS0169

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeListRow"/> class.
    /// </summary>
    public NativeListRow()
    {
        InitializeComponent();
    }
}
