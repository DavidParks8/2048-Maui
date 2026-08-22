namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Manages the logic for showing contextual coach nudges when users appear stuck.
/// </summary>
public interface ICoachNudgeService
{
    /// <summary>
    /// Gets whether the nudge is currently visible.
    /// </summary>
    bool IsNudgeVisible { get; }

    /// <summary>
    /// Tracks an invalid move attempt (no board change).
    /// </summary>
    void TrackInvalidMove();

    /// <summary>
    /// Resets tracking when a valid move is made.
    /// </summary>
    void Reset();

    /// <summary>
    /// Determines if the nudge should be shown based on accumulated state and current settings.
    /// </summary>
    /// <returns>True if the nudge should be shown to the user.</returns>
    bool ShouldShowNudge();

    /// <summary>
    /// Dismisses the nudge, preventing it from showing again this session.
    /// </summary>
    void Dismiss();
}
