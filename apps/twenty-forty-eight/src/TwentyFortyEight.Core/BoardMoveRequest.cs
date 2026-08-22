namespace TwentyFortyEight.Core;

/// <summary>
/// Request data for simulating a move on a board without mutating game state.
/// </summary>
public readonly record struct BoardMoveRequest(PlayfieldSnapshot Playfield, Direction Direction);
