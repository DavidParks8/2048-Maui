namespace TwentyFortyEight.Core;

/// <summary>
/// Core engine for the 2048 game with undo/redo support.
/// </summary>
public class Game2048Engine
{
    private readonly GameConfig _config;
    private readonly IRandomSource _random;
    private readonly List<MoveCommand> _moveHistory;
    private int _currentMoveIndex;
    private GameState _initialState;

    private GameState _currentState;

    public GameState CurrentState => _currentState;

    public bool CanUndo => _currentMoveIndex > 0;
    public bool CanRedo => _currentMoveIndex < _moveHistory.Count;

    public Game2048Engine(GameConfig config, IRandomSource random)
    {
        _config = config;
        _random = random;
        _moveHistory = new List<MoveCommand>();
        _currentMoveIndex = 0;
        _currentState = new GameState(_config.Size);
        
        // Start with two random tiles
        SpawnTile();
        SpawnTile();
        
        _initialState = _currentState;
    }

    /// <summary>
    /// Creates a new game engine from a saved state.
    /// </summary>
    public Game2048Engine(GameState state, GameConfig config, IRandomSource random)
    {
        _config = config;
        _random = random;
        _moveHistory = new List<MoveCommand>();
        _currentMoveIndex = 0;
        _currentState = state;
        _initialState = state;
    }

    /// <summary>
    /// Starts a new game.
    /// </summary>
    public void NewGame()
    {
        _moveHistory.Clear();
        _currentMoveIndex = 0;
        _currentState = new GameState(_config.Size);
        
        // Start with two random tiles
        SpawnTile();
        SpawnTile();
        
        _initialState = _currentState;
    }

    /// <summary>
    /// Performs a move in the specified direction.
    /// Returns true if the board changed, false if it was a no-op.
    /// </summary>
    public bool Move(Direction direction)
    {
        var newBoard = (int[])_currentState.Board.Clone();
        var scoreIncrease = 0;
        var boardChanged = ProcessMove(newBoard, _currentState.Size, direction, ref scoreIncrease);

        if (!boardChanged)
        {
            return false;
        }

        // Clear any redo moves
        if (_currentMoveIndex < _moveHistory.Count)
        {
            _moveHistory.RemoveRange(_currentMoveIndex, _moveHistory.Count - _currentMoveIndex);
        }

        // Update state
        var newScore = _currentState.Score + scoreIncrease;
        var newMoveCount = _currentState.MoveCount + 1;
        var isWon = _currentState.IsWon || HasWinningTile(newBoard);

        _currentState = new GameState(newBoard, _currentState.Size, newScore, newMoveCount, isWon, false);

        // Create and save move command
        var moveCommand = new MoveCommand(direction);
        
        // Spawn a new tile and record it
        var (spawnIndex, spawnValue) = SpawnTileWithInfo();
        moveCommand.SpawnedTileIndex = spawnIndex;
        moveCommand.SpawnedTileValue = spawnValue;
        
        _moveHistory.Add(moveCommand);
        _currentMoveIndex++;

        // Check if game is over
        if (IsGameOver())
        {
            _currentState = _currentState.WithUpdate(isGameOver: true);
        }

        return true;
    }

    /// <summary>
    /// Undoes the last move by replaying from initial state.
    /// </summary>
    public bool Undo()
    {
        if (_currentMoveIndex == 0)
        {
            return false;
        }

        _currentMoveIndex--;
        ReplayToCurrentIndex();
        return true;
    }

    /// <summary>
    /// Redoes the last undone move by replaying one more move.
    /// </summary>
    public bool Redo()
    {
        if (_currentMoveIndex >= _moveHistory.Count)
        {
            return false;
        }

        _currentMoveIndex++;
        ReplayToCurrentIndex();
        return true;
    }

