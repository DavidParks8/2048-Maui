using NSubstitute;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class ClassicSpawnTests
{
    [TestMethod]
    public void Move_ClassicMode_WithHighTiles_SpawnsTwoAsCommonValue()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Classic };

        IRandomSource mockRandom = Substitute.For<IRandomSource>();
        mockRandom.NextDouble().Returns(0.5); // common value
        mockRandom.Next(Arg.Any<int>()).Returns(0); // first empty cell

        // Create board with high max tile; classic spawn should still be 2/4.
        var data = new int[16];
        data[0] = 8;
        data[4] = 2048;
        var state = TestHelpers.CreateGameState(data, 4, 0, 0, false, false);

        Game2048Engine engine = new(
            state,
            config,
            mockRandom,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(mockRandom)
        );

        // Act
        engine.Move(Direction.Right);

        // Assert
        var newState = engine.CurrentState;
        Assert.IsTrue(
            newState.Board.ToArray().Contains(2),
            "Classic mode should spawn a 2 tile as the common value."
        );
    }

    [TestMethod]
    public void Move_ClassicMode_WithHighTiles_SpawnsFourAsRareValue()
    {
        // Arrange
        GameConfig config = new() { Size = 4, Mode = GameMode.Classic };

        IRandomSource mockRandom = Substitute.For<IRandomSource>();
        mockRandom.NextDouble().Returns(0.95); // rare value
        mockRandom.Next(Arg.Any<int>()).Returns(0); // first empty cell

        var data = new int[16];
        data[0] = 8;
        data[4] = 2048;
        var state = TestHelpers.CreateGameState(data, 4, 0, 0, false, false);

        Game2048Engine engine = new(
            state,
            config,
            mockRandom,
            NullStatisticsTracker.Instance,
            new BoardMoveSimulator(),
            TestHelpers.CreateSpawnStrategyFactory(mockRandom)
        );

        // Act
        engine.Move(Direction.Right);

        // Assert
        var newState = engine.CurrentState;
        Assert.IsTrue(
            newState.Board.ToArray().Contains(4),
            "Classic mode should spawn a 4 tile as the rare value."
        );
    }
}
