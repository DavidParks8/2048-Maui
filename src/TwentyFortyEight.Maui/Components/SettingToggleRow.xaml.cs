using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A reusable component for displaying a setting toggle row with a label and switch.
/// </summary>
public partial class SettingToggleRow : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    /// <summary>
    /// Gets or sets the label text displayed for the setting.
    /// </summary>
    [AutoBindable]
    private readonly string _label = string.Empty;

    /// <summary>
    /// Gets or sets the toggled state of the switch.
    /// </summary>
    [AutoBindable(DefaultBindingMode = "TwoWay")]
    private readonly bool _isToggled;

    /// <summary>
    /// Gets or sets the automation ID for the switch control.
    /// </summary>
    [AutoBindable]
    private readonly string _automationId = string.Empty;

    /// <summary>
    /// Gets or sets the semantic hint for accessibility.
    /// </summary>
    [AutoBindable]
    private readonly string _semanticHint = string.Empty;

#pragma warning restore CS0169

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingToggleRow"/> class.
    /// </summary>
    public SettingToggleRow()
    {
        InitializeComponent();
    }
}
