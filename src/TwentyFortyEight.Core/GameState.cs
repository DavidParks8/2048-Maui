namespace TwentyFortyEight.Core;

/// <summary>
/// Represents the current state of a 2048 game.
/// This is designed to be immutable/snapshot-friendly for undo/redo functionality.
/// </summary>
public class GameState
{
    /// <summary>
    /// The game board as a flat array (size * size elements).
    /// </summary>
    public int[] Board { get; }

    /// <summary>
    /// The size of the board (e.g., 4 for a 4x4 board).
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// The current score.
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// The number of moves made.
    /// </summary>
    public int MoveCount { get; }

    /// <summary>
    /// Whether the game has been won (a tile reached WinTile).
    /// </summary>
    public bool IsWon { get; }

    /// <summary>
    /// Whether the game is over (no valid moves remaining).
    /// </summary>
    public bool IsGameOver { get; }

    public GameState(int size)
    {
        Size = size;
        Board = new int[size * size];
        Score = 0;
        MoveCount = 0;
        IsWon = false;
        IsGameOver = false;
    }

    public GameState(int[] board, int size, int score, int moveCount, bool isWon, bool isGameOver)
    {
        Board = (int[])board.Clone();
        Size = size;
        Score = score;
        MoveCount = moveCount;
        IsWon = isWon;
        IsGameOver = isGameOver;
    }

    /// <summary>
    /// Gets the tile value at the specified position.
    /// </summary>
    public int GetTile(int row, int col)
    {
        return Board[row * Size + col];
    }

    /// <summary>
    /// Creates a new GameState with the tile at the specified position set to the given value.
    /// </summary>
    public GameState WithTile(int row, int col, int value)
    {
        var newBoard = (int[])Board.Clone();
        newBoard[row * Size + col] = value;
        return new GameState(newBoard, Size, Score, MoveCount, IsWon, IsGameOver);
    }

    /// <summary>
    /// Creates a new GameState with updated properties.
    /// </summary>
    public GameState WithUpdate(int[]? board = null, int? score = null, int? moveCount = null, 
        bool? isWon = null, bool? isGameOver = null)
    {
        return new GameState(
            board ?? (int[])Board.Clone(),
            Size,
            score ?? Score,
            moveCount ?? MoveCount,
            isWon ?? IsWon,
            isGameOver ?? IsGameOver
        );
    }
}
