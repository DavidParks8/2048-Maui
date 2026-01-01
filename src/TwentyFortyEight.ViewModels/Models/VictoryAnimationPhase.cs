namespace TwentyFortyEight.ViewModels.Models;

/// <summary>
/// Represents the current phase of the victory animation.
/// </summary>
public enum VictoryAnimationPhase
{
    /// <summary>
    /// No victory animation is currently active.
    /// </summary>
    None,

    /// <summary>
    /// Initial impact/shockwave phase when victory is first achieved.
    /// </summary>
    Impact,

    /// <summary>
    /// Transition phase with warp effect building.
    /// </summary>
    WarpTransition,

    /// <summary>
    /// Sustained warp effect while modal is displayed.
    /// </summary>
    WarpSustain,

    /// <summary>
    /// Modal is visible and animation has completed or was skipped.
    /// </summary>
    ModalVisible,
}
