using Microsoft.Extensions.ObjectPool;

namespace TwentyFortyEight.Core;

/// <summary>
/// Stateless board simulator for previewing moves without mutating game state.
/// </summary>
public sealed class BoardMoveSimulator : IBoardSimulator
{
    private readonly ObjectPool<List<int>> _intListPool = ObjectPool.Create(
        new IntListPooledObjectPolicy()
    );

    public (Board newBoard, int scoreIncrease, bool moved, int maxMergedValue) SimulateMove(
        Board board,
        Direction direction
    )
    {
        return direction switch
        {
            Direction.Up => ProcessMoveGeneric(board, isVertical: true, isReverse: false),
            Direction.Down => ProcessMoveGeneric(board, isVertical: true, isReverse: true),
            Direction.Left => ProcessMoveGeneric(board, isVertical: false, isReverse: false),
            Direction.Right => ProcessMoveGeneric(board, isVertical: false, isReverse: true),
            _ => (board, 0, false, 0),
        };
    }

    private (Board newBoard, int scoreIncrease, bool moved, int maxMergedValue) ProcessMoveGeneric(
        Board board,
        bool isVertical,
        bool isReverse
    )
    {
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
            for (int outer = 0; outer < size; outer++)
            {
                values.Clear();

                // Collect non-zero values from board
                for (int inner = 0; inner < size; inner++)
                {
                    var (row, col) = GetBoardPosition(size, outer, inner, isVertical, isReverse);
                    var value = board[row, col];
                    if (value != 0)
                    {
                        values.Add(value);
                    }
                }

                // Merge tiles - using index tracking instead of HashSet
                newValues.Clear();
                int i = 0;
                while (i < values.Count)
                {
                    if (i < values.Count - 1 && values[i] == values[i + 1])
                    {
                        var mergedValue = values[i] * 2;
                        newValues.Add(mergedValue);
                        scoreIncrease += mergedValue;
                        if (mergedValue > maxMergedValue)
                        {
                            maxMergedValue = mergedValue;
                        }

                        i += 2; // Skip both merged tiles
                    }
                    else
                    {
                        newValues.Add(values[i]);
                        i++;
                    }
                }

                // Fill with zeros
                while (newValues.Count < size)
                {
                    newValues.Add(0);
                }

                // Write to result and check if changed
                for (int inner = 0; inner < size; inner++)
                {
                    var (row, col) = GetBoardPosition(size, outer, inner, isVertical, isReverse);
                    result[row, col] = newValues[inner];
                    if (board[row, col] != newValues[inner])
                    {
                        moved = true;
                    }
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

    private static (int row, int col) GetBoardPosition(
        int size,
        int outer,
        int inner,
        bool isVertical,
        bool isReverse
    )
    {
        if (isVertical)
        {
            var row = isReverse ? size - 1 - inner : inner;
            return (row, outer);
        }

        var col = isReverse ? size - 1 - inner : inner;
        return (outer, col);
    }
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
