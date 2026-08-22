namespace TwentyFortyEight.Core;

/// <summary>
/// Core engine for the 2048 game with undo support.
/// </summary>
public class Game2048Engine
{
    private readonly GameConfig _config;
    private readonly IRandomSource _random;
    private readonly IStatisticsTracker _statisticsTracker;
    private readonly List<MoveRecord> _moveHistory;
    private readonly IBoardSimulator _boardSimulator;
    private readonly ISpawnStrategy _spawnStrategy;
    private int _currentMoveIndex;
    private GameState _initialState;

    private int _pendingExternalSpawnIndex = -1;
    private int _pendingExternalSpawnValue;

    private GameState _currentState;

    /// <summary>
    /// Event raised when the player achieves victory for the first time in this game.
    /// Only fires once per game, even if the player continues to reach higher tiles.
    /// </summary>
    public event EventHandler? VictoryAchieved;

    // Latch: ensures the event is raised once per game session even if the user undoes to
    // a pre-victory state and reaches the win tile again.
    private bool _victoryEventRaised;

    // Tracks the total number of undos performed in the current game session.
    private int _undoCount;

    public GameState CurrentState => _currentState;

    public bool CanUndo => _currentMoveIndex > 0;

    /// <summary>
    /// Gets the total number of undos performed in the current game session.
    /// </summary>
    public int UndoCount => _undoCount;

    /// <summary>
    /// Returns a JSON-friendly snapshot of the current game session.
    /// Includes full undo/redo history for the duration of the game.
    /// </summary>
    public GameSave ToSaveDto()
    {
        return new GameSave
        {
            InitialState = GameStateDto.FromGameState(_initialState),
            MoveHistory = [.. _moveHistory],
            CurrentMoveIndex = _currentMoveIndex,
            VictoryEventRaised = _victoryEventRaised,
            UndoCount = _undoCount,
        };
    }

    public Game2048Engine(
        GameConfig config,
        IRandomSource random,
        IStatisticsTracker statisticsTracker,
        IBoardSimulator boardSimulator,
        ISpawnStrategyFactory spawnStrategyFactory
    )
    {
        _config = config;
        _random = random;
        _statisticsTracker = statisticsTracker;
        _moveHistory = [];
        _currentMoveIndex = 0;
        _currentState = new GameState(_config.Size);
        _boardSimulator = boardSimulator;
        _spawnStrategy = spawnStrategyFactory.Create(_config);

        // Start with two random tiles
        SpawnTileWithInfo();
        SpawnTileWithInfo();

        _initialState = _currentState;

        // Track initial game start
        _statisticsTracker.OnGameStarted();
    }

    /// <summary>
    /// Creates a new game engine from a saved state.
    /// </summary>
    public Game2048Engine(
        GameState state,
        GameConfig config,
        IRandomSource random,
        IStatisticsTracker statisticsTracker,
        IBoardSimulator boardSimulator,
        ISpawnStrategyFactory spawnStrategyFactory
    )
    {
        _config = config;
        _random = random;
        _statisticsTracker = statisticsTracker;
        _moveHistory = [];
        _currentMoveIndex = 0;
        _currentState = state;
        _initialState = state;
        _boardSimulator = boardSimulator;
        _spawnStrategy = spawnStrategyFactory.Create(_config);
    }

    /// <summary>
    /// Creates a new game engine from a persisted save.
    /// </summary>
    public Game2048Engine(
        GameSave save,
        GameConfig config,
        IRandomSource random,
        IStatisticsTracker statisticsTracker,
        IBoardSimulator boardSimulator,
        ISpawnStrategyFactory spawnStrategyFactory
    )
    {
        ArgumentNullException.ThrowIfNull(save);

        _config = config;
        _random = random;
        _statisticsTracker = statisticsTracker;
        _boardSimulator = boardSimulator;
        _spawnStrategy = spawnStrategyFactory.Create(_config);

        _currentState = new GameState(_config.Size);

        _moveHistory = save.MoveHistory?.ToList() ?? [];
        _currentMoveIndex = Math.Clamp(save.CurrentMoveIndex, 0, _moveHistory.Count);

        _initialState = save.InitialState?.ToGameState() ?? new GameState(_config.Size);
        _victoryEventRaised = save.VictoryEventRaised;
        _undoCount = save.UndoCount;

        ReplayToCurrentIndex();
    }

