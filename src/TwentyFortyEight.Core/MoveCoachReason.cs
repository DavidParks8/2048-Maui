namespace TwentyFortyEight.Core;

/// <summary>
/// A platform-agnostic reason for why a move was recommended.
/// UI layers should map these to localized strings.
/// </summary>
public enum MoveCoachReason
{
    /// <summary>
    /// The move creates additional empty space.
    /// </summary>
    CreateSpace,

    /// <summary>
    /// The move merges tiles and increases score.
    /// </summary>
    MergeTiles,

    /// <summary>
    /// The move keeps the largest tile in a corner.
    /// </summary>
    KeepLargestInCorner,

    /// <summary>
    /// The move improves board monotonicity / ordering.
    /// </summary>
    ImproveOrder,

    /// <summary>
    /// The move avoids an immediate dead-end.
    /// </summary>
    AvoidDeadEnd,
}
