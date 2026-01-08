namespace TwentyFortyEight.Core;

/// <summary>
/// Request data for producing a move recommendation.
/// </summary>
public readonly record struct MoveAdvisorRequest(PlayfieldSnapshot Playfield, GameConfig Config);
