using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Tests;

[TestClass]
public class MoveAnalyzerTests
{
    private const int Size = 4;
    private readonly IMoveAnalyzer _analyzer = new MoveAnalyzer();

    #region Analyze - Spawn Detection Tests

    [TestMethod]
    public void Analyze_NewTileInEmptySpot_DetectedAsSpawned()
    {
        // Arrange: Empty board -> board with one tile
        var previousBoard = new int[16];
        var newBoard = new int[16];
        newBoard[5] = 2; // New tile spawned at index 5

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Left);

        // Assert
        Assert.HasCount(1, result.SpawnedIndices, "Should have one spawned tile");
        Assert.Contains(5, result.SpawnedIndices, "Spawned tile should be at index 5");
    }

    [TestMethod]
    public void Analyze_TileMovedAway_NewTileInVacatedSpot_DetectedAsSpawned()
    {
        // Arrange: Tile at index 1 moves left to index 0, new tile spawns at index 1
        var previousBoard = new int[16];
        previousBoard[1] = 2;

        var newBoard = new int[16];
        newBoard[0] = 2; // Original tile moved here
        newBoard[1] = 2; // NEW tile spawned in vacated spot

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Left);

        // Assert
        Assert.HasCount(1, result.SpawnedIndices, "Should have one spawned tile");
        Assert.Contains(1, result.SpawnedIndices, "Tile at index 1 should be detected as spawned");
        Assert.DoesNotContain(0, result.SpawnedIndices, "Tile at index 0 should NOT be spawned");
    }

    [TestMethod]
    public void Analyze_TileMovedTo_NotDetectedAsSpawned()
    {
        // Arrange: Tile moves from index 2 to index 0
        var previousBoard = new int[16];
        previousBoard[2] = 4;

        var newBoard = new int[16];
        newBoard[0] = 4;
        newBoard[7] = 2; // Actual spawn

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Left);

        // Assert
        Assert.DoesNotContain(0, result.SpawnedIndices, "Moved tile should not be spawned");
        Assert.Contains(7, result.SpawnedIndices, "Actual spawn should be detected");
    }

    #endregion

    #region Analyze - Merge Detection Tests

    [TestMethod]
    public void Analyze_TwoTilesMerge_DetectedAsMerged()
    {
        // Arrange: [2,2,0,0] -> [4,0,0,0] + spawn at some position
        var previousBoard = new int[16];
        previousBoard[0] = 2;
        previousBoard[1] = 2;

        var newBoard = new int[16];
        newBoard[0] = 4; // Merged tile
        newBoard[5] = 2; // Spawn

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Left);

        // Assert
        Assert.HasCount(1, result.MergedIndices, "Should have one merged tile");
        Assert.Contains(0, result.MergedIndices, "Merged tile should be at index 0");
    }

    [TestMethod]
    public void Analyze_MultipleMergesInRow_AllDetected()
    {
        // Arrange: [2,2,4,4] -> [4,8,0,0] (two merges)
        var previousBoard = new int[16];
        previousBoard[0] = 2;
        previousBoard[1] = 2;
        previousBoard[2] = 4;
        previousBoard[3] = 4;

        var newBoard = new int[16];
        newBoard[0] = 4; // Merged from 2+2
        newBoard[1] = 8; // Merged from 4+4
        newBoard[10] = 2; // Spawn

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Left);

        // Assert
        Assert.HasCount(2, result.MergedIndices, "Should have two merged tiles");
        Assert.Contains(0, result.MergedIndices, "First merge at index 0");
        Assert.Contains(1, result.MergedIndices, "Second merge at index 1");
    }

    #endregion

    #region Analyze - Moved Detection Tests

    [TestMethod]
    public void Analyze_TileMovesWithoutMerge_DetectedAsMoved()
    {
        // Arrange: Tile moves from index 2 to index 0
        var previousBoard = new int[16];
        previousBoard[2] = 4;

        var newBoard = new int[16];
        newBoard[0] = 4;
        newBoard[7] = 2; // Spawn

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Left);

        // Assert
        Assert.HasCount(1, result.MovedToIndices, "Should have one moved tile");
        Assert.Contains(0, result.MovedToIndices, "Moved tile at index 0");
    }

    [TestMethod]
    public void Analyze_TileStaysInPlace_NotDetectedAsMoved()
    {
        // Arrange: Tile already at edge, doesn't move
        var previousBoard = new int[16];
        previousBoard[0] = 4;

        var newBoard = new int[16];
        newBoard[0] = 4;
        newBoard[7] = 2; // Spawn

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Left);

        // Assert
        Assert.IsEmpty(result.MovedToIndices, "Should have no moved tiles");
    }

    #endregion

    #region Analyze - Complex Scenarios

    [TestMethod]
    public void Analyze_ComplexMove_AllCategoriesCorrect()
    {
        // Arrange: Row 0: [2,0,2,4] -> [4,4,0,0] (merge + move)
        // Plus spawn at index 5
        var previousBoard = new int[16];
        previousBoard[0] = 2;
        previousBoard[2] = 2;
        previousBoard[3] = 4;

        var newBoard = new int[16];
        newBoard[0] = 4; // Merged
        newBoard[1] = 4; // Moved (was at index 3)
        newBoard[5] = 2; // Spawned

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Left);

        // Assert
        Assert.Contains(0, result.MergedIndices, "Index 0 should be merged");
        Assert.Contains(1, result.MovedToIndices, "Index 1 should be moved-to");
        Assert.Contains(5, result.SpawnedIndices, "Index 5 should be spawned");

        Assert.HasCount(1, result.MergedIndices, "One merged tile");
        Assert.HasCount(1, result.MovedToIndices, "One moved tile");
        Assert.HasCount(1, result.SpawnedIndices, "One spawned tile");
    }

    [TestMethod]
    public void Analyze_MoveUp_CorrectCategories()
    {
        // Arrange: Column 0: tiles at rows 2 and 3 move up
        // Index 8 (row 2, col 0) = 2
        // Index 12 (row 3, col 0) = 4
        // After move up: index 0 = 2, index 4 = 4
        var previousBoard = new int[16];
        previousBoard[8] = 2;
        previousBoard[12] = 4;

        var newBoard = new int[16];
        newBoard[0] = 2;
        newBoard[4] = 4;
        newBoard[7] = 2; // Spawn

        // Act
        var result = _analyzer.Analyze(previousBoard, newBoard, Size, Direction.Up);

        // Assert
        Assert.HasCount(2, result.MovedToIndices, "Two moved tiles");
        Assert.Contains(0, result.MovedToIndices, "Moved to index 0");
        Assert.Contains(4, result.MovedToIndices, "Moved to index 4");
        Assert.HasCount(1, result.SpawnedIndices, "One spawned tile");
        Assert.Contains(7, result.SpawnedIndices, "Spawned at index 7");
    }

    #endregion
}
