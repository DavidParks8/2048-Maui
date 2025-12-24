using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

/// <summary>
/// Helper methods for creating test fixtures.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Creates a GameState from a flat board array for testing.
    /// </summary>
    public static GameState CreateGameState(
        int[] boardData,
        int size = 4,
        int score = 0,
        int moveCount = 0,
        bool isWon = false,
        bool isGameOver = false
    )
    {
        var board = new Board(boardData, size);
        var maxTileValue = boardData.Length > 0 ? boardData.Max() : 0;
        return new GameState(board, score, moveCount, isWon, isGameOver, maxTileValue);
    }
}
