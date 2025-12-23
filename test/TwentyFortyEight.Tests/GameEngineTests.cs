using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Tests;

[TestClass]
public class GameEngineTests
{
    [TestMethod]
    public void NewGame_CreatesEmptyBoardWithTwoTiles()
    {
        // Arrange & Act
        var engine = new Game2048Engine(new GameConfig(), new SystemRandomSource());

        // Assert
        var state = engine.CurrentState;
        var nonZeroCount = state.Board.Length - state.Board.CountEmptyCells();
        Assert.AreEqual(2, nonZeroCount, "New game should start with exactly 2 tiles");
        Assert.AreEqual(0, state.Score, "Score should start at 0");
        Assert.AreEqual(0, state.MoveCount, "Move count should start at 0");
        Assert.IsFalse(state.IsWon, "Game should not be won at start");
        Assert.IsFalse(state.IsGameOver, "Game should not be over at start");
    }

    [TestMethod]
    public void MoveLeft_CompressesTiles()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var engine = new Game2048Engine(config, random);

        // Create a specific board state
        var board = new int[16];
        board[0] = 2;
        board[1] = 0;
        board[2] = 2;
        board[3] = 0; // [2,0,2,0] -> [4,0,0,0]
        var state = new GameState(board, 4, 0, 0, false, false);
        engine = new Game2048Engine(state, config, random);

