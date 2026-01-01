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
    /// Set after cinematic animation completes (or immediately if reduce motion).
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
    /// Current phase of the victory animation.
    /// </summary>
    [ObservableProperty]
    private VictoryAnimationPhase _phase = VictoryAnimationPhase.None;

    /// <summary>
    /// Progress within the current phase (0.0 to 1.0).
    /// </summary>
    [ObservableProperty]
    private float _phaseProgress;

    /// <summary>
    /// Row of the winning tile on the board.
    /// </summary>
    [ObservableProperty]
    private int _winningTileRow;

    /// <summary>
    /// Column of the winning tile on the board.
    /// </summary>
    [ObservableProperty]
    private int _winningTileColumn;

    /// <summary>
    /// Resets the victory state to its initial values.
    /// </summary>
    public void Reset()
    {
        IsActive = false;
        IsModalVisible = false;
        WinningValue = 0;
        Score = 0;
        Phase = VictoryAnimationPhase.None;
        PhaseProgress = 0f;
        WinningTileRow = 0;
        WinningTileColumn = 0;
    }
}
