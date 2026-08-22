using System.Windows.Input;
using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A banner that prompts users to enable Coach when they appear stuck.
/// </summary>
public partial class CoachNudgeBanner : Border
{
#pragma warning disable CS0169 // Field is never used (used by source generator)
    [AutoBindable]
    private readonly ICommand? _dismissCommand;

    [AutoBindable]
    private readonly ICommand? _enableCoachCommand;
#pragma warning restore CS0169

    public CoachNudgeBanner()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Focuses the enable button for accessibility.
    /// </summary>
    public void FocusEnableButton()
    {
        EnableButton?.Focus();
    }
}
