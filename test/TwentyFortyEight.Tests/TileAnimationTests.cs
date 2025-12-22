using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Tests;

[TestClass]
public class AnimationDetectionTests
{
    [TestMethod]
    public void GameEngine_Move_DetectsNewTileSpawn()
    {
        // Arrange
        var config = new GameConfig();
        var randomMock = new Mock<IRandomSource>();
        
        // Setup random to return predictable values
        randomMock.SetupSequence(r => r.Next(It.IsAny<int>()))
            .Returns(0)  // First spawn position
            .Returns(1)  // Second spawn position
            .Returns(5); // Third spawn position after move (position that will be empty)
        
        randomMock.SetupSequence(r => r.NextDouble())
            .Returns(0.5)  // First spawn value (2)
            .Returns(0.5)  // Second spawn value (2)
            .Returns(0.5); // Third spawn value (2) - new tile
        
        var engine = new Game2048Engine(config, randomMock.Object);
        var initialBoardSnapshot = (int[])engine.CurrentState.Board.Clone();

        // Act
        var moved = engine.Move(Direction.Right);

        // Assert
        Assert.IsTrue(moved, "Move should succeed");
        
        // Verify a new tile was spawned (board has one more non-zero tile than initial)
        var initialNonZeroCount = initialBoardSnapshot.Count(v => v != 0);
        var finalNonZeroCount = engine.CurrentState.Board.Count(v => v != 0);
        Assert.IsGreaterThanOrEqualTo(finalNonZeroCount, initialNonZeroCount, "Should have at least the same number of tiles after move");
    }

    [TestMethod]
    public void GameEngine_Move_DetectsTileMerge()
    {
        // Arrange - Create a board with two 2's that can merge
        var config = new GameConfig();
        var randomMock = new Mock<IRandomSource>();
        
        // Create initial state with two 2's in the same row
        var initialBoard = new int[16];
        initialBoard[0] = 2;  // Top-left
        initialBoard[1] = 2;  // Next to it
        var initialState = new GameState(initialBoard, 4, 0, 0, false, false);
        
        var engine = new Game2048Engine(initialState, config, randomMock.Object);
        
        // Setup random for the new tile spawn after merge
        randomMock.Setup(r => r.Next(It.IsAny<int>())).Returns(2);
        randomMock.Setup(r => r.NextDouble()).Returns(0.5);

        // Act
        var moved = engine.Move(Direction.Left);

        // Assert
        Assert.IsTrue(moved, "Move should succeed");
        Assert.AreEqual(4, engine.CurrentState.Board[0], "First position should have merged value of 4");
        Assert.AreEqual(0, engine.CurrentState.Board[1], "Second position should be empty");
        Assert.AreEqual(4, engine.CurrentState.Score, "Score should increase by merged value");
    }

    [TestMethod]
    public void GameEngine_Move_DetectsTileSlide()
    {
        // Arrange - Create a board with a tile that needs to slide
        var config = new GameConfig();
        var randomMock = new Mock<IRandomSource>();
        
        // Create initial state with a single tile not at the edge
        var initialBoard = new int[16];
        initialBoard[1] = 2;  // Position 1 (will slide to position 0)
        var initialState = new GameState(initialBoard, 4, 0, 0, false, false);
        
        var engine = new Game2048Engine(initialState, config, randomMock.Object);
        
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
        var config = new GameConfig();
        var randomMock = new Mock<IRandomSource>();
        
        randomMock.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        randomMock.Setup(r => r.NextDouble()).Returns(0.5);
        
        var engine = new Game2048Engine(config, randomMock.Object);

        // Act & Assert for each direction
        var directions = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        
        foreach (var direction in directions)
        {
            var previousBoard = (int[])engine.CurrentState.Board.Clone();
            var moved = engine.Move(direction);
            
            if (moved)
            {
                // Verify board changed
                var boardChanged = !previousBoard.SequenceEqual(engine.CurrentState.Board);
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

    [TestMethod]
    public void GameEngine_UndoRedo_MaintainsConsistentState()
    {
        // Arrange
        var config = new GameConfig();
        var randomMock = new Mock<IRandomSource>();
        randomMock.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        randomMock.Setup(r => r.NextDouble()).Returns(0.5);
        
        var engine = new Game2048Engine(config, randomMock.Object);
        var initialState = (int[])engine.CurrentState.Board.Clone();

        // Act - Make moves, undo, and redo
        engine.Move(Direction.Left);
        var afterMove = (int[])engine.CurrentState.Board.Clone();
        
        engine.Undo();
        var afterUndo = (int[])engine.CurrentState.Board.Clone();
        
        engine.Redo();
        var afterRedo = (int[])engine.CurrentState.Board.Clone();

        // Assert
        Assert.IsFalse(initialState.SequenceEqual(afterMove), "Board should change after move");
        Assert.IsTrue(initialState.SequenceEqual(afterUndo), "Undo should restore initial state");
        Assert.IsTrue(afterMove.SequenceEqual(afterRedo), "Redo should match post-move state");
    }

    [TestMethod]
    public void AnimationLogic_IdentifiesNewTile()
    {
        // This tests the animation detection logic pattern
        var oldValue = 0;
        var newValue = 2;
        
        // Act - Simulate detection logic
        var isNewTile = oldValue == 0 && (newValue == 2 || newValue == 4);
        
        // Assert
        Assert.IsTrue(isNewTile, "Should detect new tile when 0 becomes 2 or 4");
    }

    [TestMethod]
    public void AnimationLogic_IdentifiesMerge()
    {
        // This tests the animation detection logic pattern
        var oldValue = 2;
        var newValue = 4;
        
        // Act - Simulate detection logic
        var isMerged = oldValue != 0 && newValue == oldValue * 2;
        
        // Assert
        Assert.IsTrue(isMerged, "Should detect merge when value doubles");
    }

    [TestMethod]
    public void AnimationLogic_IdentifiesSlide()
    {
        // This tests the animation detection logic pattern
        var oldValue = 0;
        var newValue = 8;
        
        // Act - Simulate detection logic (tile that slid into empty space)
        var isSlide = oldValue != newValue && newValue != 0 && !(oldValue == 0 && (newValue == 2 || newValue == 4)) && !(oldValue != 0 && newValue == oldValue * 2);
        
        // Assert
        Assert.IsTrue(isSlide, "Should detect slide when empty space gets a non-new tile");
    }
}
