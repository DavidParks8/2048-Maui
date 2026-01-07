using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class WalltastrophyModeTests
{
    [TestMethod]
    public void WalltastrophyMode_AddsWallAfterSuccessfulMove()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Walltastrophy };
        SystemRandomSource random = new(42);

        // Create a board with tiles that can merge
        var board = new int[16];
        board[0] = 2;
        board[1] = 2; // [2,2,0,0] in first row
        var state = TestHelpers.CreateGameState(board, 4, 0, 0, false, false);

        Game2048Engine engine = new(
            state,
            config,
            random,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator()
        );

        // Act
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsTrue(moved, "Move should succeed");
        Assert.IsNotNull(engine.CurrentState.Walls, "Walls should be initialized");
#pragma warning disable MSTEST0037 // Use CollectionAssert.That.HasCount instead of AreEqual
        Assert.AreEqual(1, engine.CurrentState.Walls.Count, "One wall should be added after move");
#pragma warning restore MSTEST0037
    }

    [TestMethod]
    public void ClassicMode_DoesNotAddWalls()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Classic };
        SystemRandomSource random = new(42);

        // Create a board with tiles that can merge
        var board = new int[16];
        board[0] = 2;
        board[1] = 2; // [2,2,0,0] in first row
        var state = TestHelpers.CreateGameState(board, 4, 0, 0, false, false);

        Game2048Engine engine = new(
            state,
            config,
            random,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator()
        );

        // Act
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsTrue(moved, "Move should succeed");
        var wallCount = engine.CurrentState.Walls?.Count ?? 0;
        Assert.AreEqual(0, wallCount, "No walls should be added in Classic mode");
    }

    [TestMethod]
    public void WalltastrophyMode_UndoRemovesWall()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Walltastrophy };
        SystemRandomSource random = new(42);

        // Create a board with tiles that can merge
        var board = new int[16];
        board[0] = 2;
        board[1] = 2;
        var state = TestHelpers.CreateGameState(board, 4, 0, 0, false, false);

        Game2048Engine engine = new(
            state,
            config,
            random,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator()
        );

        // Act
        engine.Move(Direction.Left);
        var wallCountAfterMove = engine.CurrentState.Walls?.Count ?? 0;

        engine.Undo();
        var wallCountAfterUndo = engine.CurrentState.Walls?.Count ?? 0;

        // Assert
        Assert.AreEqual(1, wallCountAfterMove, "One wall should exist after move");
        Assert.AreEqual(0, wallCountAfterUndo, "No walls should exist after undo");
    }

    [TestMethod]
    public void WallSegment_BlocksMovement_Horizontal()
    {
        // Arrange
        var wall = new WallSegment(Row: 0, Col: 1, WallOrientation.Horizontal);
        var from = new Position(0, 1);
        var to = new Position(1, 1);

        // Act
        var blocks = wall.BlocksMovement(from, to);

        // Assert
        Assert.IsTrue(blocks, "Horizontal wall should block vertical movement");
    }

    [TestMethod]
    public void WallSegment_BlocksMovement_Vertical()
    {
        // Arrange
        var wall = new WallSegment(Row: 1, Col: 0, WallOrientation.Vertical);
        var from = new Position(1, 0);
        var to = new Position(1, 1);

        // Act
        var blocks = wall.BlocksMovement(from, to);

        // Assert
        Assert.IsTrue(blocks, "Vertical wall should block horizontal movement");
    }

    [TestMethod]
    public void WallSegment_DoesNotBlock_WrongOrientation()
    {
        // Arrange
        var horizontalWall = new WallSegment(Row: 0, Col: 1, WallOrientation.Horizontal);
        var from = new Position(0, 0);
        var to = new Position(0, 1);

        // Act
        var blocks = horizontalWall.BlocksMovement(from, to);

        // Assert
        Assert.IsFalse(blocks, "Horizontal wall should not block horizontal movement");
    }

    [TestMethod]
    public void GameConfig_RulesetId_IncludesMode()
    {
        // Arrange & Act
        var classicConfig = new GameConfig { Mode = GameMode.Classic };
        var walltastrophyConfig = new GameConfig { Mode = GameMode.Walltastrophy };

        // Assert
        Assert.AreEqual(
            string.Empty,
            classicConfig.RulesetId,
            "Classic mode with default settings should have empty RulesetId"
        );
        Assert.AreEqual(
            "mode=Walltastrophy",
            walltastrophyConfig.RulesetId,
            "Walltastrophy mode should be in RulesetId"
        );
    }

    [TestMethod]
    public void WalltastrophyMode_NoOpMove_DoesNotAddWall()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Walltastrophy };
        SystemRandomSource random = new(42);

        // Create a board where right move is a no-op
        // All tiles already at the right edge
        var board = new int[16];
        board[3] = 2; // row 0, col 3 (rightmost)
        board[7] = 2; // row 1, col 3 (rightmost)
        var state = TestHelpers.CreateGameState(board, 4, 0, 0, false, false);

        Game2048Engine engine = new(
            state,
            config,
            random,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator()
        );

        // Act - try to move right (should be no-op)
        var moved = engine.Move(Direction.Right);
        var wallCount = engine.CurrentState.Walls?.Count ?? 0;

        // Assert
        Assert.IsFalse(moved, "Move should not succeed (no-op)");
        Assert.AreEqual(0, wallCount, "No wall should be added for unsuccessful move");
    }
}