    /// <summary>
    /// Starts a new game.
    /// </summary>
    public void NewGame()
    {
        // End previous game if it wasn't finished
        if (!_currentState.IsGameOver)
        {
            _statisticsTracker.OnGameEnded(_currentState.Score, _currentState.IsWon);
        }

        _victoryEventRaised = false;
        _undoCount = 0;

        _moveHistory.Clear();
        _currentMoveIndex = 0;
        _currentState = new GameState(_config.Size);

        _pendingExternalSpawnIndex = -1;
        _pendingExternalSpawnValue = 0;

        // Start with two random tiles
        SpawnTileWithInfo();
        SpawnTileWithInfo();

        _initialState = _currentState;

        // Track new game start
        _statisticsTracker.OnGameStarted();
    }

    /// <summary>
    /// Performs a move in the specified direction.
    /// Returns true if the board changed, false if it was a no-op.
    /// </summary>
    public bool Move(Direction direction)
    {
        // In Adversarial mode, moves are made by the AI after a player-controlled spawn.
        // If no player spawn is pending, treat Move as a no-op.
        if (_config.Mode == GameMode.Adversarial && _pendingExternalSpawnIndex < 0)
        {
            if (!_currentState.IsGameOver && IsGameOver())
            {
                FinalizeAdversarialPlayerWinByLockingAi();
            }
            return false;
        }

        var playfield = new PlayfieldSnapshot(_currentState.Board, _currentState.Wall);
        var (newBoard, scoreIncrease, boardChanged, maxMergedValue) = _boardSimulator.SimulateMove(
            new BoardMoveRequest(playfield, direction)
        );

        if (!boardChanged)
        {
            // Check if game is over (no moves possible in any direction)
            if (!_currentState.IsGameOver && IsGameOver())
            {
                if (_config.Mode == GameMode.Adversarial)
                {
                    FinalizeAdversarialPlayerWinByLockingAi();
                }
                else
                {
                    _currentState = _currentState.WithUpdate(isGameOver: true);
                    _statisticsTracker.OnGameEnded(_currentState.Score, _currentState.IsWon);
                }
            }

            // In Adversarial mode, we still want to record the player-controlled spawn as a
            // committed turn so Undo works even when the AI has no valid moves.
            if (_config.Mode == GameMode.Adversarial && _pendingExternalSpawnIndex >= 0)
            {
                RecordMove(
                    direction,
                    spawnedTileIndex: -1,
                    spawnedTileValue: 0,
                    wallAfterMove: null
                );
                return true;
            }

            return false;
        }

        // Clear any redo moves
        if (_currentMoveIndex < _moveHistory.Count)
        {
            _moveHistory.RemoveRange(_currentMoveIndex, _moveHistory.Count - _currentMoveIndex);
        }

        // Update state - track the new max tile value
        // Adversarial mode still uses the standard 2048 scoring rules (points increase on merges),
        // but the objective is inverted (lower final score is better).
        var newScore = _currentState.Score + scoreIncrease;
        var newMoveCount = _currentState.MoveCount + 1;
        var newMaxTile = Math.Max(_currentState.MaxTileValue, maxMergedValue);
        var wasWonBefore = _currentState.IsWon;
        var reachedWinTile = newMaxTile >= _config.WinTile;
        var isWon =
            _config.Mode == GameMode.Adversarial ? wasWonBefore : wasWonBefore || reachedWinTile;

        // In Adversarial mode, reaching the win tile is a LOSS (AI reached 2048).
        // End the game immediately.
        var isGameOver = false;
        if (_config.Mode == GameMode.Adversarial && reachedWinTile)
        {
            isGameOver = true;
            isWon = false;
        }

        _currentState = new GameState(
            newBoard,
            newScore,
            newMoveCount,
            isWon,
            isGameOver,
            newMaxTile
        );

        // Track statistics
        _statisticsTracker.OnMoveMade();
        _statisticsTracker.UpdateHighestTile(newMaxTile);
        _statisticsTracker.UpdateBestScore(_config.Mode, newScore);

        // Check if game was just won
        if (isWon && !wasWonBefore)
        {
            _statisticsTracker.OnGameWon();

            // Raise victory event once per game (even if Undo rewinds IsWon)
            if (!_victoryEventRaised)
            {
                _victoryEventRaised = true;

                VictoryAchieved?.Invoke(this, EventArgs.Empty);
            }
        }

        // Spawn a new tile and record it (non-adversarial modes)
        int spawnIndex = -1;
        int spawnValue = 0;
        if (_config.Mode != GameMode.Adversarial)
        {
            (spawnIndex, spawnValue) = SpawnTileWithInfo();
        }

        WallSegment? wallAfterMove = null;
        if (_config.Mode == GameMode.Walltastrophy)
        {
            wallAfterMove = CreateRandomWallSegment(_currentState.Size);
            _currentState = _currentState.WithWall(wallAfterMove);
        }

        RecordMove(direction, spawnIndex, spawnValue, wallAfterMove);

        // Check if game is over
        if (!_currentState.IsGameOver && IsGameOver())
        {
            if (_config.Mode == GameMode.Adversarial)
            {
                FinalizeAdversarialPlayerWinByLockingAi();
            }
            else
            {
                _currentState = _currentState.WithUpdate(isGameOver: true);
                _statisticsTracker.OnGameEnded(_currentState.Score, _currentState.IsWon);
            }
        }

        // If the AI reached the win tile (Adversarial loss), finalize stats now.
        if (_currentState.IsGameOver && _config.Mode == GameMode.Adversarial && reachedWinTile)
        {
            _statisticsTracker.OnGameEnded(_currentState.Score, _currentState.IsWon);
        }

        return true;
    }

