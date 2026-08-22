using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

public partial class HeaderStatCard : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    [AutoBindable]
    private readonly string _title = string.Empty;

    [AutoBindable]
    private readonly string _value = string.Empty;

    [AutoBindable]
    private readonly string _valueDescription = string.Empty;

#pragma warning restore CS0169

    public HeaderStatCard()
    {
        InitializeComponent();
    }
}
