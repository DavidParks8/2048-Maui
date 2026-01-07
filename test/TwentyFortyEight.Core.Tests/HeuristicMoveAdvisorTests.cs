using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

/// <summary>
/// Unit tests for HeuristicMoveAdvisor to ensure it provides sensible move recommendations.
/// </summary>
[TestClass]
public class HeuristicMoveAdvisorTests
{
    private Mock<IBoardSimulator> _simulatorMock = null!;
    private HeuristicMoveAdvisor _advisor = null!;

    [TestInitialize]
    public void Setup()
    {
        _simulatorMock = new Mock<IBoardSimulator>();
        _advisor = new HeuristicMoveAdvisor(_simulatorMock.Object);
    }

    [TestMethod]
    public void Recommend_EmptyBoard_ReturnsNull()
    {
        // Arrange: Board with no tiles
        int[] boardData = [0, 0, 0, 0];
        var board = new Board(boardData, 2);
        var config = new GameConfig { Size = 2 };

        // All moves return no change (no tiles to move)
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), It.IsAny<Direction>()))
            .Returns((board, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Recommend_GameOverState_ReturnsNull()
    {
        // Arrange: Full board with no possible merges
        int[] boardData = [2, 4, 8, 16];
        var board = new Board(boardData, 2);
        var config = new GameConfig { Size = 2 };

        // Setup simulator to return no moves
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), It.IsAny<Direction>()))
            .Returns((Board b, Direction d) => (b, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Recommend_OnlyOneValidMove_ReturnsThatMove()
    {
        // Arrange: Tile in top-left, can only move left (already there) or stay
        int[] boardData = [2, 0, 0, 0];
        var board = new Board(boardData, 2);
        var config = new GameConfig { Size = 2 };

        // Only left move is valid (for this test scenario)
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Up))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Down))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Right))
            .Returns((board, 0, false, 0));

        int[] movedData = [2, 0, 0, 0];
        var movedBoard = new Board(movedData, 2);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Left))
            .Returns((movedBoard, 0, true, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Direction.Left, result.Value.Direction);
    }

    [TestMethod]
    public void Recommend_PrefersCreatingSpace()
    {
        // Arrange: Two tiles that can merge vs tiles that just shift
        int[] boardData =
        [
            2,
            2,
            0,
            0, // Row 0: can merge
            4,
            0,
            0,
            0, // Row 1
            0,
            0,
            0,
            0, // Row 2
            0,
            0,
            0,
            0, // Row 3
        ];
        var board = new Board(boardData, 4);
        var config = new GameConfig { Size = 4 };

        // Left: Creates merge (4) and opens space
        int[] leftData = [4, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var leftBoard = new Board(leftData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Left))
            .Returns((leftBoard, 4, true, 4));

        // Up: Just shifts (no merge in this scenario)
        int[] upData = [2, 2, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var upBoard = new Board(upData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Up))
            .Returns((upBoard, 0, true, 0));

        // Right/Down: no move
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Right))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Down))
            .Returns((board, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Direction.Left, result.Value.Direction);
        Assert.AreEqual(MoveCoachReason.CreateSpace, result.Value.PrimaryReason);
    }

    [TestMethod]
    public void Recommend_ReasonIsCreateSpace_WhenMergeCreatesSpace()
    {
        // Arrange: Board with merge opportunity that creates space
        int[] boardData =
        [
            2,
            2,
            4,
            8, // Row 0: can merge 2+2
            4,
            8,
            16,
            32, // Row 1
            8,
            16,
            32,
            64, // Row 2
            16,
            32,
            64,
            0, // Row 3: one empty
        ];
        var board = new Board(boardData, 4);
        var config = new GameConfig { Size = 4 };

        // Left: Merges tiles (2+2=4) and creates new empty space
        int[] leftData = [4, 4, 8, 0, 4, 8, 16, 32, 8, 16, 32, 64, 16, 32, 64, 0];
        var leftBoard = new Board(leftData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Left))
            .Returns((leftBoard, 4, true, 4));

        // Other directions: no move
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Up))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Right))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Down))
            .Returns((board, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Direction.Left, result.Value.Direction);
        // CreateSpace takes priority over MergeTiles in heuristic weighting (1000 vs 80)
        Assert.AreEqual(MoveCoachReason.CreateSpace, result.Value.PrimaryReason);
    }

    [TestMethod]
    public void Recommend_ReasonIsKeepLargestInCorner_WhenMaxMovesToCorner()
    {
        // Arrange: Max tile not in corner initially
        int[] boardData = [2, 0, 0, 0, 0, 128, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var board = new Board(boardData, 4);
        var config = new GameConfig { Size = 4 };

        // Up: Moves max to top row
        int[] upData = [2, 128, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var upBoard = new Board(upData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Up))
            .Returns((upBoard, 0, true, 0));

        // Left: Moves max to corner!
        int[] leftData = [128, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var leftBoard = new Board(leftData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Left))
            .Returns((leftBoard, 0, true, 0));

        // Other directions
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Right))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Down))
            .Returns((board, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Direction.Left, result.Value.Direction);
        Assert.AreEqual(MoveCoachReason.KeepLargestInCorner, result.Value.PrimaryReason);
    }

    [TestMethod]
    public void Recommend_ConsistentResults_ForSameBoard()
    {
        // Arrange: Same board state
        int[] boardData = [2, 4, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var board = new Board(boardData, 4);
        var config = new GameConfig { Size = 4 };

        // Setup consistent simulator responses
        int[] leftData = [2, 4, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var leftBoard = new Board(leftData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Left))
            .Returns((leftBoard, 0, true, 0));

        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Up))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Right))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Down))
            .Returns((board, 0, false, 0));

        // Act: Call twice
        var result1 = _advisor.Recommend(board, config);
        var result2 = _advisor.Recommend(board, config);

        // Assert: Should be identical
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.AreEqual(result1.Value.Direction, result2.Value.Direction);
        Assert.AreEqual(result1.Value.Score, result2.Value.Score);
        Assert.AreEqual(result1.Value.PrimaryReason, result2.Value.PrimaryReason);
    }

    [TestMethod]
    public void Recommend_HandlesDifferentBoardSizes()
    {
        // Test 3x3 board
        int[] boardData3x3 = [2, 2, 0, 0, 0, 0, 0, 0, 0];
        var board3x3 = new Board(boardData3x3, 3);
        var config3x3 = new GameConfig { Size = 3 };

        int[] movedData3x3 = [4, 0, 0, 0, 0, 0, 0, 0, 0];
        var movedBoard3x3 = new Board(movedData3x3, 3);
        _simulatorMock
            .Setup(s => s.SimulateMove(board3x3, Direction.Left))
            .Returns((movedBoard3x3, 4, true, 4));

        _simulatorMock
            .Setup(s => s.SimulateMove(board3x3, Direction.Up))
            .Returns((board3x3, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(board3x3, Direction.Right))
            .Returns((board3x3, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(board3x3, Direction.Down))
            .Returns((board3x3, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board3x3, config3x3);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(Direction.Left, result.Value.Direction);
    }

    [TestMethod]
    public void Recommend_AllMovesInvalid_ReturnsNull()
    {
        // Arrange
        int[] boardData = [2, 4, 8, 16];
        var board = new Board(boardData, 2);
        var config = new GameConfig { Size = 2 };

        // All moves return no change
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), It.IsAny<Direction>()))
            .Returns((board, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Recommend_ScoreIsNonNegative_WhenValidMoveExists()
    {
        // Arrange
        int[] boardData = [2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var board = new Board(boardData, 4);
        var config = new GameConfig { Size = 4 };

        int[] movedData = [2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var movedBoard = new Board(movedData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Left))
            .Returns((movedBoard, 0, true, 0));

        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Up))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Right))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Down))
            .Returns((board, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert
        Assert.IsNotNull(result);
        // Result has a valid score
        Assert.IsTrue(result.HasValue);
    }

    [TestMethod]
    public void Recommend_PrioritizesEmptySpaceCreation()
    {
        // Arrange: Two options - one creates more space
        int[] boardData = [2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0];
        var board = new Board(boardData, 4);
        var config = new GameConfig { Size = 4 };

        // Move that creates more space (via merges)
        int[] goodData = [4, 4, 4, 4, 4, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var goodBoard = new Board(goodData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Left))
            .Returns((goodBoard, 8, true, 4));

        // Move that creates less space
        int[] okData = [2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0];
        var okBoard = new Board(okData, 4);
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Up))
            .Returns((okBoard, 0, true, 0));

        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Right))
            .Returns((board, 0, false, 0));
        _simulatorMock
            .Setup(s => s.SimulateMove(It.IsAny<Board>(), Direction.Down))
            .Returns((board, 0, false, 0));

        // Act
        var result = _advisor.Recommend(board, config);

        // Assert: Should prefer left (more space)
        Assert.IsNotNull(result);
        Assert.AreEqual(Direction.Left, result.Value.Direction);
    }
}