    private void FinalizeAdversarialPlayerWinByLockingAi()
    {
        // Winning in Adversarial mode ends the game.
        // We also raise the victory event so the UI can show the victory overlay
        // (instead of the generic game-over dialog).
        _currentState = _currentState.WithUpdate(isGameOver: true, isWon: true);

        _statisticsTracker.OnGameWon();

        // Raise victory event once per game (even if Undo rewinds IsWon)
        if (!_victoryEventRaised)
        {
            _victoryEventRaised = true;
            VictoryAchieved?.Invoke(this, EventArgs.Empty);
        }

        _statisticsTracker.OnGameEnded(_currentState.Score, _currentState.IsWon);
    }

    /// <summary>
    /// In Adversarial mode, the player places the next tile at a chosen empty cell.
    /// This does not increment move count and is recorded on the next AI move for Undo.
    /// </summary>
    public bool TrySpawnExternalTile(Position position, out int spawnedValue)
    {
        spawnedValue = 0;

        if (_config.Mode != GameMode.Adversarial)
        {
            return false;
        }

        if (_currentState.IsGameOver)
        {
            return false;
        }

        if (
            (uint)position.Row >= (uint)_currentState.Size
            || (uint)position.Column >= (uint)_currentState.Size
        )
        {
            return false;
        }

        // Only allow one pending spawn per AI turn.
        if (_pendingExternalSpawnIndex >= 0)
        {
            return false;
        }

        if (_currentState.Board[position.Row, position.Column] != 0)
        {
            return false;
        }

        spawnedValue = _spawnStrategy.GetSpawnValue(_currentState, _config);
        _currentState = _currentState.WithTile(position.Row, position.Column, spawnedValue);

        _pendingExternalSpawnIndex = _currentState.Board.GetIndex(position.Row, position.Column);
        _pendingExternalSpawnValue = spawnedValue;
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
        _undoCount++;
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
            _initialState.IsGameOver,
            _initialState.MaxTileValue,
            _initialState.Wall
        );

        // Replay moves up to currentMoveIndex
        for (int i = 0; i < _currentMoveIndex; i++)
        {
            var move = _moveHistory[i];

            _currentState = ApplyRecordedMove(_currentState, move);
        }

