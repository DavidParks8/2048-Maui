using Microsoft.Extensions.ObjectPool;

namespace TwentyFortyEight.Core;

/// <summary>
/// Stateless board simulator for previewing moves without mutating game state.
/// </summary>
internal sealed class BoardMoveSimulator : IBoardSimulator
{
    private readonly ObjectPool<List<int>> _intListPool = ObjectPool.Create(
        new IntListPooledObjectPolicy()
    );

    public (Board newBoard, int scoreIncrease, bool moved, int maxMergedValue) SimulateMove(
        BoardMoveRequest request
    )
    {
        var playfield = request.Playfield;
        var direction = request.Direction;
        var board = playfield.Board;
        var wall = playfield.Wall;
        var size = board.Size;
        var result = new int[size, size];
        var moved = false;
        var scoreIncrease = 0;
        var maxMergedValue = 0;

        // Rent pooled lists to avoid allocations
        var values = _intListPool.Get();
        var newValues = _intListPool.Get();

        try
        {
            Span<int> indices = stackalloc int[size];

            for (int outer = 0; outer < size; outer++)
            {
                FillLineIndices(indices, size, outer, direction);
                var split = WallSegmentSplitHelper.TryGetSplitIndex(
                    indices,
                    size,
                    outer,
                    direction,
                    wall
                );

                if (split is null)
                {
                    ProcessSegment(
                        board,
                        result,
                        indices,
                        0,
                        size,
                        values,
                        newValues,
                        ref moved,
                        ref scoreIncrease,
                        ref maxMergedValue
                    );
                }
                else
                {
                    ProcessSegment(
                        board,
                        result,
                        indices,
                        0,
                        split.Value,
                        values,
                        newValues,
                        ref moved,
                        ref scoreIncrease,
                        ref maxMergedValue
                    );
                    ProcessSegment(
                        board,
                        result,
                        indices,
                        split.Value,
                        size - split.Value,
                        values,
                        newValues,
                        ref moved,
                        ref scoreIncrease,
                        ref maxMergedValue
                    );
                }
            }
        }
        finally
        {
            _intListPool.Return(values);
            _intListPool.Return(newValues);
        }

        return (Board.FromMutableArray(result, size), scoreIncrease, moved, maxMergedValue);
    }

    private static void FillLineIndices(Span<int> indices, int size, int line, Direction direction)
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

    private static void ProcessSegment(
        Board board,
        int[,] result,
        Span<int> indices,
        int segmentStart,
        int segmentLength,
        List<int> values,
        List<int> newValues,
        ref bool moved,
        ref int scoreIncrease,
        ref int maxMergedValue
    )
    {
        values.Clear();
        for (int i = 0; i < segmentLength; i++)
        {
            var idx = indices[segmentStart + i];
            var value = board[idx];
            if (value != 0)
            {
                values.Add(value);
            }
        }

        newValues.Clear();
        int readIndex = 0;
        while (readIndex < values.Count)
        {
            if (readIndex < values.Count - 1 && values[readIndex] == values[readIndex + 1])
            {
                var mergedValue = values[readIndex] * 2;
                newValues.Add(mergedValue);
                scoreIncrease += mergedValue;
                if (mergedValue > maxMergedValue)
                {
                    maxMergedValue = mergedValue;
                }

                readIndex += 2;
            }
            else
            {
                newValues.Add(values[readIndex]);
                readIndex++;
            }
        }

        while (newValues.Count < segmentLength)
        {
            newValues.Add(0);
        }

        for (int i = 0; i < segmentLength; i++)
        {
            var idx = indices[segmentStart + i];
            var row = idx / board.Size;
            var col = idx % board.Size;
            var newValue = newValues[i];
            result[row, col] = newValue;
            if (board[row, col] != newValue)
            {
                moved = true;
            }
        }
    }

    // GetBoardPosition removed in favor of FillLineIndices for wall-aware segmentation.
}

/// <summary>
/// Pooled object policy for List<int>.
/// </summary>
file sealed class IntListPooledObjectPolicy : PooledObjectPolicy<List<int>>
{
    public override List<int> Create() => new(8);

    public override bool Return(List<int> obj)
    {
        obj.Clear();
        return true;
    }
}
