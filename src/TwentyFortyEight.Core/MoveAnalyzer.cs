using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.ObjectPool;

namespace TwentyFortyEight.Core;

/// <summary>
/// Analyzes tile movements and categorizes tiles after a move.
/// This logic is platform-agnostic and can be used by any UI implementation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thread Safety:</b> This class uses thread-local storage for the result object.
/// Each thread gets its own <see cref="MoveAnalysisResult"/> instance that is reused across calls.
/// </para>
/// <para>
/// <b>IMPORTANT - Result Reuse:</b> The <see cref="MoveAnalysisResult"/> returned by <see cref="Analyze"/>
/// is <b>cleared and repopulated on each call</b>. Do not hold references to the result across
/// multiple calls to <see cref="Analyze"/>. If you need to preserve data, copy it immediately:
/// <code>
/// var result = analyzer.Analyze(new MoveAnalysisRequest(prev, next, dir));
/// var movementsCopy = result.Movements.ToList(); // Copy before next Analyze call
/// </code>
/// </para>
/// </remarks>
public class MoveAnalyzer : IMoveAnalyzer
{
    // Pool for temporary lookup HashSets (not exposed to callers)
    private static readonly ObjectPool<HashSet<Position>> s_positionSetPool = ObjectPool.Create(
        new PositionHashSetPooledObjectPolicy()
    );

    // Pool for temporary tile lists used during calculation
    private static readonly ObjectPool<List<(int index, int value)>> s_tileListPool =
        ObjectPool.Create(new TileListPooledObjectPolicy());

    // Thread-static reusable result instance - each thread gets its own to avoid contention.
    [ThreadStatic]
    private static MoveAnalysisResult? s_result;

    /// <inheritdoc />
    public MoveAnalysisResult Analyze(MoveAnalysisRequest request)
    {
        var result = s_result ??= new MoveAnalysisResult();
        // Clear the reusable result before populating
        result.Clear();

        var previousBoard = request.PreviousBoard;
        var newBoard = request.NewBoard;

        // Calculate movements directly into the result
        CalculateTileMovements(previousBoard, request.Direction, request.Wall, result);

        // Rent pooled HashSets for internal lookups only
        var movedFromPositions = s_positionSetPool.Get();
        var movedToPositions = s_positionSetPool.Get();
        var mergeTargetPositions = s_positionSetPool.Get();

        try
        {
            foreach (var movement in result.Movements)
            {
                movedFromPositions.Add(movement.From);
                movedToPositions.Add(movement.To);
                if (movement.IsMerging)
                {
                    mergeTargetPositions.Add(movement.To);
                }
            }

            for (int i = 0; i < newBoard.Length; i++)
            {
                var newValue = newBoard[i];
                var oldValue = previousBoard[i];
                var (row, col) = newBoard.GetPosition(i);
                Position position = new(row, col);

                if (newValue == 0)
                    continue;

                // Use pre-computed lookups instead of LINQ queries
                var movedAwayFrom = movedFromPositions.Contains(position);
                var isMovedHere = movedToPositions.Contains(position);
                var hasMergingMovement = mergeTargetPositions.Contains(position);

                // Case 1: Tile merged (check this first, as merges can produce 4s too)
                if (hasMergingMovement)
                {
                    result.AddMergedIndex(i);
                }
                // Case 2: New tile spawned (must be 2 or 4, and nothing moved here)
                // Either: was empty and now has 2/4 (and nothing moved here)
                // Or: a tile moved away and there's still a value here (new spawn in vacated spot)
                else if (
                    (newValue == 2 || newValue == 4)
                    && ((oldValue == 0 && !isMovedHere) || (movedAwayFrom && !isMovedHere))
                )
                {
                    result.AddSpawnedIndex(i);
                }
                // Case 3: Tile moved here (without merging)
                else if (isMovedHere)
                {
                    result.AddMovedToIndex(i);
                }
            }

            return result;
        }
        finally
        {
            // Return internal lookup sets to pool
            s_positionSetPool.Return(movedFromPositions);
            s_positionSetPool.Return(movedToPositions);
            s_positionSetPool.Return(mergeTargetPositions);
        }
    }

