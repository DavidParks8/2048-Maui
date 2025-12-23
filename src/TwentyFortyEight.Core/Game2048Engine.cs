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
    private readonly IGameStatisticsTracker? _statisticsTracker;
    private readonly List<MoveRecord> _moveHistory;
    private int _currentMoveIndex;
    private GameState _initialState;
    private bool _hasReachedWinningTile;

    private GameState _currentState;

    public GameState CurrentState => _currentState;

    public bool CanUndo => _currentMoveIndex > 0;

    public Game2048Engine(
        GameConfig config,
        IRandomSource random,
        IGameStatisticsTracker? statisticsTracker = null
    )
    {
        _config = config;
        _random = random;
        _statisticsTracker = statisticsTracker;
        _moveHistory = [];
        _currentMoveIndex = 0;
        _currentState = new GameState(_config.Size);

        // Start with two random tiles
        SpawnTileWithInfo();
        SpawnTileWithInfo();

        _initialState = _currentState;

        // Notify about game start
        _statisticsTracker?.OnGameStarted();
    }

    /// <summary>
    /// Creates a new game engine from a saved state.
    /// </summary>
    public Game2048Engine(
        GameState state,
        GameConfig config,
        IRandomSource random,
        IGameStatisticsTracker? statisticsTracker = null
    )
    {
        _config = config;
        _random = random;
        _statisticsTracker = statisticsTracker;
        _moveHistory = [];
        _currentMoveIndex = 0;
        _currentState = state;
        _initialState = state;
        _hasReachedWinningTile = state.IsWon;
    }

    /// <summary>
    /// Starts a new game.
    /// </summary>
    public void NewGame()
    {
        // Check if previous game ended without winning
        if (!_hasReachedWinningTile && _currentState.MoveCount > 0)
        {
            _statisticsTracker?.OnGameOver(_currentState.Score, false);
        }

        _moveHistory.Clear();
        _currentMoveIndex = 0;
        _currentState = new GameState(_config.Size);

        // Start with two random tiles
        SpawnTileWithInfo();
        SpawnTileWithInfo();

        _initialState = _currentState;

        // Notify about game start
        _statisticsTracker?.OnGameStarted();
    }

    /// <summary>
    /// Performs a move in the specified direction.
    /// Returns true if the board changed, false if it was a no-op.
    /// </summary>
    public bool Move(Direction direction)
    {
        var (newBoard, scoreIncrease, boardChanged, maxMergedValue) = ProcessMove(
            _currentState.Board,
            direction
        );

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

        // Update state - track the new max tile value
        var newScore = _currentState.Score + scoreIncrease;
        var newMoveCount = _currentState.MoveCount + 1;
        var newMaxTile = Math.Max(_currentState.MaxTileValue, maxMergedValue);
        var isWon = _currentState.IsWon || newMaxTile >= _config.WinTile;

        _currentState = new GameState(newBoard, newScore, newMoveCount, isWon, false, newMaxTile);

        // Spawn a new tile and record it
        var (spawnIndex, spawnValue) = SpawnTileWithInfo();
        MoveRecord moveRecord = new(direction, spawnIndex, spawnValue);

        _moveHistory.Add(moveRecord);
        _currentMoveIndex++;

        // Check if won for the first time
        if (isWon && !_hasReachedWinningTile)
        {
            _hasReachedWinningTile = true;
            _statisticsTracker?.OnGameWon();
        }

        // Get highest tile on board
        int highestTile = 0;
        for (int i = 0; i < newBoard.Length; i++)
        {
            if (newBoard[i] > highestTile)
            {
                highestTile = newBoard[i];
            }
        }

        // Notify about move
        _statisticsTracker?.OnMoveMade(newScore, highestTile);

        // Check if game is over
        if (IsGameOver())
        {
            _currentState = _currentState.WithUpdate(isGameOver: true);
            _statisticsTracker?.OnGameOver(newScore, _hasReachedWinningTile);
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

            var (newBoard, scoreIncrease, _, maxMergedValue) = ProcessMove(
                _currentState.Board,
                move.Direction
            );

            var newScore = _currentState.Score + scoreIncrease;
            var newMoveCount = _currentState.MoveCount + 1;
            var newMaxTile = Math.Max(_currentState.MaxTileValue, maxMergedValue);
            var isWon = _currentState.IsWon || newMaxTile >= _config.WinTile;

            _currentState = new GameState(
                newBoard,
                newScore,
                newMoveCount,
                isWon,
                false,
                newMaxTile
            );

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

        // Get adaptive spawn values based on tracked max tile
        var (commonValue, rareValue) = GetSpawnValues(_currentState.MaxTileValue);
        var value = _random.NextDouble() < 0.9 ? commonValue : rareValue;

        _currentState = _currentState.WithTile(position.Value.Row, position.Value.Column, value);

        var index = _currentState.Board.GetIndex(position.Value.Row, position.Value.Column);
        return (index, value);
    }

    /// <summary>
    /// Gets the spawn values based on the current maximum tile on the board.
    /// Spawn values scale up as the game progresses to keep the game playable at high scores.
    /// </summary>
    private static (int commonValue, int rareValue) GetSpawnValues(int maxTileOnBoard) =>
        maxTileOnBoard switch
        {
            >= 131072 => (32, 64), // 2^17: spawn 32 (90%) or 64 (10%)
            >= 32768 => (16, 32), // 2^15: spawn 16 (90%) or 32 (10%)
            >= 8192 => (8, 16), // 2^13: spawn 8 (90%) or 16 (10%)
            >= 2048 => (4, 8), // 2^11: spawn 4 (90%) or 8 (10%)
            _ => (2, 4), // Default: spawn 2 (90%) or 4 (10%)
        };

    private bool IsGameOver()
    {
        var board = _currentState.Board;

        // Game is not over if there are empty cells or possible merges
        return board.CountEmptyCells() == 0 && !board.HasPossibleMerges();
    }

    private static (Board newBoard, int scoreIncrease, bool moved, int maxMergedValue) ProcessMove(
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
            _ => (board, 0, false, 0),
        };
    }

    private static (
        Board newBoard,
        int scoreIncrease,
        bool moved,
        int maxMergedValue
    ) ProcessMoveGeneric(Board board, bool isVertical, bool isReverse)
    {
        var size = board.Size;
        var result = new int[size, size];
        var moved = false;
        var scoreIncrease = 0;
        var maxMergedValue = 0;

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
                        if (mergedValue > maxMergedValue)
                            maxMergedValue = mergedValue;
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

        return (Board.FromMutableArray(result, size), scoreIncrease, moved, maxMergedValue);
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
