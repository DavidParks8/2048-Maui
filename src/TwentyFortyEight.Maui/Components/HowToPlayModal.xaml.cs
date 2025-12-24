using System.Windows.Input;
using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A modal overlay component that displays how to play instructions.
/// </summary>
public partial class HowToPlayModal : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    /// <summary>
    /// Gets or sets the command to execute when the modal is closed.
    /// </summary>
    [AutoBindable]
    private readonly ICommand? _closeCommand;

#pragma warning restore CS0169

    public HowToPlayModal()
    {
        InitializeComponent();
    }
}
