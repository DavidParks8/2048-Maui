namespace TwentyFortyEight.Core;

/// <summary>
/// Analyzes tile movements and categorizes tiles after a move.
/// This logic is platform-agnostic and can be used by any UI implementation.
/// </summary>
public interface IMoveAnalyzer
{
    /// <summary>
    /// Analyzes a move by comparing the previous and new board states.
    /// Returns movement information and tile categorizations.
    /// </summary>
    /// <param name="previousBoard">The board state before the move.</param>
    /// <param name="newBoard">The board state after the move (including spawned tile).</param>
    /// <param name="size">The size of the board (e.g., 4 for a 4x4 board).</param>
    /// <param name="direction">The direction of the move.</param>
    /// <returns>Analysis result with movements and tile categorizations.</returns>
    MoveAnalysisResult Analyze(int[] previousBoard, int[] newBoard, int size, Direction direction);
}
