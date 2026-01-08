using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class GameSaveTests
{
    private static void MakeAnyMove(Game2048Engine engine)
    {
        foreach (
            var direction in new[] { Direction.Left, Direction.Right, Direction.Up, Direction.Down }
        )
        {
            if (engine.Move(direction))
            {
                return;
            }
        }

        Assert.Fail("Expected at least one direction to produce a valid move.");
    }

    [TestMethod]
    public void GameSave_RoundTripsEngine_WithUndoHistory()
    {
        // Arrange
        var config = new GameConfig
        {
            Size = 4,
            WinTile = 2048,
            Mode = GameMode.Modern,
        };
        var random = new SystemRandomSource(42);
        var stats = NullStatisticsTracker.Instance;
        var simulator = new BoardMoveSimulator();

        var engine = TestHelpers.CreateEngine(config, random, stats, simulator);

        // Make a couple moves.
        MakeAnyMove(engine);
        MakeAnyMove(engine);

        // Undo one move so we have history beyond the cursor.
        Assert.IsTrue(engine.Undo());

        var savedState = engine.CurrentState;
        var save = engine.ToSaveDto();

        // Act
        var restored = TestHelpers.CreateEngine(save, config, random, stats, simulator);

        // Assert
        CollectionAssert.AreEqual(
            savedState.Board.ToArray(),
            restored.CurrentState.Board.ToArray()
        );
        Assert.AreEqual(savedState.Score, restored.CurrentState.Score);
        Assert.AreEqual(savedState.MoveCount, restored.CurrentState.MoveCount);
        Assert.AreEqual(savedState.IsWon, restored.CurrentState.IsWon);
        Assert.AreEqual(savedState.IsGameOver, restored.CurrentState.IsGameOver);

        // Undo should still work after restoring.
        Assert.AreEqual(engine.CanUndo, restored.CanUndo);
        if (restored.CanUndo)
        {
            Assert.IsTrue(restored.Undo());
        }
    }
}
