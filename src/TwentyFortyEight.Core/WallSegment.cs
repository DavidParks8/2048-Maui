namespace TwentyFortyEight.Core;

/// <summary>
/// Represents the orientation of a wall segment.
/// </summary>
public enum WallOrientation
{
    /// <summary>
    /// Horizontal wall segment (between rows, blocking vertical movement).
    /// </summary>
    Horizontal,

    /// <summary>
    /// Vertical wall segment (between columns, blocking horizontal movement).
    /// </summary>
    Vertical,
}

/// <summary>
/// Represents a wall segment between two cells.
/// For horizontal walls: between row/row+1 at column col.
/// For vertical walls: between col/col+1 at row row.
/// </summary>
/// <param name="Row">The row coordinate.</param>
/// <param name="Col">The column coordinate.</param>
/// <param name="Orientation">The orientation of the wall.</param>
public readonly record struct WallSegment(int Row, int Col, WallOrientation Orientation)
{
    /// <summary>
    /// Checks if this wall blocks movement from one position to another.
    /// </summary>
    public bool BlocksMovement(Position from, Position to)
    {
        if (Orientation == WallOrientation.Horizontal)
        {
            // Horizontal wall is between rows Row and Row+1
            // Blocks vertical movement between these rows
            int minRow = Math.Min(from.Row, to.Row);
            int maxRow = Math.Max(from.Row, to.Row);

            // Wall must be between the two rows and in the same column
            return Col == from.Column && Col == to.Column && Row >= minRow && Row < maxRow;
        }
        else
        {
            // Vertical wall is between columns Col and Col+1
            // Blocks horizontal movement between these columns
            int minCol = Math.Min(from.Column, to.Column);
            int maxCol = Math.Max(from.Column, to.Column);

            // Wall must be between the two columns and in the same row
            return Row == from.Row && Row == to.Row && Col >= minCol && Col < maxCol;
        }
    }
}
