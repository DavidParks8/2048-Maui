using Microsoft.Extensions.ObjectPool;

namespace TwentyFortyEight.Core;

/// <summary>
/// Core engine for the 2048 game with undo support.
/// </summary>
public class Game2048Engine
{
    // Object pools for ProcessMoveGeneric to avoid allocations per move
    private static readonly ObjectPool<List<int>> s_intListPool = ObjectPool.Create(
        new IntListPooledObjectPolicy()
    );

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
        _moveHistory = [];
        _currentMoveIndex = 0;
        _currentState = new GameState(_config.Size);

        // Start with two random tiles
        SpawnTileWithInfo();
        SpawnTileWithInfo();

        _initialState = _currentState;
    }

    /// <summary>
    /// Creates a new game engine from a saved state.
    /// </summary>
    public Game2048Engine(GameState state, GameConfig config, IRandomSource random)
    {
        _config = config;
        _random = random;
        _moveHistory = [];
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
        SpawnTileWithInfo();
        SpawnTileWithInfo();

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
        MoveRecord moveRecord = new(direction, spawnIndex, spawnValue);

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

    private (int index, int value) SpawnTileWithInfo()
    {
        var position = _currentState.Board.GetRandomEmptyCell(_random);

        if (!position.HasValue)
        {
            return (-1, 0);
        }

        var value = _random.NextDouble() < 0.9 ? 2 : 4;

        _currentState = _currentState.WithTile(position.Value.Row, position.Value.Column, value);

        var index = _currentState.Board.GetIndex(position.Value.Row, position.Value.Column);
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

        // Rent pooled lists to avoid allocations
        var values = s_intListPool.Get();
        var newValues = s_intListPool.Get();

        try
        {
            for (int outer = 0; outer < size; outer++)
            {
                values.Clear();

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

                // Merge tiles - using index tracking instead of HashSet
                newValues.Clear();
                int i = 0;
                while (i < values.Count)
                {
                    if (i < values.Count - 1 && values[i] == values[i + 1])
                    {
                        var mergedValue = values[i] * 2;
                        newValues.Add(mergedValue);
                        scoreIncrease += mergedValue;
                        i += 2; // Skip both merged tiles
                    }
                    else
                    {
                        newValues.Add(values[i]);
                        i++;
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
        }
        finally
        {
            s_intListPool.Return(values);
            s_intListPool.Return(newValues);
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

/// <summary>
/// Pooled object policy for List&lt;int&gt;.
/// </summary>
file sealed class IntListPooledObjectPolicy : PooledObjectPolicy<List<int>>
{
    public override List<int> Create() => new(8);

    public override bool Return(List<int> obj)
    {
        obj.Clear();
        return true;
    }
}
