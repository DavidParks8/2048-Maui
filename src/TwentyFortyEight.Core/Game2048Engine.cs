namespace TwentyFortyEight.Core;

/// <summary>
/// Core engine for the 2048 game with undo/redo support.
/// </summary>
public class Game2048Engine
{
    private readonly GameConfig _config;
    private readonly IRandomSource _random;
    private readonly Stack<GameState> _undoStack;
    private readonly Stack<GameState> _redoStack;
    private const int MaxHistorySize = 50;

    private GameState _currentState;

    public GameState CurrentState => _currentState;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public Game2048Engine(GameConfig? config = null, IRandomSource? random = null)
    {
        _config = config ?? new GameConfig();
        _random = random ?? new SystemRandomSource();
        _undoStack = new Stack<GameState>();
        _redoStack = new Stack<GameState>();
        _currentState = new GameState(_config.Size);
        
        // Start with two random tiles
        SpawnTile();
        SpawnTile();
    }

    /// <summary>
    /// Creates a new game engine from a saved state.
    /// </summary>
    public Game2048Engine(GameState state, GameConfig? config = null, IRandomSource? random = null)
    {
        _config = config ?? new GameConfig();
        _random = random ?? new SystemRandomSource();
        _undoStack = new Stack<GameState>();
        _redoStack = new Stack<GameState>();
        _currentState = state;
    }

    /// <summary>
    /// Starts a new game.
    /// </summary>
    public void NewGame()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _currentState = new GameState(_config.Size);
        
