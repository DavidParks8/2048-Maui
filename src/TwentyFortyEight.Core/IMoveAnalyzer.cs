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
    /// <remarks>
    /// <para>
    /// <b>IMPORTANT:</b> The returned <see cref="MoveAnalysisResult"/> is reused across calls
    /// on the same thread. The result is only valid until the next call to <see cref="Analyze"/>.
    /// </para>
    /// <para>
    /// If you need to preserve the result beyond the next call, copy the data you need
    /// (e.g., <c>result.Movements.ToList()</c>) before calling <see cref="Analyze"/> again.
    /// </para>
    /// </remarks>
    /// <param name="previousBoard">The board state before the move.</param>
    /// <param name="newBoard">The board state after the move (including spawned tile).</param>
    /// <param name="direction">The direction of the move.</param>
    /// <returns>Analysis result with movements and tile categorizations. This instance is reused; see remarks.</returns>
    MoveAnalysisResult Analyze(Board previousBoard, Board newBoard, Direction direction);
}
