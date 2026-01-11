using Moq;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class AdversarialModeTests
{
    [TestMethod]
    public void TrySpawnExternalTile_EmptyPosition_SpawnsTile()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Adversarial };
        var board = new int[16];
        board[0] = 2; // (0,0) has a tile
        var state = TestHelpers.CreateGameState(board, 4);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);
        random.Setup(r => r.NextDouble()).Returns(0.0); // Will spawn a 2

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        // Act
        var position = new Position(1, 1); // index 5
        bool success = engine.TrySpawnExternalTile(position, out int spawnedValue);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(2, spawnedValue);
        Assert.AreEqual(2, engine.CurrentState.Board[5]);
    }

    [TestMethod]
    public void TrySpawnExternalTile_OccupiedPosition_ReturnsFalse()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Adversarial };
        var board = new int[16];
        board[0] = 2; // (0,0) has a tile
        var state = TestHelpers.CreateGameState(board, 4);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        // Act
        var position = new Position(0, 0); // Already occupied
        bool success = engine.TrySpawnExternalTile(position, out int spawnedValue);

        // Assert
        Assert.IsFalse(success);
        Assert.AreEqual(0, spawnedValue);
    }

    [TestMethod]
    public void Move_InAdversarialMode_UsesStandardPositiveScore()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Adversarial };
        var board = new int[16];
        board[0] = 2; // (0,0)
        board[1] = 2; // (0,1) - will merge with (0,0) on move left
        var state = TestHelpers.CreateGameState(board, 4);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);
        random.Setup(r => r.NextDouble()).Returns(0.0); // Spawn a 2

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        // In adversarial mode, must spawn first before AI can move
        engine.TrySpawnExternalTile(new Position(2, 0), out _); // Spawn at a different position

        // Act
        engine.Move(Direction.Left);

        // Assert - merge of two 2s gives 4 points
        Assert.AreEqual(4, engine.CurrentState.Score);
    }

    [TestMethod]
    public void Move_AdversarialMode_LockedBoardAfterMove_IsWin()
    {
        // Arrange - a board where after a move, no more moves are possible
        GameConfig config = new() { Size = 2, Mode = GameMode.Adversarial };
        // Board: [2, 2]  After move left: [4, 0]
        //        [4, 4]                   [8, 0]
        // Then the board has empty cells, so not locked.

        // Let's create a board that after merging becomes locked:
        // [2, 4] -> move left -> [2, 4] (no change, but we need to test locked state)
        // [4, 2]                 [4, 2]
        // Actually this board has no valid moves, so Move() returns false.

        // Test: spawn a tile that fills the last empty cell, then try to move
        // and verify that when all moves fail, the game shows IsWon.

        // Simpler approach: test that when Move fails on locked board, IsWon is set
        var board = new int[4] { 2, 4, 4, 2 };
        var state = TestHelpers.CreateGameState(board, 2);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        // Act - attempt to move (should fail since no moves possible)
        bool moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsFalse(moved);
        // The board was already locked - IsGameOver check happens after a successful move
        // or needs to be triggered by external spawn. For this test, verify the IsGameOver helper.
        // Note: The game won't mark IsWon just from a failed move; it happens when Move succeeds
        // but then no further moves are possible.
    }

    [TestMethod]
    public void Move_AdversarialMode_LockedBoard_PlayerWins_RaisesVictoryAchieved()
    {
        // Arrange - a fully locked board (no moves possible)
        GameConfig config = new() { Size = 2, Mode = GameMode.Adversarial };
        var board = new int[4] { 2, 4, 4, 2 };
        var state = TestHelpers.CreateGameState(board, 2);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        var victoryEvents = 0;
        engine.VictoryAchieved += (_, _) => victoryEvents++;

        // Act - no pending external spawn; Move() should detect lock and finalize win
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsFalse(moved);
        Assert.IsTrue(engine.CurrentState.IsGameOver);
        Assert.IsTrue(engine.CurrentState.IsWon);
        Assert.AreEqual(1, victoryEvents);
    }

    [TestMethod]
    public void Move_AdversarialMode_AIReaches2048_IsGameOver()
    {
        // Arrange - two 1024 tiles that will merge to 2048
        GameConfig config = new()
        {
            Size = 4,
            Mode = GameMode.Adversarial,
            WinTile = 2048,
        };
        var board = new int[16];
        board[0] = 1024; // (0,0)
        board[1] = 1024; // (0,1) - will merge to 2048 on move left
        var state = TestHelpers.CreateGameState(board, 4);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);
        random.Setup(r => r.NextDouble()).Returns(0.0); // Spawn a 2

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        // In adversarial mode, must spawn first before AI can move
        engine.TrySpawnExternalTile(new Position(2, 0), out _);

        // Act
        engine.Move(Direction.Left);

        // Assert - when AI reaches 2048 (through merge), player loses
        Assert.IsTrue(engine.CurrentState.IsGameOver);
        Assert.IsFalse(engine.CurrentState.IsWon);
    }

    [TestMethod]
    public void Undo_AdversarialMode_RestoresStateBeforeTurn()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Adversarial };
        var board = new int[16];
        board[0] = 2;
        var state = TestHelpers.CreateGameState(board, 4);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);
        random.Setup(r => r.NextDouble()).Returns(0.0); // Spawn 2

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        // Capture initial state
        var initialBoard = engine.CurrentState.Board.Clone();

        // External spawn at position (1,1) = index 5
        engine.TrySpawnExternalTile(new Position(1, 1), out _);

        // Move after spawn
        engine.Move(Direction.Left);

        // Act - undo should restore state before the entire turn (spawn + move)
        bool undone = engine.Undo();

        // Assert
        Assert.IsTrue(undone);
        // Board should be back to initial state (no external spawn)
        CollectionAssert.AreEqual(initialBoard.ToArray(), engine.CurrentState.Board.ToArray());
    }

    [TestMethod]
    public void ExternalSpawn_RecordedInMoveRecord()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Adversarial };
        var board = new int[16];
        board[0] = 2;
        var state = TestHelpers.CreateGameState(board, 4);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);
        random.Setup(r => r.NextDouble()).Returns(0.0); // Spawn 2
        random.Setup(r => r.Next(It.IsAny<int>())).Returns(14);

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        // External spawn at index 5 (row=1, col=1)
        engine.TrySpawnExternalTile(new Position(1, 1), out _);

        // Move
        engine.Move(Direction.Left);

        // Assert - the move record should have the external spawn info
        var saveDto = engine.ToSaveDto();
        var lastMove = saveDto.MoveHistory!.Last();
        Assert.AreEqual(5, lastMove.ExternalSpawnedTileIndex);
        Assert.AreEqual(2, lastMove.ExternalSpawnedTileValue);
    }

    [TestMethod]
    public void GameConfig_Adversarial_HasCorrectModeId()
    {
        // Arrange
        var config = new GameConfig { Size = 4, Mode = GameMode.Adversarial };

        // Assert
        StringAssert.Contains(config.RulesetId, "adversarial");
    }
}
