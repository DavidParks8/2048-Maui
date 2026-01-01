using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels;

/// <summary>
/// ViewModel for the victory celebration flow.
/// Manages victory state, animations, and user interactions.
/// </summary>
public sealed partial class VictoryViewModel(
    IReduceMotionService reduceMotionService,
    IUserFeedbackService userFeedbackService,
    ILocalizationService localizationService
) : ObservableObject
{
    /// <summary>
    /// Observable state for the victory flow.
    /// </summary>
    [ObservableProperty]
    private VictoryState _state = new();

    /// <summary>
    /// Gets the localized score display text.
    /// </summary>
    public string ScoreDisplayText => localizationService.FormatScore(State.Score);

    /// <summary>
    /// Event raised when the user chooses to keep playing after victory.
    /// </summary>
    public event EventHandler? KeepPlayingRequested;

    /// <summary>
    /// Event raised when the user chooses to start a new game after victory.
    /// </summary>
    public event EventHandler? NewGameRequested;

    /// <summary>
    /// Event raised when the victory animation should start.
    /// The View layer listens to this to coordinate platform-specific rendering.
    /// </summary>
    public event EventHandler<VictoryAnimationStartEventArgs>? AnimationStartRequested;

    /// <summary>
    /// Event raised when the victory animation should stop immediately.
    /// </summary>
    public event EventHandler? AnimationStopRequested;

    /// <summary>
    /// Whether animations should be skipped due to accessibility preferences.
    /// </summary>
    public bool ShouldReduceMotion => reduceMotionService.ShouldReduceMotion();

    /// <summary>
    /// Triggers the victory celebration flow.
    /// Called when the game engine raises VictoryAchieved.
    /// </summary>
    /// <param name="victoryArgs">Event args from the engine with winning tile info.</param>
    /// <param name="score">Current score at time of victory.</param>
    /// <param name="winningValue">The winning tile value (e.g., 2048).</param>
    public void TriggerVictory(VictoryEventArgs victoryArgs, int score, int winningValue = 2048)
    {
        State.WinningTileRow = victoryArgs.WinningTileRow;
        State.WinningTileColumn = victoryArgs.WinningTileColumn;
        State.Score = score;
        State.WinningValue = winningValue;
        State.IsActive = true;

        // Notify that the formatted score text has changed
        OnPropertyChanged(nameof(ScoreDisplayText));

        if (ShouldReduceMotion)
        {
            // Skip animation, go directly to modal
            State.Phase = VictoryAnimationPhase.ModalVisible;
            State.IsModalVisible = true;
            userFeedbackService.PerformVictoryHaptic();
            userFeedbackService.AnnounceWin();
        }
        else
        {
            // Start animation sequence
            State.Phase = VictoryAnimationPhase.Impact;
            State.PhaseProgress = 0f;

            AnimationStartRequested?.Invoke(
                this,
                new VictoryAnimationStartEventArgs
                {
                    WinningTileRow = victoryArgs.WinningTileRow,
                    WinningTileColumn = victoryArgs.WinningTileColumn,
                    Score = score,
                }
            );
        }
    }

    /// <summary>
    /// Called by the animation service to update animation progress.
    /// </summary>
    /// <param name="phase">Current animation phase.</param>
    /// <param name="progress">Progress within the phase (0.0-1.0).</param>
    public void UpdateAnimationProgress(VictoryAnimationPhase phase, float progress)
    {
        State.Phase = phase;
        State.PhaseProgress = progress;
    }

    /// <summary>
    /// Called by the animation service when it's time to show the modal.
    /// </summary>
    public void ShowModal()
    {
        State.Phase = VictoryAnimationPhase.ModalVisible;
        State.IsModalVisible = true;
        userFeedbackService.AnnounceWin();
    }

    /// <summary>
    /// User chose to continue playing past 2048.
    /// </summary>
    [RelayCommand]
    private void KeepPlaying()
    {
        HideVictoryOverlay();
        KeepPlayingRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// User chose to start a new game.
    /// </summary>
    [RelayCommand]
    private void NewGame()
    {
        HideVictoryOverlay();
        NewGameRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HideVictoryOverlay()
    {
        AnimationStopRequested?.Invoke(this, EventArgs.Empty);
        State.Reset();
    }
}
