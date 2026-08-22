namespace TwentyFortyEight.Core;

/// <summary>
/// A suggested next move with a heuristic score.
/// </summary>
public readonly record struct MoveRecommendation(
    Direction Direction,
    double Score,
    MoveCoachReason PrimaryReason
);
