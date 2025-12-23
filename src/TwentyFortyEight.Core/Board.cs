using CommunityToolkit.HighPerformance;

namespace TwentyFortyEight.Core;

/// <summary>
/// Represents a 2048 game board with high-performance 2D access.
/// Uses a native 2D array internally and provides Span2D access for efficient operations.
/// </summary>
public readonly struct Board : IEquatable<Board>
{
    private readonly int[,] _data;

    /// <summary>
    /// Gets the size of the board (e.g., 4 for a 4x4 board).
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the total number of cells on the board.
    /// </summary>
    public int Length => Size * Size;

    /// <summary>
    /// Creates a new empty board with the specified size.
    /// </summary>
    /// <param name="size">The size of the board (e.g., 4 for a 4x4 board).</param>
    public Board(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        Size = size;
        _data = new int[size, size];
    }

    /// <summary>
    /// Creates a board from an existing flat array (for deserialization/compatibility).
    /// The array is copied to ensure immutability.
    /// </summary>
    /// <param name="data">The flat array representing the board in row-major order.</param>
    /// <param name="size">The size of the board.</param>
    public Board(int[] data, int size)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        if (data.Length != size * size)
        {
            throw new ArgumentException(
                $"Data length ({data.Length}) must equal size squared ({size * size}).",
                nameof(data)
            );
        }

        Size = size;
        _data = new int[size, size];

        // Copy from flat array to 2D in row-major order
        for (int i = 0; i < data.Length; i++)
        {
            _data[i / size, i % size] = data[i];
        }
    }

    /// <summary>
    /// Creates a board from an existing 2D array.
    /// The array is cloned to ensure immutability.
    /// </summary>
    /// <param name="data">The 2D array representing the board.</param>
    public Board(int[,] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        int rows = data.GetLength(0);
        int cols = data.GetLength(1);

        if (rows != cols)
        {
            throw new ArgumentException("Board must be square.", nameof(data));
        }

        Size = rows;
        _data = (int[,])data.Clone();
    }

    /// <summary>
    /// Private constructor that takes ownership of the array (no cloning).
    /// </summary>
    private Board(int[,] data, int size, bool takeOwnership)
    {
        Size = size;
        _data = takeOwnership ? data : (int[,])data.Clone();
    }

    /// <summary>
    /// Gets the tile value at the specified flat index (row-major order).
    /// </summary>
    public int this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            return _data[index / Size, index % Size];
        }
    }

    /// <summary>
    /// Gets the tile value at the specified row and column.
    /// </summary>
    public int this[int row, int col]
    {
        get
        {
            ValidatePosition(row, col);
            return _data[row, col];
        }
    }

    /// <summary>
    /// Gets the tile value at the specified position.
    /// </summary>
    public int this[Position position] => _data[position.Row, position.Column];

    /// <summary>
    /// Gets a read-only 2D span view of the board.
    /// </summary>
    public ReadOnlySpan2D<int> AsReadOnlySpan2D() => _data;

    /// <summary>
    /// Creates a mutable copy of this board as a 2D array.
    /// Use this when you need to modify the board for game logic.
    /// </summary>
    public int[,] ToMutableArray() => (int[,])_data.Clone();

    /// <summary>
    /// Creates a Board from a modified 2D array.
    /// Takes ownership of the array (no cloning) for efficiency.
    /// </summary>
    internal static Board FromMutableArray(int[,] data, int size) =>
        new(data, size, takeOwnership: true);

    /// <summary>
    /// Gets a copy of the underlying data as a flat array.
    /// Use sparingly as this allocates a new array (mainly for serialization).
    /// </summary>
    public int[] ToArray()
    {
        var result = new int[Length];
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                result[row * Size + col] = _data[row, col];
            }
        }
        return result;
    }

    /// <summary>
    /// Creates a new board with the tile at the specified position set to the given value.
    /// </summary>
    public Board WithTile(int row, int col, int value)
    {
        ValidatePosition(row, col);
        var newData = (int[,])_data.Clone();
        newData[row, col] = value;
        return new Board(newData, Size, takeOwnership: true);
    }

    /// <summary>
    /// Creates a new board with the tile at the specified flat index set to the given value.
    /// </summary>
    public Board WithTile(int index, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
        return WithTile(index / Size, index % Size, value);
    }

    /// <summary>
    /// Creates a clone of this board.
    /// </summary>
    public Board Clone() => new((int[,])_data.Clone(), Size, takeOwnership: true);

    /// <summary>
    /// Calculates the flat index from row and column.
    /// </summary>
    public int GetIndex(int row, int col)
    {
        ValidatePosition(row, col);
        return row * Size + col;
    }

    /// <summary>
    /// Calculates the row and column from a flat index.
    /// </summary>
    public (int Row, int Column) GetPosition(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
        return (index / Size, index % Size);
    }

    /// <summary>
    /// Counts the number of empty cells on the board.
    /// </summary>
    public int CountEmptyCells()
    {
        int count = 0;
        var span = AsReadOnlySpan2D();
        foreach (var value in span)
        {
            if (value == 0)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Finds all empty cell positions.
    /// </summary>
    public List<Position> FindEmptyCells()
    {
        var emptyCells = new List<Position>();
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if (_data[row, col] == 0)
                {
                    emptyCells.Add(new Position(row, col));
                }
            }
        }
        return emptyCells;
    }

    /// <summary>
    /// Checks if the board contains the specified value.
    /// </summary>
    public bool Contains(int value)
    {
        var span = AsReadOnlySpan2D();
        foreach (var tile in span)
        {
            if (tile == value)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the board contains any tile with a value greater than or equal to the specified threshold.
    /// </summary>
    public bool ContainsAtLeast(int threshold)
    {
        var span = AsReadOnlySpan2D();
        foreach (var tile in span)
        {
            if (tile >= threshold)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if any adjacent tiles can merge (used for game over detection).
    /// </summary>
    public bool HasPossibleMerges()
    {
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                var current = _data[row, col];

                // Check right
                if (col < Size - 1 && current == _data[row, col + 1])
                    return true;

                // Check down
                if (row < Size - 1 && current == _data[row + 1, col])
                    return true;
            }
        }
        return false;
    }

    private void ValidatePosition(int row, int col)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Size);
        ArgumentOutOfRangeException.ThrowIfNegative(col);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Size);
    }

    #region Equality

    public bool Equals(Board other)
    {
        if (Size != other.Size)
            return false;

        var span1 = AsReadOnlySpan2D();
        var span2 = other.AsReadOnlySpan2D();

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if (span1[row, col] != span2[row, col])
                    return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is Board other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Size);
        var span = AsReadOnlySpan2D();
        foreach (var value in span)
        {
            hash.Add(value);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(Board left, Board right) => left.Equals(right);

    public static bool operator !=(Board left, Board right) => !left.Equals(right);

    #endregion
}
