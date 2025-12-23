namespace TwentyFortyEight.Core;

/// <summary>
/// Represents the current state of a 2048 game.
/// This is designed to be immutable/snapshot-friendly for undo/redo functionality.
/// </summary>
/// <param name="Board">The game board.</param>
/// <param name="Score">The current score.</param>
/// <param name="MoveCount">The number of moves made.</param>
/// <param name="IsWon">Whether the game has been won (a tile reached WinTile).</param>
/// <param name="IsGameOver">Whether the game is over (no valid moves remaining).</param>
public record GameState(Board Board, int Score, int MoveCount, bool IsWon, bool IsGameOver)
{
    /// <summary>
    /// Gets the size of the board (e.g., 4 for a 4x4 board).
    /// </summary>
    public int Size => Board.Size;

    /// <summary>
    /// Creates a new empty game state with the specified board size.
    /// </summary>
    /// <param name="size">The size of the board (e.g., 4 for a 4x4 board).</param>
    public GameState(int size)
        : this(new Board(size), Score: 0, MoveCount: 0, IsWon: false, IsGameOver: false) { }

    /// <summary>
    /// Legacy constructor for compatibility - creates a Board from int[].
    /// </summary>
    public GameState(int[] board, int size, int score, int moveCount, bool isWon, bool isGameOver)
        : this(new Board(board, size), score, moveCount, isWon, isGameOver) { }

    /// <summary>
    /// Gets the tile value at the specified position.
    /// </summary>
    public int GetTile(int row, int col) => Board[row, col];

    /// <summary>
    /// Creates a new GameState with the tile at the specified position set to the given value.
    /// </summary>
    public GameState WithTile(int row, int col, int value) =>
        this with
        {
            Board = Board.WithTile(row, col, value),
        };

    /// <summary>
    /// Creates a new GameState with updated properties.
    /// </summary>
    public GameState WithUpdate(
        Board? board = null,
        int? score = null,
        int? moveCount = null,
        bool? isWon = null,
        bool? isGameOver = null
    ) =>
        this with
        {
            Board = board ?? Board,
            Score = score ?? Score,
            MoveCount = moveCount ?? MoveCount,
            IsWon = isWon ?? IsWon,
            IsGameOver = isGameOver ?? IsGameOver,
        };
}
