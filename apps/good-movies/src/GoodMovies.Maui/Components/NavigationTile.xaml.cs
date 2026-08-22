using System.Windows.Input;
using Maui.BindableProperty.Generator.Core;

namespace GoodMovies.Maui.Components;

public partial class NavigationTile : ContentView
{
#pragma warning disable CS0169
    [AutoBindable]
    private readonly string _icon = string.Empty;

    [AutoBindable]
    private readonly string _label = string.Empty;

    [AutoBindable]
    private readonly string _subtext = string.Empty;

    [AutoBindable]
    private readonly ICommand? _command;

    [AutoBindable]
    private readonly object? _commandParameter;

    [AutoBindable(DefaultValue = "false")]
    private readonly bool _isSelected;

    [AutoBindable(DefaultValue = "true")]
    private readonly bool _isButtonEnabled;

    [AutoBindable(DefaultValue = "false")]
    private readonly bool _isCompact;

    [AutoBindable]
    private readonly string _automationId = string.Empty;

    [AutoBindable]
    private readonly string _accessibilityLabel = string.Empty;

    [AutoBindable]
    private readonly string _accessibilityHint = string.Empty;
#pragma warning restore CS0169

    public NavigationTile()
    {
        InitializeComponent();
    }

    private void OnPressed(object? sender, EventArgs e) => TileBorder.TranslationY = 4;

    private void OnReleased(object? sender, EventArgs e) => TileBorder.TranslationY = 0;
}
