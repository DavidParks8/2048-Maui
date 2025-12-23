using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Tests;

[TestClass]
public class GameStateSerializationTests
{
    [TestMethod]
    public void GameStateDto_SerializesAndDeserializes()
    {
        // Arrange
        var board = new int[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 0, 0, 0, 0, 0 };
        var state = new GameState(board, 4, 5000, 42, true, false);

        // Act
        var dto = GameStateDto.FromGameState(state);
        var restored = dto.ToGameState();

        // Assert
        CollectionAssert.AreEqual(state.Board.ToArray(), restored.Board.ToArray());
        Assert.AreEqual(state.Size, restored.Size);
        Assert.AreEqual(state.Score, restored.Score);
        Assert.AreEqual(state.MoveCount, restored.MoveCount);
        Assert.AreEqual(state.IsWon, restored.IsWon);
        Assert.AreEqual(state.IsGameOver, restored.IsGameOver);
    }

    [TestMethod]
    public void GameStateDto_BoardIsCloned()
    {
        // Arrange
        var board = new int[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 0, 0, 0, 0, 0 };
        var state = new GameState(board, 4, 5000, 42, true, false);

        // Act
        var dto = GameStateDto.FromGameState(state);
        dto.Board[0] = 999; // Modify DTO board

        // Assert
        Assert.AreEqual(2, state.Board[0], "Original state board should not be modified");
    }

    [TestMethod]
    public void GameState_WithTile_CreatesNewState()
    {
        // Arrange
        var state = new GameState(4);

        // Act
        var newState = state.WithTile(0, 0, 2);

        // Assert
        Assert.AreEqual(0, state.GetTile(0, 0), "Original state should not be modified");
        Assert.AreEqual(2, newState.GetTile(0, 0), "New state should have the updated tile");
    }

    [TestMethod]
    public void GameState_WithUpdate_CreatesNewState()
    {
        // Arrange
        var state = new GameState(4);

        // Act
        var newState = state.WithUpdate(score: 100, moveCount: 5, isWon: true);

        // Assert
        Assert.AreEqual(0, state.Score, "Original state should not be modified");
        Assert.AreEqual(100, newState.Score, "New state should have updated score");
        Assert.AreEqual(5, newState.MoveCount, "New state should have updated move count");
        Assert.IsTrue(newState.IsWon, "New state should be marked as won");
    }
}