        // Check if game is over
        if (IsGameOver())
        {
            if (
                _config.Mode == GameMode.Adversarial
                && _currentState.MaxTileValue < _config.WinTile
            )
            {
                _currentState = _currentState.WithUpdate(isGameOver: true, isWon: true);
            }
            else
            {
                _currentState = _currentState.WithUpdate(isGameOver: true);
            }
        }
    }

    private GameState ApplyRecordedMove(GameState state, MoveRecord move)
    {
        // Apply any player-controlled spawn first (Adversarial).
        if (move.ExternalSpawnedTileIndex >= 0)
        {
            var spawnRow = move.ExternalSpawnedTileIndex / state.Size;
            var spawnCol = move.ExternalSpawnedTileIndex % state.Size;
            state = state.WithTile(spawnRow, spawnCol, move.ExternalSpawnedTileValue);
        }

        var playfield = new PlayfieldSnapshot(state.Board, state.Wall);
        var (newBoard, scoreIncrease, moved, maxMergedValue) = _boardSimulator.SimulateMove(
            new BoardMoveRequest(playfield, move.Direction)
        );

        if (!moved)
        {
            // No movement, but the external spawn may have still changed the board.
            return state;
        }

        var newScore = state.Score + scoreIncrease;
        var newMoveCount = state.MoveCount + 1;
        var newMaxTile = Math.Max(state.MaxTileValue, maxMergedValue);
        var reachedWinTile = newMaxTile >= _config.WinTile;
        var isWon =
            _config.Mode == GameMode.Adversarial ? state.IsWon : state.IsWon || reachedWinTile;

        var isGameOver = false;
        if (_config.Mode == GameMode.Adversarial && reachedWinTile)
        {
            isGameOver = true;
            isWon = false;
        }

        var updated = new GameState(
            newBoard,
            newScore,
            newMoveCount,
            isWon,
            isGameOver,
            newMaxTile
        );

        if (move.SpawnedTileIndex >= 0)
        {
            var row = move.SpawnedTileIndex / updated.Size;
            var col = move.SpawnedTileIndex % updated.Size;
            updated = updated.WithTile(row, col, move.SpawnedTileValue);
        }

        if (_config.Mode == GameMode.Walltastrophy)
        {
            updated = updated.WithWall(move.WallAfterMove);
        }

        return updated;
    }

    private void RecordMove(
        Direction direction,
        int spawnedTileIndex,
        int spawnedTileValue,
        WallSegment? wallAfterMove
    )
    {
        var externalIndex = _pendingExternalSpawnIndex;
        var externalValue = _pendingExternalSpawnValue;

        _pendingExternalSpawnIndex = -1;
        _pendingExternalSpawnValue = 0;

        MoveRecord moveRecord = new(
            direction,
            spawnedTileIndex,
            spawnedTileValue,
            wallAfterMove,
            externalIndex,
            externalValue
        );

        _moveHistory.Add(moveRecord);
        _currentMoveIndex++;
    }

    private (int index, int value) SpawnTileWithInfo()
    {
        var position = _currentState.Board.GetRandomEmptyCell(_random);

        if (!position.HasValue)
        {
            return (-1, 0);
        }

        var value = _spawnStrategy.GetSpawnValue(_currentState, _config);

        _currentState = _currentState.WithTile(position.Value.Row, position.Value.Column, value);

        var index = _currentState.Board.GetIndex(position.Value.Row, position.Value.Column);
        return (index, value);
    }

    private bool IsGameOver()
    {
        var board = _currentState.Board;
        var playfield = new PlayfieldSnapshot(board, _currentState.Wall);

        if (_config.Mode == GameMode.Walltastrophy)
        {
            // Walls can eliminate moves even when empties exist, so probe all directions.
            return !_boardSimulator.WouldMove(new BoardMoveRequest(playfield, Direction.Up))
                && !_boardSimulator.WouldMove(new BoardMoveRequest(playfield, Direction.Down))
                && !_boardSimulator.WouldMove(new BoardMoveRequest(playfield, Direction.Left))
                && !_boardSimulator.WouldMove(new BoardMoveRequest(playfield, Direction.Right));
        }

        // Non-walltastrophy modes: game is not over if there are empty cells or possible merges.
        return board.CountEmptyCells() == 0 && !board.HasPossibleMerges();
    }

    private WallSegment? CreateRandomWallSegment(int size)
    {
        if (size < 2)
        {
            return null;
        }

        var orientation = (WallOrientation)_random.Next(2);
        int divider = _random.Next(size - 1);
        int start = _random.Next(size);
        int maxLength = size - start;
        int length = 1 + _random.Next(maxLength);

        var wall = new WallSegment(orientation, divider, start, length);
        return wall.IsValidForSize(size) ? wall : null;
    }
}
