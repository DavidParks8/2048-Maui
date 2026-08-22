namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class MoveAnalyzerTests
{
    private const int Size = 4;
    private readonly IMoveAnalyzer _analyzer = new MoveAnalyzer();

    /// <summary>
    /// Helper to create a Board from a flat int array for testing.
    /// </summary>
    private static Board CreateBoard(int[] data) => new(data, Size);

    #region Analyze - Spawn Detection Tests

    [TestMethod]
    public void Analyze_NewTileInEmptySpot_DetectedAsSpawned()
    {
        // Arrange: Empty board -> board with one tile
        var previousBoard = new int[16];
        var newBoard = new int[16];
        newBoard[5] = 2; // New tile spawned at index 5

        // Act
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

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
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

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
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

        // Assert
        Assert.DoesNotContain(0, result.SpawnedIndices, "Moved tile should not be spawned");
        Assert.Contains(7, result.SpawnedIndices, "Actual spawn should be detected");
    }

    #endregion

    [TestMethod]
    public void Analyze_WithWall_DoesNotMoveAcrossWall()
    {
        // Arrange
        // Row 0 has a tile at col 3. Moving left would normally move it to col 0.
        // A vertical wall between col 1 and col 2 blocks that, so it should stop at col 2.
        var previousBoard = new int[16];
        previousBoard[3] = 2;

        var newBoard = new int[16];
        newBoard[2] = 2; // moved tile stops at col 2
        newBoard[5] = 2; // spawn

        var wall = new WallSegment(WallOrientation.Vertical, divider: 1, start: 0, length: 1);

        // Act
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard), wall),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

        // Assert
        Assert.Contains(2, result.MovedToIndices, "Moved tile should land at index 2");
        Assert.DoesNotContain(
            0,
            result.MovedToIndices,
            "Tile should not cross the wall to index 0"
        );
        Assert.Contains(5, result.SpawnedIndices, "Spawn should still be detected");
    }

    [TestMethod]
    public void Analyze_ResultIsReusedAndClearedBetweenCalls()
    {
        // Arrange
        var empty = CreateBoard(new int[16]);

        var boardA = new int[16];
        boardA[1] = 2;

        var boardB = new int[16];
        boardB[2] = 2;

        // Act
        var result1 = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(empty),
                CreateBoard(boardA),
                Direction.Left
            )
        );
        var spawned1 = result1.SpawnedIndices.ToArray();

        var result2 = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(empty),
                CreateBoard(boardB),
                Direction.Left
            )
        );
        var spawned2 = result2.SpawnedIndices.ToArray();

        // Assert
        Assert.AreSame(
            result1,
            result2,
            "Analyzer should reuse the same result instance per-thread"
        );
        CollectionAssert.AreEqual(new[] { 1 }, spawned1);
        CollectionAssert.AreEqual(new[] { 2 }, spawned2);
        CollectionAssert.AreEqual(
            new[] { 2 },
            result1.SpawnedIndices.ToArray(),
            "Result should be cleared and repopulated on subsequent calls"
        );
    }

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
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

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
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

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
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

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
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

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
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

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
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard)),
                CreateBoard(newBoard),
                Direction.Up
            )
        );

        // Assert
        Assert.HasCount(2, result.MovedToIndices, "Two moved tiles");
        Assert.Contains(0, result.MovedToIndices, "Moved to index 0");
        Assert.Contains(4, result.MovedToIndices, "Moved to index 4");
        Assert.HasCount(1, result.SpawnedIndices, "One spawned tile");
        Assert.Contains(7, result.SpawnedIndices, "Spawned at index 7");
    }

    [TestMethod]
    public void Analyze_MoveUp_WithHorizontalWall_BlocksCrossingDivider()
    {
        // Arrange
        // Column 0 has a tile at row 3. Moving up would normally move it to row 0.
        // A horizontal wall between row 1 and row 2 blocks that, so it should stop at row 2.
        var previousBoard = new int[16];
        previousBoard[12] = 2; // (3,0)

        var newBoard = new int[16];
        newBoard[8] = 2; // (2,0)
        newBoard[5] = 2; // spawn

        var wall = new WallSegment(WallOrientation.Horizontal, divider: 1, start: 0, length: 1);

        // Act
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard), wall),
                CreateBoard(newBoard),
                Direction.Up
            )
        );

        // Assert
        Assert.Contains(8, result.MovedToIndices, "Moved tile should land at index 8");
        Assert.DoesNotContain(
            0,
            result.MovedToIndices,
            "Tile should not cross the wall to index 0"
        );
        Assert.Contains(5, result.SpawnedIndices, "Spawn should still be detected");
    }

    [TestMethod]
    public void Analyze_MoveDown_WithHorizontalWall_BlocksCrossingDivider()
    {
        // Arrange
        // Column 0 has a tile at row 0. Moving down would normally move it to row 3.
        // A horizontal wall between row 1 and row 2 blocks that, so it should stop at row 1.
        var previousBoard = new int[16];
        previousBoard[0] = 2; // (0,0)

        var newBoard = new int[16];
        newBoard[4] = 2; // (1,0)
        newBoard[5] = 2; // spawn

        var wall = new WallSegment(WallOrientation.Horizontal, divider: 1, start: 0, length: 1);

        // Act
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard), wall),
                CreateBoard(newBoard),
                Direction.Down
            )
        );

        // Assert
        Assert.Contains(4, result.MovedToIndices, "Moved tile should land at index 4");
        Assert.DoesNotContain(
            12,
            result.MovedToIndices,
            "Tile should not cross the wall to index 12"
        );
        Assert.Contains(5, result.SpawnedIndices, "Spawn should still be detected");
    }

    [TestMethod]
    public void Analyze_MergeBlockedByWall_NotDetectedAsMerged()
    {
        // Arrange
        // Row 0: [2,2,0,0]. Normally moving left merges into [4,0,0,0].
        // A vertical wall between col 0 and col 1 blocks that adjacency, so no merge should occur.
        var previousBoard = new int[16];
        previousBoard[0] = 2;
        previousBoard[1] = 2;

        var newBoard = new int[16];
        newBoard[0] = 2;
        newBoard[1] = 2;

        var wall = new WallSegment(WallOrientation.Vertical, divider: 0, start: 0, length: 1);

        // Act
        var result = _analyzer.Analyze(
            new MoveAnalysisRequest(
                new PlayfieldSnapshot(CreateBoard(previousBoard), wall),
                CreateBoard(newBoard),
                Direction.Left
            )
        );

        // Assert
        Assert.IsEmpty(result.MergedIndices, "No merge should be detected across a wall");
        Assert.IsEmpty(result.MovedToIndices, "No movement should be detected when blocked");
        Assert.IsEmpty(result.SpawnedIndices, "No spawn should be detected on no-op boards");
    }

    #endregion
}
