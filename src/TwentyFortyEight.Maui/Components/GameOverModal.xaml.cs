using System.Windows.Input;
using Maui.BindableProperty.Generator.Core;

namespace TwentyFortyEight.Maui.Components;

/// <summary>
/// A modal overlay component displayed when the game is over.
/// </summary>
public partial class GameOverModal : ContentView
{
#pragma warning disable CS0169 // Field is never used - used by source generator

    /// <summary>
    /// Gets or sets the current score to display.
    /// </summary>
    [AutoBindable]
    private readonly int _score;

    /// <summary>
    /// Gets or sets the best score to display.
    /// </summary>
    [AutoBindable]
    private readonly int _bestScore;

    /// <summary>
    /// Gets or sets the command to execute when the New Game button is tapped.
    /// </summary>
    [AutoBindable]
    private readonly ICommand? _newGameCommand;

#pragma warning restore CS0169

    public GameOverModal()
    {
        InitializeComponent();
    }
}
