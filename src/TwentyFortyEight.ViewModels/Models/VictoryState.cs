using CommunityToolkit.Mvvm.ComponentModel;

namespace TwentyFortyEight.ViewModels.Models;

/// <summary>
/// Observable state for the victory celebration flow.
/// Tracks animation phase, progress, and display values.
/// </summary>
public sealed partial class VictoryState : ObservableObject
{
    /// <summary>
    /// Whether the victory overlay/modal should be visible.
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// Whether the victory modal card should be visible.
    /// Set after the victory animation begins (or immediately if reduce motion).
    /// </summary>
    [ObservableProperty]
    private bool _isModalVisible;

    /// <summary>
    /// The winning tile value (e.g., 2048).
    /// </summary>
    [ObservableProperty]
    private int _winningValue;

    /// <summary>
    /// The score at the time of victory.
    /// </summary>
    [ObservableProperty]
    private int _score;

    /// <summary>
    /// Resets the victory state to its initial values.
    /// </summary>
    public void Reset()
    {
        IsActive = false;
        IsModalVisible = false;
        WinningValue = 0;
        Score = 0;
    }
}
