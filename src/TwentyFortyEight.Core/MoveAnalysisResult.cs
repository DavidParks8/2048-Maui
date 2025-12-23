namespace TwentyFortyEight.Core;

/// <summary>
/// Represents the result of analyzing a move, including tile movements and categorizations.
/// </summary>
public sealed class MoveAnalysisResult
{
    /// <summary>
    /// List of all tile movements with source and destination positions.
    /// </summary>
    public IReadOnlyList<TileMovement> Movements { get; }

    /// <summary>
    /// Board indices where new tiles spawned.
    /// </summary>
    public IReadOnlySet<int> SpawnedIndices { get; }

    /// <summary>
    /// Board indices where tiles merged.
    /// </summary>
    public IReadOnlySet<int> MergedIndices { get; }

    /// <summary>
    /// Board indices where tiles moved to (non-merge destinations).
    /// </summary>
    public IReadOnlySet<int> MovedToIndices { get; }

    public MoveAnalysisResult(
        IReadOnlyList<TileMovement> movements,
        IReadOnlySet<int> spawnedIndices,
        IReadOnlySet<int> mergedIndices,
        IReadOnlySet<int> movedToIndices
    )
    {
        Movements = movements;
        SpawnedIndices = spawnedIndices;
        MergedIndices = mergedIndices;
        MovedToIndices = movedToIndices;
    }
}