        // Start with two random tiles
        SpawnTile();
        SpawnTile();
    }

    /// <summary>
    /// Performs a move in the specified direction.
    /// Returns true if the board changed, false if it was a no-op.
    /// </summary>
    public bool Move(Direction direction)
    {
        var newBoard = (int[])_currentState.Board.Clone();
        var scoreIncrease = 0;
        var boardChanged = false;

        switch (direction)
        {
            case Direction.Up:
                boardChanged = MoveUp(newBoard, _currentState.Size, ref scoreIncrease);
                break;
            case Direction.Down:
                boardChanged = MoveDown(newBoard, _currentState.Size, ref scoreIncrease);
                break;
            case Direction.Left:
                boardChanged = MoveLeft(newBoard, _currentState.Size, ref scoreIncrease);
                break;
            case Direction.Right:
                boardChanged = MoveRight(newBoard, _currentState.Size, ref scoreIncrease);
                break;
        }

        if (!boardChanged)
        {
            return false;
        }

        // Save current state to undo stack
        _undoStack.Push(_currentState);
        if (_undoStack.Count > MaxHistorySize)
        {
            // Remove oldest state
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = temp.Length - 1; i >= 0 && _undoStack.Count < MaxHistorySize; i--)
            {
                _undoStack.Push(temp[i]);
            }
        }

        // Clear redo stack on new move
        _redoStack.Clear();

        // Update state
        var newScore = _currentState.Score + scoreIncrease;
        var newMoveCount = _currentState.MoveCount + 1;
        var isWon = _currentState.IsWon || HasWinningTile(newBoard);

        _currentState = new GameState(newBoard, _currentState.Size, newScore, newMoveCount, isWon, false);

        // Spawn a new tile
        SpawnTile();

        // Check if game is over
        if (IsGameOver())
        {
            _currentState = _currentState.WithUpdate(isGameOver: true);
        }

        return true;
    }

    /// <summary>
    /// Undoes the last move.
    /// </summary>
    public bool Undo()
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        _redoStack.Push(_currentState);
        if (_redoStack.Count > MaxHistorySize)
        {
            // Remove oldest state
            var temp = _redoStack.ToArray();
            _redoStack.Clear();
            for (int i = temp.Length - 1; i >= 0 && _redoStack.Count < MaxHistorySize; i--)
            {
                _redoStack.Push(temp[i]);
            }
        }

        _currentState = _undoStack.Pop();
        return true;
    }

    /// <summary>
    /// Redoes the last undone move.
    /// </summary>
    public bool Redo()
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }

        _undoStack.Push(_currentState);
        if (_undoStack.Count > MaxHistorySize)
        {
            // Remove oldest state
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = temp.Length - 1; i >= 0 && _undoStack.Count < MaxHistorySize; i--)
            {
                _undoStack.Push(temp[i]);
            }
        }

        _currentState = _redoStack.Pop();
        return true;
    }

    private void SpawnTile()
    {
        var emptyCells = new List<int>();
        for (int i = 0; i < _currentState.Board.Length; i++)
        {
            if (_currentState.Board[i] == 0)
            {
                emptyCells.Add(i);
            }
        }

        if (emptyCells.Count == 0)
        {
            return;
        }

        var index = emptyCells[_random.Next(emptyCells.Count)];
        var value = _random.NextDouble() < 0.9 ? 2 : 4;

        var row = index / _currentState.Size;
        var col = index % _currentState.Size;
        _currentState = _currentState.WithTile(row, col, value);
    }

    private bool HasWinningTile(int[] board)
    {
        foreach (var tile in board)
        {
            if (tile >= _config.WinTile)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsGameOver()
    {
        var size = _currentState.Size;
        var board = _currentState.Board;

        // Check for empty cells
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
            {
                return false;
            }
        }

        // Check for possible merges
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                var current = board[row * size + col];

                // Check right
                if (col < size - 1 && current == board[row * size + col + 1])
                {
                    return false;
                }

                // Check down
                if (row < size - 1 && current == board[(row + 1) * size + col])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool MoveLeft(int[] board, int size, ref int scoreIncrease)
    {
        var moved = false;

        for (int row = 0; row < size; row++)
        {
            var rowValues = new List<int>();
            var merged = new HashSet<int>();

            // Collect non-zero values
            for (int col = 0; col < size; col++)
            {
                var value = board[row * size + col];
                if (value != 0)
                {
                    rowValues.Add(value);
                }
            }

            // Merge tiles
            var newRow = new List<int>();
            for (int i = 0; i < rowValues.Count; i++)
            {
                if (i < rowValues.Count - 1 && rowValues[i] == rowValues[i + 1] && !merged.Contains(i))
                {
                    var mergedValue = rowValues[i] * 2;
                    newRow.Add(mergedValue);
                    scoreIncrease += mergedValue;
                    merged.Add(i);
                    merged.Add(i + 1);
                    i++; // Skip next tile
                }
                else if (!merged.Contains(i))
                {
                    newRow.Add(rowValues[i]);
                }
            }

            // Fill with zeros
            while (newRow.Count < size)
            {
                newRow.Add(0);
            }

            // Update board and check if changed
            for (int col = 0; col < size; col++)
            {
                if (board[row * size + col] != newRow[col])
                {
                    moved = true;
                }
                board[row * size + col] = newRow[col];
            }
        }

        return moved;
    }

    private bool MoveRight(int[] board, int size, ref int scoreIncrease)
    {
        var moved = false;

        for (int row = 0; row < size; row++)
        {
            var rowValues = new List<int>();
            var merged = new HashSet<int>();

            // Collect non-zero values (right to left)
            for (int col = size - 1; col >= 0; col--)
            {
                var value = board[row * size + col];
                if (value != 0)
                {
                    rowValues.Add(value);
                }
            }

            // Merge tiles
            var newRow = new List<int>();
            for (int i = 0; i < rowValues.Count; i++)
            {
                if (i < rowValues.Count - 1 && rowValues[i] == rowValues[i + 1] && !merged.Contains(i))
                {
                    var mergedValue = rowValues[i] * 2;
                    newRow.Add(mergedValue);
                    scoreIncrease += mergedValue;
                    merged.Add(i);
                    merged.Add(i + 1);
                    i++; // Skip next tile
                }
                else if (!merged.Contains(i))
                {
                    newRow.Add(rowValues[i]);
                }
            }

            // Fill with zeros
            while (newRow.Count < size)
            {
                newRow.Add(0);
            }

            // Update board (right to left) and check if changed
            for (int col = 0; col < size; col++)
            {
                var newValue = newRow[col];
                var boardCol = size - 1 - col;
                if (board[row * size + boardCol] != newValue)
                {
                    moved = true;
                }
                board[row * size + boardCol] = newValue;
            }
        }

        return moved;
    }

    private bool MoveUp(int[] board, int size, ref int scoreIncrease)
    {
        var moved = false;

        for (int col = 0; col < size; col++)
        {
            var colValues = new List<int>();
            var merged = new HashSet<int>();

            // Collect non-zero values
            for (int row = 0; row < size; row++)
            {
                var value = board[row * size + col];
                if (value != 0)
                {
                    colValues.Add(value);
                }
            }

            // Merge tiles
            var newCol = new List<int>();
            for (int i = 0; i < colValues.Count; i++)
            {
                if (i < colValues.Count - 1 && colValues[i] == colValues[i + 1] && !merged.Contains(i))
                {
                    var mergedValue = colValues[i] * 2;
                    newCol.Add(mergedValue);
                    scoreIncrease += mergedValue;
                    merged.Add(i);
                    merged.Add(i + 1);
                    i++; // Skip next tile
                }
                else if (!merged.Contains(i))
                {
                    newCol.Add(colValues[i]);
                }
            }

            // Fill with zeros
            while (newCol.Count < size)
            {
                newCol.Add(0);
            }

            // Update board and check if changed
            for (int row = 0; row < size; row++)
            {
                if (board[row * size + col] != newCol[row])
                {
                    moved = true;
                }
                board[row * size + col] = newCol[row];
            }
        }

        return moved;
    }

    private bool MoveDown(int[] board, int size, ref int scoreIncrease)
    {
        var moved = false;

        for (int col = 0; col < size; col++)
        {
            var colValues = new List<int>();
            var merged = new HashSet<int>();

            // Collect non-zero values (bottom to top)
            for (int row = size - 1; row >= 0; row--)
            {
                var value = board[row * size + col];
                if (value != 0)
                {
                    colValues.Add(value);
                }
            }

            // Merge tiles
            var newCol = new List<int>();
            for (int i = 0; i < colValues.Count; i++)
            {
                if (i < colValues.Count - 1 && colValues[i] == colValues[i + 1] && !merged.Contains(i))
                {
                    var mergedValue = colValues[i] * 2;
                    newCol.Add(mergedValue);
                    scoreIncrease += mergedValue;
                    merged.Add(i);
                    merged.Add(i + 1);
                    i++; // Skip next tile
                }
                else if (!merged.Contains(i))
                {
                    newCol.Add(colValues[i]);
                }
            }

            // Fill with zeros
            while (newCol.Count < size)
            {
                newCol.Add(0);
            }

            // Update board (bottom to top) and check if changed
            for (int row = 0; row < size; row++)
            {
                var newValue = newCol[row];
                var boardRow = size - 1 - row;
                if (board[boardRow * size + col] != newValue)
                {
                    moved = true;
                }
                board[boardRow * size + col] = newValue;
            }
        }

        return moved;
    }
}
