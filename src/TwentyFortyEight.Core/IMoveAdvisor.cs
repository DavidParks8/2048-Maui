namespace TwentyFortyEight.Core;

/// <summary>
/// Provides a recommended next move for a given board state.
/// Intended for "coach"/hint UX.
/// </summary>
public interface IMoveAdvisor
{
    /// <summary>
    /// Returns the recommended move for the current board state, or <see langword="null"/>
    /// if no valid move exists.
    /// </summary>
    MoveRecommendation? Recommend(MoveAdvisorRequest request);
}
