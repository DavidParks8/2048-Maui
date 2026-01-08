namespace TwentyFortyEight.Core;

/// <summary>
/// Helper methods for interpreting <see cref="WallSegment"/> with respect to line-processing order.
/// </summary>
internal static class WallSegmentSplitHelper
{
    public static int? TryGetSplitIndex(
        Span<int> indices,
        int size,
        int line,
        Direction direction,
        WallSegment? wall
    )
    {
        if (wall is null)
        {
            return null;
        }

        int first;
        int second;

        switch (direction)
        {
            case Direction.Left:
            case Direction.Right:
                if (wall.Orientation != WallOrientation.Vertical)
                {
                    return null;
                }

                if (line < wall.Start || line >= wall.Start + wall.Length)
                {
                    return null;
                }

                var leftIdx = line * size + wall.Divider;
                var rightIdx = leftIdx + 1;
                first = direction == Direction.Left ? leftIdx : rightIdx;
                second = direction == Direction.Left ? rightIdx : leftIdx;
                break;

            case Direction.Up:
            case Direction.Down:
                if (wall.Orientation != WallOrientation.Horizontal)
                {
                    return null;
                }

                if (line < wall.Start || line >= wall.Start + wall.Length)
                {
                    return null;
                }

                var topIdx = wall.Divider * size + line;
                var bottomIdx = topIdx + size;
                first = direction == Direction.Up ? topIdx : bottomIdx;
                second = direction == Direction.Up ? bottomIdx : topIdx;
                break;

            default:
                return null;
        }

        for (int i = 0; i < size - 1; i++)
        {
            if (indices[i] == first && indices[i + 1] == second)
            {
                return i + 1;
            }
        }

        return null;
    }
}
