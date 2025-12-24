namespace TwentyFortyEight.Core;

/// <summary>
/// Data transfer object for serializing/deserializing game state.
/// JSON-friendly representation with versioning for future compatibility.
/// </summary>
public class GameStateDto
{
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
            Board = state.Board.ToArray(),
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
        var board = new Board(Board, Size);
        var maxTileValue = Board.Length > 0 ? Board.Max() : 0;
        return new GameState(board, Score, MoveCount, IsWon, IsGameOver, maxTileValue);
    }
}
