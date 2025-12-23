namespace TwentyFortyEight.Core;

/// <summary>
/// Analyzes tile movements and categorizes tiles after a move.
/// This logic is platform-agnostic and can be used by any UI implementation.
/// </summary>
public class MoveAnalyzer : IMoveAnalyzer
{
    /// <inheritdoc />
    public MoveAnalysisResult Analyze(
        int[] previousBoard,
        int[] newBoard,
        int size,
        Direction direction
    )
    {
        var movements = CalculateTileMovements(previousBoard, size, direction);

        var spawnedIndices = new HashSet<int>();
        var mergedIndices = new HashSet<int>();
        var movedToIndices = new HashSet<int>();

        for (int i = 0; i < newBoard.Length; i++)
        {
            var newValue = newBoard[i];
            var oldValue = previousBoard[i];
            Position position = new(i / size, i % size);

            if (newValue == 0)
                continue;

            // Check if a tile moved away from this position
            var movedAwayFrom = movements.Any(m => m.From == position);
            // Check if a tile moved to this position
            var isMovedHere = movements.Any(m => m.To == position);
            // Check if this position received a merge
            var hasMergingMovement = movements.Any(m => m.To == position && m.IsMerging);

            // Case 1: Tile merged (check this first, as merges can produce 4s too)
            if (hasMergingMovement)
            {
                mergedIndices.Add(i);
            }
            // Case 2: New tile spawned (must be 2 or 4, and nothing moved here)
            // Either: was empty and now has 2/4 (and nothing moved here)
            // Or: a tile moved away and there's still a value here (new spawn in vacated spot)
            else if (
                (newValue == 2 || newValue == 4)
                && ((oldValue == 0 && !isMovedHere) || (movedAwayFrom && !isMovedHere))
            )
            {
                spawnedIndices.Add(i);
            }
            // Case 3: Tile moved here (without merging)
            else if (isMovedHere)
            {
                movedToIndices.Add(i);
            }
        }

        return new MoveAnalysisResult(movements, spawnedIndices, mergedIndices, movedToIndices);
    }

    /// <summary>
    /// Calculates tile movements from the previous board state.
    /// This tracks where each tile moves to, including merges.
    /// </summary>
    private List<TileMovement> CalculateTileMovements(
        int[] previousBoard,
        int size,
        Direction direction
    )
    {
        var movements = new List<TileMovement>();

        // Process each line (row or column) depending on direction
        for (int line = 0; line < size; line++)
        {
            // Get indices for this line based on direction
            var indices = GetLineIndices(line, size, direction);

            // Collect non-zero tiles from previous board with their positions
            var tiles = new List<(int index, int value)>();
            foreach (var idx in indices)
            {
                if (previousBoard[idx] != 0)
                {
                    tiles.Add((idx, previousBoard[idx]));
                }
            }

            if (tiles.Count == 0)
                continue;

            // Process tiles: merge and compact toward the direction
            int destPosition = 0;
            int i = 0;
            while (i < tiles.Count)
            {
                var (sourceIdx, value) = tiles[i];
                var source = new Position(sourceIdx / size, sourceIdx % size);

                // Check if next tile can merge with this one
                if (i + 1 < tiles.Count && tiles[i + 1].value == value)
                {
                    // Merge: both tiles move to the destination
                    var destIdx = indices[destPosition];
                    var dest = new Position(destIdx / size, destIdx % size);

                    // First tile moves and merges
                    movements.Add(new TileMovement(source, dest, value, true));

                    // Second tile also moves and merges
                    var (source2Idx, _) = tiles[i + 1];
                    var source2 = new Position(source2Idx / size, source2Idx % size);
                    movements.Add(new TileMovement(source2, dest, value, true));

                    i += 2;
                }
                else
                {
                    // No merge: tile just moves (or stays)
                    var destIdx = indices[destPosition];
                    var dest = new Position(destIdx / size, destIdx % size);

                    // Only record if actually moving
                    if (source != dest)
                    {
                        movements.Add(new TileMovement(source, dest, value, false));
                    }

                    i++;
                }
                destPosition++;
            }
        }

        return movements;
    }

    /// <summary>
    /// Gets the board indices for a line (row or column) in the order they should be processed.
    /// </summary>
    private static int[] GetLineIndices(int line, int size, Direction direction)
    {
        var indices = new int[size];
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
        return indices;
    }
}
