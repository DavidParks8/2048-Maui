namespace TwentyFortyEight.Core;

/// <summary>
/// Request data for producing a move recommendation.
/// </summary>
public readonly record struct MoveAdvisorRequest(
    Board Board,
    GameConfig Config,
    WallSegment? Wall = null
);
