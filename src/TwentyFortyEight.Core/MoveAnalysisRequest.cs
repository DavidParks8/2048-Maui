namespace TwentyFortyEight.Core;

/// <summary>
/// Request data for analyzing a move by comparing previous and next board states.
/// </summary>
public readonly record struct MoveAnalysisRequest(
    PlayfieldSnapshot Previous,
    Board NewBoard,
    Direction Direction
);