    private void ReplayToCurrentIndex()
    {
        // Start from initial state (always starts at score 0, move 0, not won, not over)
        _currentState = new GameState(
            (int[])_initialState.Board.Clone(),
            _initialState.Size,
            _initialState.Score,
            _initialState.MoveCount,
            _initialState.IsWon,
            _initialState.IsGameOver);

        // Replay moves up to currentMoveIndex
        for (int i = 0; i < _currentMoveIndex; i++)
        {
            var move = _moveHistory[i];
            
            var newBoard = (int[])_currentState.Board.Clone();
            var scoreIncrease = 0;
            ProcessMove(newBoard, _currentState.Size, move.Direction, ref scoreIncrease);

            var newScore = _currentState.Score + scoreIncrease;
            var newMoveCount = _currentState.MoveCount + 1;
            var isWon = _currentState.IsWon || HasWinningTile(newBoard);

            _currentState = new GameState(newBoard, _currentState.Size, newScore, newMoveCount, isWon, false);

            // Restore the spawned tile
            if (move.SpawnedTileIndex >= 0)
            {
                var row = move.SpawnedTileIndex / _currentState.Size;
                var col = move.SpawnedTileIndex % _currentState.Size;
                _currentState = _currentState.WithTile(row, col, move.SpawnedTileValue);
            }
        }

        // Check if game is over
        if (IsGameOver())
        {
            _currentState = _currentState.WithUpdate(isGameOver: true);
        }
    }

    private void SpawnTile()
    {
        SpawnTileWithInfo();
    }

    private (int index, int value) SpawnTileWithInfo()
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
            return (-1, 0);
        }

        var index = emptyCells[_random.Next(emptyCells.Count)];
        var value = _random.NextDouble() < 0.9 ? 2 : 4;

        var row = index / _currentState.Size;
        var col = index % _currentState.Size;
        _currentState = _currentState.WithTile(row, col, value);
        
        return (index, value);
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

    private bool ProcessMove(int[] board, int size, Direction direction, ref int scoreIncrease)
    {
        return direction switch
        {
            Direction.Up => ProcessMoveGeneric(board, size, true, false, ref scoreIncrease),
            Direction.Down => ProcessMoveGeneric(board, size, true, true, ref scoreIncrease),
            Direction.Left => ProcessMoveGeneric(board, size, false, false, ref scoreIncrease),
            Direction.Right => ProcessMoveGeneric(board, size, false, true, ref scoreIncrease),
            _ => false
        };
    }

    private bool ProcessMoveGeneric(int[] board, int size, bool isVertical, bool isReverse, ref int scoreIncrease)
    {
        var moved = false;
        var outerCount = size;
        
        for (int outer = 0; outer < outerCount; outer++)
        {
            var values = new List<int>();
            var merged = new HashSet<int>();

            // Collect non-zero values
            for (int inner = 0; inner < size; inner++)
            {
                var index = GetBoardIndex(size, outer, inner, isVertical, isReverse);
                
                var value = board[index];
                if (value != 0)
                {
                    values.Add(value);
                }
            }

            // Merge tiles
            var newValues = new List<int>();
            for (int i = 0; i < values.Count; i++)
            {
                if (i < values.Count - 1 && values[i] == values[i + 1] && !merged.Contains(i))
                {
                    var mergedValue = values[i] * 2;
                    newValues.Add(mergedValue);
                    scoreIncrease += mergedValue;
                    merged.Add(i);
                    merged.Add(i + 1);
                    i++; // Skip next tile
                }
                else if (!merged.Contains(i))
                {
                    newValues.Add(values[i]);
                }
            }

            // Fill with zeros
            while (newValues.Count < size)
            {
                newValues.Add(0);
            }

            // Update board and check if changed
            for (int inner = 0; inner < size; inner++)
            {
                var index = GetBoardIndex(size, outer, inner, isVertical, isReverse);
                
                if (board[index] != newValues[inner])
                {
                    moved = true;
                }
                board[index] = newValues[inner];
            }
        }

        return moved;
    }

    private static int GetBoardIndex(int size, int outer, int inner, bool isVertical, bool isReverse)
    {
        return isVertical
            ? (isReverse ? (size - 1 - inner) * size + outer : inner * size + outer)
            : (isReverse ? outer * size + (size - 1 - inner) : outer * size + inner);
    }
}
