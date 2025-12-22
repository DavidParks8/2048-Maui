namespace TwentyFortyEight.Core;

/// <summary>
/// Data transfer object for serializing/deserializing game state.
/// JSON-friendly representation with versioning for future compatibility.
/// </summary>
public class GameStateDto
{
    /// <summary>
    /// Version of the serialization format.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// The game board as a flat array.
    /// </summary>
    public int[] Board { get; set; } = Array.Empty<int>();

    /// <summary>
    /// The size of the board.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// The current score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// The number of moves made.
    /// </summary>
    public int MoveCount { get; set; }

    /// <summary>
    /// Whether the game has been won.
    /// </summary>
    public bool IsWon { get; set; }

    /// <summary>
    /// Whether the game is over.
    /// </summary>
    public bool IsGameOver { get; set; }

    /// <summary>
    /// Creates a DTO from a GameState.
    /// </summary>
    public static GameStateDto FromGameState(GameState state)
    {
        return new GameStateDto
        {
            Board = (int[])state.Board.Clone(),
            Size = state.Size,
            Score = state.Score,
            MoveCount = state.MoveCount,
            IsWon = state.IsWon,
            IsGameOver = state.IsGameOver,
        };
    }

    /// <summary>
    /// Creates a GameState from this DTO.
    /// </summary>
    public GameState ToGameState()
    {
        return new GameState((int[])Board.Clone(), Size, Score, MoveCount, IsWon, IsGameOver);
    }
}
