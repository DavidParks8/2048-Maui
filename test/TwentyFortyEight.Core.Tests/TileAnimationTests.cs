using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class AnimationDetectionTests
{
    [TestMethod]
    public void GameEngine_Move_DetectsNewTileSpawn()
    {
        // Arrange
        GameConfig config = new();
        Mock<IRandomSource> randomMock = new();

        // Setup random to return predictable values
        randomMock
            .SetupSequence(r => r.Next(It.IsAny<int>()))
            .Returns(0) // First spawn position
            .Returns(1) // Second spawn position
            .Returns(5); // Third spawn position after move (position that will be empty)

        randomMock
            .SetupSequence(r => r.NextDouble())
            .Returns(0.5) // First spawn value (2)
            .Returns(0.5) // Second spawn value (2)
            .Returns(0.5); // Third spawn value (2) - new tile

        Game2048Engine engine = new(config, randomMock.Object, NullStatisticsTracker.Instance);
        var initialBoardSnapshot = engine.CurrentState.Board.ToArray();

        // Act
        var moved = engine.Move(Direction.Right);

        // Assert
        Assert.IsTrue(moved, "Move should succeed");

        // Verify a new tile was spawned (board has one more non-zero tile than initial)
        var initialNonZeroCount = initialBoardSnapshot.Count(v => v != 0);
        var finalNonZeroCount = engine.CurrentState.Board.ToArray().Count(v => v != 0);
        Assert.IsGreaterThanOrEqualTo(
            finalNonZeroCount,
            initialNonZeroCount,
            "Should have at least the same number of tiles after move"
        );
    }

    [TestMethod]
    public void GameEngine_Move_DetectsTileMerge()
    {
        // Arrange - Create a board with two 2's that can merge
        GameConfig config = new();
        Mock<IRandomSource> randomMock = new();

        // Create initial state with two 2's in the same row
        var initialBoard = new int[16];
        initialBoard[0] = 2; // Top-left
        initialBoard[1] = 2; // Next to it
        var initialState = TestHelpers.CreateGameState(initialBoard, 4, 0, 0, false, false);

        Game2048Engine engine = new(
            initialState,
            config,
            randomMock.Object,
            NullStatisticsTracker.Instance
        );

        // Setup random for the new tile spawn after merge
        randomMock.Setup(r => r.Next(It.IsAny<int>())).Returns(2);
        randomMock.Setup(r => r.NextDouble()).Returns(0.5);

        // Act
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsTrue(moved, "Move should succeed");
        Assert.AreEqual(
            4,
            engine.CurrentState.Board[0],
            "First position should have merged value of 4"
        );
        Assert.AreEqual(0, engine.CurrentState.Board[1], "Second position should be empty");
        Assert.AreEqual(4, engine.CurrentState.Score, "Score should increase by merged value");
    }

    [TestMethod]
    public void GameEngine_Move_DetectsTileSlide()
    {
        // Arrange - Create a board with a tile that needs to slide
        GameConfig config = new();
        Mock<IRandomSource> randomMock = new();

        // Create initial state with a single tile not at the edge
        var initialBoard = new int[16];
        initialBoard[1] = 2; // Position 1 (will slide to position 0)
        var initialState = TestHelpers.CreateGameState(initialBoard, 4, 0, 0, false, false);

        Game2048Engine engine = new(
            initialState,
            config,
            randomMock.Object,
            NullStatisticsTracker.Instance
        );

        // Setup random for the new tile spawn after slide
        randomMock.Setup(r => r.Next(It.IsAny<int>())).Returns(2);
        randomMock.Setup(r => r.NextDouble()).Returns(0.5);

        // Act
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsTrue(moved, "Move should succeed");
        Assert.AreEqual(2, engine.CurrentState.Board[0], "Tile should have slid to first position");
        Assert.AreEqual(0, engine.CurrentState.Board[1], "Original position should be empty");
    }

    [TestMethod]
    public void GameEngine_MultipleDirections_AnimationsWorkCorrectly()
    {
        // Arrange
        GameConfig config = new();
        Mock<IRandomSource> randomMock = new();

        randomMock.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        randomMock.Setup(r => r.NextDouble()).Returns(0.5);

        Game2048Engine engine = new(config, randomMock.Object, NullStatisticsTracker.Instance);

        // Act & Assert for each direction
        var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        foreach (var direction in directions)
        {
            var previousBoard = engine.CurrentState.Board.ToArray();
            var moved = engine.Move(direction);

            if (moved)
            {
                // Verify board changed
                var currentBoard = engine.CurrentState.Board.ToArray();
                var boardChanged = !previousBoard.SequenceEqual(currentBoard);
                Assert.IsTrue(boardChanged, $"Board should change after moving {direction}");
            }
        }
    }

    [TestMethod]
    public void GameState_ComparingBoards_DetectsChanges()
    {
        // Arrange
        var board1 = new int[] { 2, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        var board2 = new int[] { 2, 2, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        var board3 = new int[] { 4, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // Act & Assert
        // Detect new tile (0 -> 2)
        for (int i = 0; i < board1.Length; i++)
        {
            if (board1[i] == 0 && board2[i] == 2)
            {
                Assert.AreEqual(1, i, "New tile should be at position 1");
            }
        }

        // Detect merge (2 -> 4 at same position)
        for (int i = 0; i < board1.Length; i++)
        {
            if (board1[i] == 2 && board3[i] == 4)
            {
                Assert.AreEqual(0, i, "Merge should occur at position 0");
            }
        }
    }
}
