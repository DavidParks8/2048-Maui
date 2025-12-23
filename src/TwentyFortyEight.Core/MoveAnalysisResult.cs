namespace TwentyFortyEight.Core;

/// <summary>
/// Represents the result of analyzing a move, including tile movements and categorizations.
/// This class is designed to be reused to avoid allocations. Call <see cref="Clear"/> before repopulating.
/// </summary>
public sealed class MoveAnalysisResult
{
    // Internal mutable collections for reuse
    private readonly List<TileMovement> _movements;
    private readonly HashSet<int> _spawnedIndices;
    private readonly HashSet<int> _mergedIndices;
    private readonly HashSet<int> _movedToIndices;

    /// <summary>
    /// List of all tile movements with source and destination positions.
    /// </summary>
    public IReadOnlyList<TileMovement> Movements => _movements;

    /// <summary>
    /// Board indices where new tiles spawned.
    /// </summary>
    public IReadOnlySet<int> SpawnedIndices => _spawnedIndices;

    /// <summary>
    /// Board indices where tiles merged.
    /// </summary>
    public IReadOnlySet<int> MergedIndices => _mergedIndices;

    /// <summary>
    /// Board indices where tiles moved to (non-merge destinations).
    /// </summary>
    public IReadOnlySet<int> MovedToIndices => _movedToIndices;

    /// <summary>
    /// Creates a new reusable result with pre-allocated capacity for a given board size.
    /// </summary>
    /// <param name="boardSize">The board size (e.g., 4 for a 4x4 board).</param>
    public MoveAnalysisResult(int boardSize = 4)
    {
        var maxTiles = boardSize * boardSize;
        _movements = new List<TileMovement>(maxTiles);
        _spawnedIndices = new HashSet<int>(maxTiles);
        _mergedIndices = new HashSet<int>(maxTiles);
        _movedToIndices = new HashSet<int>(maxTiles);
    }

    /// <summary>
    /// Clears all collections to prepare for reuse.
    /// </summary>
    public void Clear()
    {
        _movements.Clear();
        _spawnedIndices.Clear();
        _mergedIndices.Clear();
        _movedToIndices.Clear();
    }

    /// <summary>
    /// Adds a tile movement to the result.
    /// </summary>
    internal void AddMovement(TileMovement movement) => _movements.Add(movement);

    /// <summary>
    /// Marks an index as having a spawned tile.
    /// </summary>
    internal void AddSpawnedIndex(int index) => _spawnedIndices.Add(index);

    /// <summary>
    /// Marks an index as having a merged tile.
    /// </summary>
    internal void AddMergedIndex(int index) => _mergedIndices.Add(index);

    /// <summary>
    /// Marks an index as a move destination (non-merge).
    /// </summary>
    internal void AddMovedToIndex(int index) => _movedToIndices.Add(index);
}
