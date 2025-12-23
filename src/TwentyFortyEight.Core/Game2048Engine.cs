namespace TwentyFortyEight.Core;

/// <summary>
/// Core engine for the 2048 game with undo support.
/// </summary>
public class Game2048Engine
{
    private readonly GameConfig _config;
    private readonly IRandomSource _random;
    private readonly List<MoveRecord> _moveHistory;
    private int _currentMoveIndex;
    private GameState _initialState;

    private GameState _currentState;

    public GameState CurrentState => _currentState;

    public bool CanUndo => _currentMoveIndex > 0;

    public Game2048Engine(GameConfig config, IRandomSource random)
    {
        _config = config;
        _random = random;
        _moveHistory = new List<MoveRecord>();
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
        _moveHistory = new List<MoveRecord>();
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
        var (newBoard, scoreIncrease, boardChanged) = ProcessMove(_currentState.Board, direction);

        if (!boardChanged)
        {
            // Check if game is over (no moves possible in any direction)
            if (!_currentState.IsGameOver && IsGameOver())
            {
                _currentState = _currentState.WithUpdate(isGameOver: true);
            }
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
        var isWon = _currentState.IsWon || newBoard.ContainsAtLeast(_config.WinTile);

        _currentState = new GameState(newBoard, newScore, newMoveCount, isWon, false);

        // Spawn a new tile and record it
        var (spawnIndex, spawnValue) = SpawnTileWithInfo();
        MoveRecord moveRecord = new(direction)
        {
            SpawnedTileIndex = spawnIndex,
            SpawnedTileValue = spawnValue,
        };

        _moveHistory.Add(moveRecord);
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

    private void ReplayToCurrentIndex()
    {
        // Start from initial state (always starts at score 0, move 0, not won, not over)
        _currentState = new GameState(
            _initialState.Board.Clone(),
            _initialState.Score,
            _initialState.MoveCount,
            _initialState.IsWon,
            _initialState.IsGameOver
        );

        // Replay moves up to currentMoveIndex
        for (int i = 0; i < _currentMoveIndex; i++)
        {
            var move = _moveHistory[i];

            var (newBoard, scoreIncrease, _) = ProcessMove(_currentState.Board, move.Direction);

            var newScore = _currentState.Score + scoreIncrease;
            var newMoveCount = _currentState.MoveCount + 1;
            var isWon = _currentState.IsWon || newBoard.ContainsAtLeast(_config.WinTile);

            _currentState = new GameState(newBoard, newScore, newMoveCount, isWon, false);

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
        var emptyCells = _currentState.Board.FindEmptyCells();

        if (emptyCells.Count == 0)
        {
            return (-1, 0);
        }

        var position = emptyCells[_random.Next(emptyCells.Count)];
        var value = _random.NextDouble() < 0.9 ? 2 : 4;

        _currentState = _currentState.WithTile(position.Row, position.Column, value);

        var index = _currentState.Board.GetIndex(position.Row, position.Column);
        return (index, value);
    }

    private bool IsGameOver()
    {
        var board = _currentState.Board;

        // Game is not over if there are empty cells or possible merges
        return board.CountEmptyCells() == 0 && !board.HasPossibleMerges();
    }

    private static (Board newBoard, int scoreIncrease, bool moved) ProcessMove(
        Board board,
        Direction direction
    )
    {
        return direction switch
        {
            Direction.Up => ProcessMoveGeneric(board, isVertical: true, isReverse: false),
            Direction.Down => ProcessMoveGeneric(board, isVertical: true, isReverse: true),
            Direction.Left => ProcessMoveGeneric(board, isVertical: false, isReverse: false),
            Direction.Right => ProcessMoveGeneric(board, isVertical: false, isReverse: true),
            _ => (board, 0, false),
        };
    }

    private static (Board newBoard, int scoreIncrease, bool moved) ProcessMoveGeneric(
        Board board,
        bool isVertical,
        bool isReverse
    )
    {
        var size = board.Size;
        var result = new int[size, size];
        var moved = false;
        var scoreIncrease = 0;

        for (int outer = 0; outer < size; outer++)
        {
            var values = new List<int>();

            // Collect non-zero values from board
            for (int inner = 0; inner < size; inner++)
            {
                var (row, col) = GetBoardPosition(size, outer, inner, isVertical, isReverse);
                var value = board[row, col];
                if (value != 0)
                {
                    values.Add(value);
                }
            }

            // Merge tiles
            var newValues = new List<int>();
            var merged = new HashSet<int>();
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

            // Write to result and check if changed
            for (int inner = 0; inner < size; inner++)
            {
                var (row, col) = GetBoardPosition(size, outer, inner, isVertical, isReverse);
                result[row, col] = newValues[inner];
                if (board[row, col] != newValues[inner])
                {
                    moved = true;
                }
            }
        }

        return (Board.FromMutableArray(result, size), scoreIncrease, moved);
    }

    private static (int row, int col) GetBoardPosition(
        int size,
        int outer,
        int inner,
        bool isVertical,
        bool isReverse
    )
    {
        if (isVertical)
        {
            var row = isReverse ? size - 1 - inner : inner;
            return (row, outer);
        }
        else
        {
            var col = isReverse ? size - 1 - inner : inner;
            return (outer, col);
        }
    }
}
