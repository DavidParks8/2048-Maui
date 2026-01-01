namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Abstracts the platform-specific victory animation rendering.
/// Implemented in the MAUI layer to coordinate SkiaSharp rendering.
/// </summary>
public interface IVictoryAnimationService
{
    /// <summary>
    /// Raised when the animation reaches the point where the modal should be shown.
    /// </summary>
    event EventHandler? ShowModalRequested;

    /// <summary>
    /// Raised when the animation has fully completed or been stopped.
    /// </summary>
    event EventHandler? AnimationCompleted;

    /// <summary>
    /// Initializes the animation service with UI references.
    /// Called once during page setup.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Starts the victory animation sequence.
    /// </summary>
    /// <param name="winningTileRow">Row of the winning tile.</param>
    /// <param name="winningTileColumn">Column of the winning tile.</param>
    /// <param name="score">Score at time of victory.</param>
    /// <returns>Task that completes when animation setup is done (not when animation ends).</returns>
    Task StartAnimationAsync(int winningTileRow, int winningTileColumn, int score);

    /// <summary>
    /// Stops the animation and hides the overlay.
    /// </summary>
    void StopAnimation();

    /// <summary>
    /// Transitions to the sustained warp effect (called when modal becomes visible).
    /// </summary>
    void EnterSustainMode();
}
