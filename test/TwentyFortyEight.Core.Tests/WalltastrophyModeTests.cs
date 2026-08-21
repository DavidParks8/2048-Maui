using NSubstitute;

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

        IRandomSource random = Substitute.For<IRandomSource>();
        random
            .Next(Arg.Any<int>())
            .Returns(
                14, // Spawn tile selection (count = 15)
                1, // Wall orientation = Vertical
                0, // divider
                0, // start
                0 // length => 1 + 0
            );
        random.NextDouble().Returns(0.0);

        Game2048Engine engine = new(
            state,
            config,
            random,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random)
        );

        var moved = engine.Move(Direction.Left);

        Assert.IsTrue(moved);

        // Wall at divider=1 splits row 0 into [0..1] and [2..3]. Tile from col 3 can only move to col 2.
        Assert.AreEqual(2, engine.CurrentState.Board[2]);
        Assert.AreEqual(0, engine.CurrentState.Board[0]);
        random.Received(5).Next(Arg.Any<int>());
        random.Received(1).NextDouble();
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

        IRandomSource random = Substitute.For<IRandomSource>();
        random
            .Next(Arg.Any<int>())
            .Returns(
                0, // Move 1 spawn (count = 15)
                0, // Move 1 wall orientation = Horizontal
                1, // divider
                0, // start
                3, // length => 1 + 3 = 4
                14, // Move 2 spawn (count = 15)
                1, // Move 2 wall orientation = Vertical
                0, // divider
                2, // start
                1 // length => 1 + 1 = 2
            );
        random.NextDouble().Returns(0.0, 0.0);

        Game2048Engine engine = new(
            state,
            config,
            random,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(random)
        );

        Assert.IsTrue(engine.Move(Direction.Right));
        Assert.AreEqual(expectedWall1, engine.CurrentState.Wall);

        Assert.IsTrue(engine.Move(Direction.Left));
        Assert.AreEqual(expectedWall2, engine.CurrentState.Wall);

        Assert.IsTrue(engine.Undo());
        Assert.AreEqual(expectedWall1, engine.CurrentState.Wall);

        Assert.IsTrue(engine.Undo());
        Assert.IsNull(engine.CurrentState.Wall);
        random.Received(10).Next(Arg.Any<int>());
        random.Received(2).NextDouble();
    }
}
