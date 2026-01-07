namespace TwentyFortyEight.Core;

/// <summary>
/// Request data for analyzing a move by comparing previous and next board states.
/// </summary>
public readonly record struct MoveAnalysisRequest(
    Board PreviousBoard,
    Board NewBoard,
    Direction Direction,
    WallSegment? Wall = null
);
