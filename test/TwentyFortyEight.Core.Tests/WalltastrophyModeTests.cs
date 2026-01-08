using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class WalltastrophyModeTests
{
    [TestMethod]
    public void MoveLeft_WithVerticalWall_BlocksCrossingDivider()
    {
        GameConfig config = new() { Size = 4, Mode = GameMode.Walltastrophy };

        var board = new int[16];
        board[3] = 2; // (0,3)

        var initialWall = new WallSegment(
            WallOrientation.Vertical,
            divider: 1,
            start: 0,
            length: 1
        );
        var state = TestHelpers.CreateGameState(board, 4).WithWall(initialWall);

        var random = new Mock<IRandomSource>(MockBehavior.Strict);
        random
            .SetupSequence(r => r.Next(It.IsAny<int>()))
            // Spawn tile selection (count = 15)
            .Returns(14)
            // Wall placement after move
            .Returns(1) // orientation = Vertical
            .Returns(0) // divider
            .Returns(0) // start
            .Returns(0); // length => 1 + 0
        random.SetupSequence(r => r.NextDouble()).Returns(0.0);

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        var moved = engine.Move(Direction.Left);

        Assert.IsTrue(moved);

        // Wall at divider=1 splits row 0 into [0..1] and [2..3]. Tile from col 3 can only move to col 2.
        Assert.AreEqual(2, engine.CurrentState.Board[2]);
        Assert.AreEqual(0, engine.CurrentState.Board[0]);
    }

    [TestMethod]
    public void Undo_Walltastrophy_RestoresWallsExactly()
    {
        GameConfig config = new() { Size = 4, Mode = GameMode.Walltastrophy };

        var board = new int[16];
        board[0] = 2; // (0,0)
        var state = TestHelpers.CreateGameState(board, 4);

        var expectedWall1 = new WallSegment(
            WallOrientation.Horizontal,
            divider: 1,
            start: 0,
            length: 4
        );
        var expectedWall2 = new WallSegment(
            WallOrientation.Vertical,
            divider: 0,
            start: 2,
            length: 2
        );

        var random = new Mock<IRandomSource>(MockBehavior.Strict);
        random
            .SetupSequence(r => r.Next(It.IsAny<int>()))
            // Move 1 spawn (count = 15)
            .Returns(0)
            // Move 1 wall
            .Returns(0) // orientation = Horizontal
            .Returns(1) // divider
            .Returns(0) // start
            .Returns(3) // length => 1 + 3 = 4
            // Move 2 spawn (count = 15)
            .Returns(14)
            // Move 2 wall
            .Returns(1) // orientation = Vertical
            .Returns(0) // divider
            .Returns(2) // start
            .Returns(1); // length => 1 + 1 = 2
        random.SetupSequence(r => r.NextDouble()).Returns(0.0).Returns(0.0);

        Game2048Engine engine = new(
            state,
            config,
            random.Object,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random.Object)
        );

        Assert.IsTrue(engine.Move(Direction.Right));
        Assert.AreEqual(expectedWall1, engine.CurrentState.Wall);

        Assert.IsTrue(engine.Move(Direction.Left));
        Assert.AreEqual(expectedWall2, engine.CurrentState.Wall);

        Assert.IsTrue(engine.Undo());
        Assert.AreEqual(expectedWall1, engine.CurrentState.Wall);

        Assert.IsTrue(engine.Undo());
        Assert.IsNull(engine.CurrentState.Wall);
    }
}