        // Act
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsTrue(moved, "Move should succeed");
        Assert.AreEqual(4, engine.CurrentState.Board[0], "First tile should be merged to 4");
        Assert.AreEqual(4, engine.CurrentState.Score, "Score should increase by merged value");
    }

    [TestMethod]
    public void MoveLeft_MergesMultiplePairs()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var board = new int[16];
        board[0] = 2;
        board[1] = 2;
        board[2] = 2;
        board[3] = 2; // [2,2,2,2] -> [4,4,0,0]
        var state = new GameState(board, 4, 0, 0, false, false);
        var engine = new Game2048Engine(state, config, random);

        // Act
        engine.Move(Direction.Left);

        // Assert
        Assert.AreEqual(4, engine.CurrentState.Board[0]);
        Assert.AreEqual(4, engine.CurrentState.Board[1]);
        Assert.AreEqual(0, engine.CurrentState.Board[2]);
        Assert.AreEqual(8, engine.CurrentState.Score); // 4 + 4
    }

    [TestMethod]
    public void MoveLeft_OneMergePerTilePerMove()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var board = new int[16];
        board[0] = 2;
        board[1] = 2;
        board[2] = 2;
        board[3] = 0; // [2,2,2,0] -> [4,2,0,0]
        var state = new GameState(board, 4, 0, 0, false, false);
        var engine = new Game2048Engine(state, config, random);

        // Act
        engine.Move(Direction.Left);

        // Assert
        Assert.AreEqual(4, engine.CurrentState.Board[0]);
        Assert.AreEqual(2, engine.CurrentState.Board[1]);
        Assert.AreEqual(0, engine.CurrentState.Board[2]);
        Assert.AreEqual(0, engine.CurrentState.Board[3]);
        Assert.AreEqual(4, engine.CurrentState.Score);
    }

    [TestMethod]
    public void MoveRight_CompressesTiles()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var board = new int[16];
        board[0] = 0;
        board[1] = 2;
        board[2] = 0;
        board[3] = 2; // [0,2,0,2] -> [0,0,0,4]
        var state = new GameState(board, 4, 0, 0, false, false);
        var engine = new Game2048Engine(state, config, random);

        // Act
        engine.Move(Direction.Right);

        // Assert
        Assert.AreEqual(0, engine.CurrentState.Board[0]);
        Assert.AreEqual(0, engine.CurrentState.Board[1]);
        Assert.AreEqual(0, engine.CurrentState.Board[2]);
        Assert.AreEqual(4, engine.CurrentState.Board[3]);
        Assert.AreEqual(4, engine.CurrentState.Score);
    }

    [TestMethod]
    public void MoveUp_CompressesTiles()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var board = new int[16];
        board[0] = 2; // Row 0
        board[4] = 0; // Row 1
        board[8] = 2; // Row 2
        board[12] = 0; // Row 3
        // Column 0: [2,0,2,0] -> [4,0,0,0]
        var state = new GameState(board, 4, 0, 0, false, false);
        var engine = new Game2048Engine(state, config, random);

        // Act
        engine.Move(Direction.Up);

        // Assert
        Assert.AreEqual(4, engine.CurrentState.Board[0]);
        Assert.AreEqual(0, engine.CurrentState.Board[4]);
        Assert.AreEqual(0, engine.CurrentState.Board[8]);
        Assert.AreEqual(0, engine.CurrentState.Board[12]);
        Assert.AreEqual(4, engine.CurrentState.Score);
    }

    [TestMethod]
    public void MoveDown_CompressesTiles()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var board = new int[16];
        board[0] = 2; // Row 0
        board[4] = 0; // Row 1
        board[8] = 2; // Row 2
        board[12] = 0; // Row 3
        // Column 0: [2,0,2,0] -> [0,0,0,4]
        var state = new GameState(board, 4, 0, 0, false, false);
        var engine = new Game2048Engine(state, config, random);

        // Act
        engine.Move(Direction.Down);

        // Assert
        Assert.AreEqual(0, engine.CurrentState.Board[0]);
        Assert.AreEqual(0, engine.CurrentState.Board[4]);
        Assert.AreEqual(0, engine.CurrentState.Board[8]);
        Assert.AreEqual(4, engine.CurrentState.Board[12]);
        Assert.AreEqual(4, engine.CurrentState.Score);
    }

    [TestMethod]
    public void NoOpMove_DoesNotSpawnTile()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var board = new int[16];
        board[0] = 2;
        board[1] = 4;
        board[2] = 8;
        board[3] = 16;
        // All tiles already at the left
        var state = new GameState(board, 4, 10, 5, false, false);
        var engine = new Game2048Engine(state, config, random);

        // Act
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsFalse(moved, "Move should be a no-op");
        Assert.AreEqual(10, engine.CurrentState.Score, "Score should not change");
        Assert.AreEqual(5, engine.CurrentState.MoveCount, "Move count should not change");

        // Count non-zero tiles - should remain the same (no new tile spawned)
        var nonZeroCount =
            engine.CurrentState.Board.Length - engine.CurrentState.Board.CountEmptyCells();
        Assert.AreEqual(4, nonZeroCount, "No new tile should spawn on no-op move");
    }

    [TestMethod]
    public void SpawnTile_Uses90Percent2And10Percent4()
    {
        // This test verifies the spawn distribution using a seeded RNG
        var config = new GameConfig { Size = 4 };
        var spawnedValues = new List<int>();

        // Run multiple games to check spawn distribution
        for (int i = 0; i < 100; i++)
        {
            var random = new SystemRandomSource(i);
            var board = new int[16];
            board[0] = 2;
            board[1] = 0;
            board[2] = 2;
            board[3] = 0;
            var state = new GameState(board, 4, 0, 0, false, false);
            var engine = new Game2048Engine(state, config, random);

            // Get initial tiles count
            var initialCount = state.Board.Length - state.Board.CountEmptyCells();

            // Make a move to trigger spawn
            engine.Move(Direction.Left);

            // Find the newly spawned tile
            for (int j = 0; j < engine.CurrentState.Board.Length; j++)
            {
                if (state.Board[j] == 0 && engine.CurrentState.Board[j] != 0)
                {
                    spawnedValues.Add(engine.CurrentState.Board[j]);
                    break;
                }
            }
        }

        // Verify all spawned values are either 2 or 4
        Assert.IsTrue(
            spawnedValues.All(v => v == 2 || v == 4),
            "All spawned values should be 2 or 4"
        );

        // Most should be 2s (roughly 90%)
        var twoCount = spawnedValues.Count(v => v == 2);
        Assert.IsGreaterThanOrEqualTo(70, twoCount, $"Expected at least 70 2s, got {twoCount}");
    }

    [TestMethod]
    public void WinDetection_TriggersWhenReachingWinTile()
    {
        // Arrange
        var config = new GameConfig { Size = 4, WinTile = 2048 };
        var random = new SystemRandomSource(42);
        var board = new int[16];
        board[0] = 1024;
        board[1] = 1024;
        var state = new GameState(board, 4, 0, 0, false, false);
        var engine = new Game2048Engine(state, config, random);

        // Act
        engine.Move(Direction.Left);

        // Assert
        Assert.IsTrue(engine.CurrentState.IsWon, "Game should be won when reaching 2048");
    }

    [TestMethod]
    public void GameOver_DetectedWhenNoMovesAvailable()
    {
        // Arrange - Create a full board with no possible merges
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var board = new int[16] { 2, 4, 8, 16, 16, 8, 4, 2, 2, 4, 8, 16, 16, 8, 4, 2 };
        var state = new GameState(board, 4, 0, 0, false, false);
        var engine = new Game2048Engine(state, config, random);

        // The current state should already be game over since the board is full
        // and no merges are possible

        // Act - Try to make a move (this triggers game over detection)
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsFalse(moved, "No move should be possible");
        Assert.IsTrue(
            engine.CurrentState.IsGameOver,
            "Game should be over when no moves are possible"
        );
    }

    [TestMethod]
    public void Undo_RevertsLastMove()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var engine = new Game2048Engine(config, random);
        var initialBoard = engine.CurrentState.Board.ToArray();
        var initialScore = engine.CurrentState.Score;

        // Act
        engine.Move(Direction.Left);
        var boardAfterMove = engine.CurrentState.Board.ToArray();
        var scoreAfterMove = engine.CurrentState.Score;

        var undone = engine.Undo();

        // Assert
        Assert.IsTrue(undone, "Undo should succeed");
        CollectionAssert.AreEqual(
            initialBoard,
            engine.CurrentState.Board.ToArray(),
            "Board should be restored"
        );
        Assert.AreEqual(initialScore, engine.CurrentState.Score, "Score should be restored");
        Assert.IsFalse(engine.CanUndo, "Should not be able to undo again");
    }

    [TestMethod]
    public void Undo_BoundedCapacity()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };
        var random = new SystemRandomSource(42);
        var board = new int[16];
        board[0] = 2;
        board[1] = 0;
        board[2] = 0;
        board[3] = 0;
        board[4] = 0;
        board[5] = 2;
        board[6] = 0;
        board[7] = 0;
        var state = new GameState(board, 4, 0, 0, false, false);
        var engine = new Game2048Engine(state, config, random);

        // Act - Make more than 50 moves
        for (int i = 0; i < 60; i++)
        {
            if (i % 2 == 0)
                engine.Move(Direction.Left);
            else
                engine.Move(Direction.Right);
        }

        // Count how many undos we can do
        int undoCount = 0;
        while (engine.CanUndo && undoCount < 100) // Add safety limit
        {
            engine.Undo();
            undoCount++;
        }

        // Assert
        Assert.IsLessThanOrEqualTo(
            50,
            undoCount,
            $"Should be able to undo at most 50 moves, but got {undoCount}"
        );
    }
}