    /// <summary>
    /// Calculates tile movements from the previous board state and adds them to the result.
    /// This tracks where each tile moves to, including merges.
    /// </summary>
    private static void CalculateTileMovements(
        Board previousBoard,
        Direction direction,
        WallSegment? wall,
        MoveAnalysisResult result
    )
    {
        var size = previousBoard.Size;

        // Use SpanOwner for pooled array allocation
        using SpanOwner<int> indicesOwner = SpanOwner<int>.Allocate(size);
        var indices = indicesOwner.Span;

        // Process each line (row or column) depending on direction
        for (int line = 0; line < size; line++)
        {
            // Get indices for this line based on direction
            FillLineIndices(indices, line, size, direction);

            var split = WallSegmentSplitHelper.TryGetSplitIndex(
                indices,
                size,
                line,
                direction,
                wall
            );

            if (split is null)
            {
                CalculateSegmentMovements(previousBoard, indices, 0, size, result);
            }
            else
            {
                CalculateSegmentMovements(previousBoard, indices, 0, split.Value, result);
                CalculateSegmentMovements(
                    previousBoard,
                    indices,
                    split.Value,
                    size - split.Value,
                    result
                );
            }
        }
    }

    private static void CalculateSegmentMovements(
        Board previousBoard,
        Span<int> indices,
        int segmentStart,
        int segmentLength,
        MoveAnalysisResult result
    )
    {
        var tiles = s_tileListPool.Get();
        try
        {
            tiles.Clear();
            for (int i = 0; i < segmentLength; i++)
            {
                var idx = indices[segmentStart + i];
                if (previousBoard[idx] != 0)
                {
                    tiles.Add((idx, previousBoard[idx]));
                }
            }

            if (tiles.Count == 0)
            {
                return;
            }

            int destPosition = segmentStart;
            int iTile = 0;
            while (iTile < tiles.Count)
            {
                var (sourceIdx, value) = tiles[iTile];
                var (sourceRow, sourceCol) = previousBoard.GetPosition(sourceIdx);
                Position source = new(sourceRow, sourceCol);

                if (iTile + 1 < tiles.Count && tiles[iTile + 1].value == value)
                {
                    var destIdx = indices[destPosition];
                    var (destRow, destCol) = previousBoard.GetPosition(destIdx);
                    Position dest = new(destRow, destCol);

                    result.AddMovement(new TileMovement(source, dest, value, true));

                    var (source2Idx, _) = tiles[iTile + 1];
                    var (source2Row, source2Col) = previousBoard.GetPosition(source2Idx);
                    Position source2 = new(source2Row, source2Col);
                    result.AddMovement(new TileMovement(source2, dest, value, true));

                    iTile += 2;
                }
                else
                {
                    var destIdx = indices[destPosition];
                    var (destRow, destCol) = previousBoard.GetPosition(destIdx);
                    Position dest = new(destRow, destCol);

                    if (source != dest)
                    {
                        result.AddMovement(new TileMovement(source, dest, value, false));
                    }

                    iTile++;
                }

                destPosition++;
            }
        }
        finally
        {
            s_tileListPool.Return(tiles);
        }
    }

    /// <summary>
    /// Fills the indices span with board indices for a line (row or column) in the order they should be processed.
    /// </summary>
    private static void FillLineIndices(Span<int> indices, int line, int size, Direction direction)
    {
        for (int i = 0; i < size; i++)
        {
            indices[i] = direction switch
            {
                Direction.Left => line * size + i,
                Direction.Right => line * size + (size - 1 - i),
                Direction.Up => i * size + line,
                Direction.Down => (size - 1 - i) * size + line,
                _ => 0,
            };
        }
    }
}

/// <summary>
/// Pooled object policy for HashSet&lt;Position&gt;.
/// </summary>
file sealed class PositionHashSetPooledObjectPolicy : PooledObjectPolicy<HashSet<Position>>
{
    public override HashSet<Position> Create() => new(16);

    public override bool Return(HashSet<Position> obj)
    {
        obj.Clear();
        return true;
    }
}

/// <summary>
/// Pooled object policy for List&lt;(int index, int value)&gt;.
/// </summary>
file sealed class TileListPooledObjectPolicy : PooledObjectPolicy<List<(int index, int value)>>
{
    public override List<(int index, int value)> Create() => new(8);

    public override bool Return(List<(int index, int value)> obj)
    {
        obj.Clear();
        return true;
    }
}
