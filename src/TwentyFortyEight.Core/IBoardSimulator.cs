namespace TwentyFortyEight.Core;

/// <summary>
/// Provides board simulation capabilities for preview/analysis without mutating game state.
/// </summary>
public interface IBoardSimulator
{
    /// <summary>
    /// Simulates a move on the given board without modifying it.
    /// </summary>
    /// <param name="request">Move simulation request.</param>
    /// <returns>
    /// A tuple containing the resulting board, score increase from merges,
    /// whether the board changed, and the maximum value merged.
    /// </returns>
    (Board newBoard, int scoreIncrease, bool moved, int maxMergedValue) SimulateMove(
        BoardMoveRequest request
    );
}
