using System.Collections.Frozen;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Messages;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels;

/// <summary>
/// ViewModel for the 2048 game.
/// </summary>
public partial class GameViewModel : ObservableObject
{
    private const int DefaultBoardSize = 4;

    private bool _isNewGameConfirmationInProgress;

    private GameConfig _config;
    private readonly ILogger<GameViewModel> _logger;
    private readonly IMoveAnalyzer _moveAnalyzer;
    private readonly IBoardSimulator _boardSimulator;
    private readonly ISettingsService _settingsService;
    private readonly IStatisticsTracker _statisticsTracker;
    private readonly IGame2048EngineFactory _engineFactory;
    private readonly IGameStateRepository _repository;
    private readonly IGameSessionCoordinator _sessionCoordinator;
    private readonly IUserFeedbackService _feedbackService;
    private readonly VictoryViewModel _victoryViewModel;
    private readonly ICoachNudgeService _coachNudgeService;
    private readonly ICoachSuggestionService _coachSuggestionService;
    private Game2048Engine _engine;

    /// <summary>
    /// Semaphore to prevent concurrent move processing.
    /// Ensures only one move is processed at a time to avoid broken board state from fast swiping.
    /// </summary>
    private readonly SemaphoreSlim _moveLock = new(1, 1);

    /// <summary>
    /// Flag to track if initialization is complete to prevent screen reader announcements during startup.
    /// </summary>
    private bool _isInitialized = false;

    /// <summary>
    /// The collection of tiles for the game board.
    /// </summary>
    public ObservableCollection<TileViewModel> Tiles { get; }

    /// <summary>
    /// Event raised when tiles are updated and need animations.
    /// </summary>
    public event EventHandler<TileUpdateEventArgs>? TilesUpdated;

    /// <summary>
    /// Event raised when victory animation should play.
    /// Forwarded from the Core engine's VictoryAchieved event.
    /// </summary>
    public event EventHandler? VictoryAnimationRequested;

    [ObservableProperty]
    private int _score;

    partial void OnScoreChanged(int value)
    {
        // Don't announce during initialization to avoid NullReferenceException
        // when MAUI's SemanticScreenReader isn't fully initialized yet
        if (!_isInitialized || value <= 0)
        {
            return;
        }

        // Use feedback service for announcements
        _feedbackService.AnnounceScoreIfSignificant(value, value - 10);
    }

    [ObservableProperty]
    private int _bestScore;

    [ObservableProperty]
    private int _pendingBoardSize;

    [ObservableProperty]
    private GameMode _pendingGameMode;

    /// <summary>
    /// Gets the board size for UI layout calculations.
    /// </summary>
    public int BoardSize => _config.Size;

    /// <summary>
    /// Gets the active game mode.
    /// </summary>
    public GameMode GameMode => _config.Mode;

    /// <summary>
    /// Gets the current between-cell wall segment (Walltastrophy), or null.
    /// </summary>
    public WallSegment? Wall => _engine.CurrentState.Wall;

    /// <summary>
    /// Gets the total number of undos performed in the current game session.
    /// </summary>
    public int UndoCount => _engine.UndoCount;

    [ObservableProperty]
    private double _boardScaleFactor = 1.0;

    [ObservableProperty]
    private int _moves;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _isSocialGamingAvailable;

    [ObservableProperty]
    private bool _isCoachEnabled;

    [ObservableProperty]
    private Direction? _coachSuggestedDirection;

    [ObservableProperty]
    private MoveCoachReason? _coachPrimaryReason;

    [ObservableProperty]
    private bool _isCoachSuggestionVisible;

    [ObservableProperty]
    private int _coachMoveCounter;

    [ObservableProperty]
    private bool _isCoachNudgeVisible;

    [ObservableProperty]
    private bool _isUndoButtonVisible;

    public GameViewModel(
        ILogger<GameViewModel> logger,
        IMoveAnalyzer moveAnalyzer,
        IBoardSimulator boardSimulator,
        ISettingsService settingsService,
        IStatisticsTracker statisticsTracker,
        IGame2048EngineFactory engineFactory,
        IGameStateRepository repository,
        IGameSessionCoordinator sessionCoordinator,
        IUserFeedbackService feedbackService,
        VictoryViewModel victoryViewModel,
        ICoachNudgeService coachNudgeService,
        ICoachSuggestionService coachSuggestionService
    )
    {
        _logger = logger;
        _moveAnalyzer = moveAnalyzer;
        _boardSimulator = boardSimulator;
        _settingsService = settingsService;
        _statisticsTracker = statisticsTracker;
        _engineFactory = engineFactory;
        _repository = repository;
        _sessionCoordinator = sessionCoordinator;
        _feedbackService = feedbackService;
        _victoryViewModel = victoryViewModel;
        _coachNudgeService = coachNudgeService;
        _coachSuggestionService = coachSuggestionService;

        IsCoachEnabled = _settingsService.CoachEnabled;
        IsUndoButtonVisible = _settingsService.UndoButtonVisible;

        WeakReferenceMessenger.Default.Register<BoardSizeChangeRequestedMessage>(
            this,
            static (recipient, message) =>
            {
                if (recipient is GameViewModel vm)
                {
                    _ = vm.ApplyBoardSizeChangeRequestAsyncSafe(message.NewSize);
                }
            }
        );

        WeakReferenceMessenger.Default.Register<CoachEnabledChangedMessage>(
            this,
            static (recipient, message) =>
            {
                if (recipient is GameViewModel vm)
                {
                    vm.IsCoachEnabled = message.IsEnabled;
                    vm.UpdateCoachSuggestion();
                }
            }
        );

        WeakReferenceMessenger.Default.Register<UndoButtonVisibilityChangedMessage>(
            this,
            static (recipient, message) =>
            {
                if (recipient is GameViewModel vm)
                {
                    vm.IsUndoButtonVisible = message.IsVisible;
                }
            }
        );

        var lastConfig = _settingsService.LastActiveGameConfig;

        _config = lastConfig;
        PendingBoardSize = _config.Size;
        PendingGameMode = _config.Mode;
        _engine = _engineFactory.Create(_config);
        _engine.VictoryAchieved += OnEngineVictoryAchieved;

        // Initialize tiles collection (4x4 grid = 16 tiles)
        Tiles = [];
        for (int row = 0; row < _config.Size; row++)
        {
            for (int col = 0; col < _config.Size; col++)
            {
                Tiles.Add(new TileViewModel { Row = row, Column = col });
            }
        }

        // Load saved state or start new game
        LoadGame();
        UpdateUI();

        // Check social gaming availability
        IsSocialGamingAvailable = _sessionCoordinator.IsSocialGamingAvailable;

        // Mark initialization complete - now safe to announce to screen readers
        _isInitialized = true;

        // Subscribe to theme changes to update tile colors
        Application.Current?.RequestedThemeChanged += OnAppThemeChanged;
    }

    [RelayCommand]
    private void ToggleCoach()
    {
        IsCoachEnabled = !IsCoachEnabled;
        _settingsService.CoachEnabled = IsCoachEnabled;

        _coachNudgeService.Dismiss();
        IsCoachNudgeVisible = false;

        if (IsCoachEnabled)
        {
            UpdateCoachSuggestion();
        }
        else
        {
            ClearCoachSuggestion();
        }
    }

    [RelayCommand]
    private void DismissCoachNudge()
    {
        _coachNudgeService.Dismiss();
        IsCoachNudgeVisible = false;
    }

    [RelayCommand]
    private void EnableCoachFromNudge()
    {
        IsCoachEnabled = true;
        _settingsService.CoachEnabled = true;
        _coachNudgeService.Dismiss();
        IsCoachNudgeVisible = false;
        UpdateCoachSuggestion();
    }

    private void OnAppThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        // Refresh all tiles to update their colors based on the new theme
        foreach (var tile in Tiles)
        {
            // Force update of color properties
            tile.RefreshColors();
        }
    }

    [RelayCommand]
    private async Task NewGameAsync()
    {
        // Confirm if a game is in progress (has moves and not game over).
        if (Moves > 0 && !_engine.CurrentState.IsGameOver)
        {
            _isNewGameConfirmationInProgress = true;
            try
            {
                bool confirmed = await _feedbackService.ConfirmNewGameAsync();
                if (!confirmed)
                {
                    return;
                }
            }
            finally
            {
                _isNewGameConfirmationInProgress = false;
            }
        }

        StartNewGame();
    }

    private void StartNewGame()
    {
        // Hide victory overlay if it's showing
        _victoryViewModel.HideVictoryOverlayIfShowing();

        _coachNudgeService.Reset();
        IsCoachNudgeVisible = false;

        _engine.NewGame();
        UpdateUI();
        _repository.SaveGame(_config, _engine.ToSaveDto());
    }

    [RelayCommand]
    private Task ShowHowToPlay()
    {
        return _feedbackService.ShowHowToPlayAsync();
    }

    [RelayCommand]
    private Task MoveAsync(Direction direction)
    {
        return PerformMoveAsync(direction, skipSlideAnimation: false);
    }

    /// <summary>
    /// Commits a move that was already visually scrubbed via swipe preview.
    /// The UI can skip the slide and only run post-move effects.
    /// </summary>
    public Task CommitSwipePreviewMoveAsync(Direction direction)
    {
        return PerformMoveAsync(direction, skipSlideAnimation: true);
    }

    /// <summary>
    /// Creates a non-committing move preview used to drive scrubbable swipe animations.
    /// Returns false when the move would not change the board.
    /// </summary>
    public bool TryCreateMovePreview(Direction direction, out MovePreview preview)
    {
        var state = _engine.CurrentState;
        PlayfieldSnapshot playfield = new(state.Board, state.Wall);

        var (newBoard, _, moved, _) = _boardSimulator.SimulateMove(
            new BoardMoveRequest(playfield, direction)
        );

        if (!moved)
        {
            preview = null!;
            return false;
        }

        var analysis = _moveAnalyzer.Analyze(
            new MoveAnalysisRequest(playfield, newBoard, direction)
        );

        // IMPORTANT: Copy the movements list because analysis.Movements is a pooled
        // reference that gets cleared on the next Analyze() call.
        List<TileMovement> movementsCopy = [.. analysis.Movements];
        if (movementsCopy.Count == 0)
        {
            preview = null!;
            return false;
        }

        preview = new MovePreview { Direction = direction, TileMovements = movementsCopy };
        return true;
    }

    private async Task PerformMoveAsync(Direction direction, bool skipSlideAnimation)
    {
        if (_isNewGameConfirmationInProgress)
        {
            return;
        }

        // Use non-blocking Wait(0) to check if we can acquire the lock immediately
        // If not, another move is in progress - skip this one
        if (!_moveLock.Wait(0))
        {
            return;
        }

        try
        {
            // Capture previous state before the move
            var previousBoard = _engine.CurrentState.Board.Clone();
            var previousWall = _engine.CurrentState.Wall;
            var previousScore = Score;

            var moved = _engine.Move(direction);
            if (moved)
            {
                _coachNudgeService.Reset();
                IsCoachNudgeVisible = false;

                CoachMoveCounter++;

                // Trigger haptic feedback if enabled and supported
                _feedbackService.PerformMoveHaptic();

                UpdateUI(previousBoard, direction, previousWall, skipSlideAnimation);
                _repository.SaveGame(_config, _engine.ToSaveDto());

                // Update best score and submit to social gaming service
                bool isNewBest = Score > BestScore;
                if (isNewBest)
                {
                    BestScore = Score;
                    _repository.UpdateBestScoreIfHigher(_config, Score);
                }

                // Wait for the slide duration to block input, ensuring the game feels responsive
                // but prevents rapid-fire moves that could break the animation state.
                await Task.Delay(GetInputBlockDuration());

                // Check and report achievements and scores
                await _sessionCoordinator.OnMoveCompletedAsync(_engine.CurrentState);
                await _sessionCoordinator.OnScoreChangedAsync(Score, isNewBest, _config);
            }
            else
            {
                _coachNudgeService.TrackInvalidMove();

                if (!_engine.CurrentState.IsGameOver && _coachNudgeService.ShouldShowNudge())
                {
                    IsCoachNudgeVisible = true;
                    _feedbackService.AnnounceCoachNudge();
                }
            }
        }
        finally
        {
            _moveLock.Release();
        }
    }

    private TimeSpan GetInputBlockDuration()
    {
        // Only block input during the slide.
        // This makes the game feel responsive even if animations overlap.
        // The MAUI animation system automatically respects OS accessibility settings
        // (like reduced motion on iOS/Android) and will skip or shorten animations appropriately.
        var durationMs = AnimationConstants.BaseSlideAnimationDuration;

        // Add a tiny buffer (e.g. 10ms) to ensure the UI thread has picked up the change
        return TimeSpan.FromMilliseconds(durationMs + 10);
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_isNewGameConfirmationInProgress)
        {
            return;
        }

        if (_engine.Undo())
        {
            UpdateUI();
            _repository.SaveGame(_config, _engine.ToSaveDto());
        }
    }

    [RelayCommand]
    private void OpenStats()
    {
        StrongReferenceMessenger.Default.Send(new NavigateToStatsMessage());
    }

    [RelayCommand]
    private void OpenSettings()
    {
        StrongReferenceMessenger.Default.Send(new NavigateToSettingsMessage());
    }

    [RelayCommand]
    private void OpenAbout()
    {
        StrongReferenceMessenger.Default.Send(new NavigateToAboutMessage());
    }

    private void UpdateUI(
        Board? previousBoard = null,
        Direction? moveDirection = null,
        WallSegment? previousWall = null,
        bool skipSlideAnimation = false
    )
    {
        var state = _engine.CurrentState;

        if (previousBoard != null && moveDirection != null)
        {
            // Use Core MoveAnalyzer for all movement and categorization logic
            var analysis = _moveAnalyzer.Analyze(
                new MoveAnalysisRequest(
                    new PlayfieldSnapshot(previousBoard.Value, previousWall),
                    state.Board,
                    moveDirection.Value
                )
            );

            HashSet<TileViewModel> movedTiles = [];
            HashSet<TileViewModel> newTiles = [];
            HashSet<TileViewModel> mergedTiles = [];

            for (int i = 0; i < state.Board.Length; i++)
            {
                var tile = Tiles[i];
                var newValue = state.Board[i];

                // Reset animation flags
                tile.IsNewTile = false;
                tile.IsMerged = false;

                // Categorize tile based on analysis results
                if (analysis.SpawnedIndices.Contains(i))
                {
                    tile.IsNewTile = true;
                    newTiles.Add(tile);
                }
                else if (analysis.MergedIndices.Contains(i))
                {
                    tile.IsMerged = true;
                    mergedTiles.Add(tile);
                }
                else if (analysis.MovedToIndices.Contains(i))
                {
                    movedTiles.Add(tile);
                }

                tile.Value = newValue;
            }

            // Create event args with frozen collections if there are changes
            if (
                movedTiles.Count > 0
                || newTiles.Count > 0
                || mergedTiles.Count > 0
                || analysis.Movements.Count > 0
            )
            {
                // IMPORTANT: Copy the movements list because analysis.Movements is a pooled
                // reference that gets cleared on the next Analyze() call.
                List<TileMovement> movementsCopy = analysis.Movements.ToList();

                TileUpdateEventArgs eventArgs = new()
                {
                    MovedTiles = movedTiles.ToFrozenSet(),
                    NewTiles = newTiles.ToFrozenSet(),
                    MergedTiles = mergedTiles.ToFrozenSet(),
                    MoveDirection = moveDirection.Value,
                    TileMovements = movementsCopy,
                    SkipSlideAnimation = skipSlideAnimation,
                    WallAfterMove = state.Wall,
                };

                TilesUpdated?.Invoke(this, eventArgs);
            }
        }
        else
        {
            // No previous board - just update values
            for (int i = 0; i < state.Board.Length; i++)
            {
                Tiles[i].Value = state.Board[i];
            }
        }

        // Update properties
        Score = state.Score;
        Moves = state.MoveCount;
        CanUndo = _engine.CanUndo;

        // Wall may change on moves/undo or initial load.
        OnPropertyChanged(nameof(Wall));
        // UndoCount may change on undo.
        OnPropertyChanged(nameof(UndoCount));

        // Handle game over state
        if (state.IsGameOver)
        {
            // Don't announce during initialization
            if (_isInitialized)
            {
                _feedbackService.AnnounceGameOver(Score);
                // Show game over dialog asynchronously (fire and forget)
                _ = ShowGameOverDialogAsync();
            }
        }

        // Refresh command can execute states
        UndoCommand.NotifyCanExecuteChanged();

        UpdateCoachSuggestion();
    }

    private void UpdateCoachSuggestion()
    {
        var state = _engine.CurrentState;
        var recommendation = _coachSuggestionService.GetSuggestion(
            new CoachSuggestionRequest(
                state.Board,
                _config,
                IsCoachEnabled,
                state.IsGameOver,
                state.Wall
            )
        );

        if (recommendation is null)
        {
            ClearCoachSuggestion();
            return;
        }

        CoachSuggestedDirection = recommendation.Value.Direction;
        CoachPrimaryReason = recommendation.Value.PrimaryReason;
        IsCoachSuggestionVisible = true;
    }

    private void ClearCoachSuggestion()
    {
        CoachSuggestedDirection = null;
        CoachPrimaryReason = null;
        IsCoachSuggestionVisible = false;
    }

    private void LoadGame()
    {
        try
        {
            // Load best score from repository
            BestScore = _repository.GetBestScore(_config);

            // Try to load saved game
            var save = _repository.LoadGame(_config);
            if (save != null)
            {
                // IMPORTANT: Unsubscribe before replacing engine to prevent leaks/double firing.
                _engine.VictoryAchieved -= OnEngineVictoryAchieved;

                _engine = _engineFactory.Create(save, _config);
                _engine.VictoryAchieved += OnEngineVictoryAchieved;
                return;
            }
        }
        catch (Exception ex)
        {
            LogLoadGameError(ex);
        }

        // If loading failed or no saved game, start new game
        _engine.NewGame();
    }

    private void RebuildTilesForCurrentBoardSize()
    {
        Tiles.Clear();

        for (int row = 0; row < _config.Size; row++)
        {
            for (int col = 0; col < _config.Size; col++)
            {
                Tiles.Add(new TileViewModel { Row = row, Column = col });
            }
        }
    }

    [RelayCommand]
    private Task PlaySelectedModeAsync()
    {
        var config = new GameConfig
        {
            Size = PendingBoardSize,
            WinTile = _config.WinTile,
            Mode = PendingGameMode,
        };
        return ApplyRulesetAsync(config, startNew: false);
    }

    [RelayCommand]
    private Task StartNewSelectedModeAsync()
    {
        var config = new GameConfig
        {
            Size = PendingBoardSize,
            WinTile = _config.WinTile,
            Mode = PendingGameMode,
        };
        return ApplyRulesetAsync(config, startNew: true);
    }

    public async Task ApplyRulesetAsync(GameConfig newConfig, bool startNew)
    {
        if (newConfig.Size <= 0 || newConfig.Size > GameConfig.MaxReasonableBoardSize)
        {
            LogInvalidBoardSizeRequested(newConfig.Size);
            return;
        }

        var oldConfig = _config;
        var oldSize = oldConfig.Size;
        var oldRulesetId = oldConfig.RulesetId;
        var newRulesetId = newConfig.RulesetId;
        if (!startNew && string.Equals(oldRulesetId, newRulesetId, StringComparison.Ordinal))
        {
            return;
        }

        await _moveLock.WaitAsync();
        try
        {
            _isNewGameConfirmationInProgress = false;

            // Persist the outgoing run and finalize stats for the outgoing ruleset.
            _repository.SaveGame(oldConfig, _engine.ToSaveDto());

            if (!_engine.CurrentState.IsGameOver)
            {
                _statisticsTracker.OnGameEnded(
                    _engine.CurrentState.Score,
                    _engine.CurrentState.IsWon
                );
            }

            await _repository.FlushAsync(oldConfig);

            _engine.VictoryAchieved -= OnEngineVictoryAchieved;
            _config = newConfig;

            // Persist last active mode so we restore the correct ruleset on reboot.
            _settingsService.LastActiveGameConfig = _config;
            PendingBoardSize = _config.Size;
            PendingGameMode = _config.Mode;

            // Rebuild tiles before any UpdateUI() calls.
            RebuildTilesForCurrentBoardSize();

            // Load ruleset-scoped best score.
            BestScore = _repository.GetBestScore(_config);

            if (startNew)
            {
                _repository.ClearSavedGame(_config);
            }

            // Load ruleset-scoped saved game if present.
            var save = startNew ? null : _repository.LoadGame(_config);
            if (save != null)
            {
                _engine = _engineFactory.Create(save, _config);
            }
            else
            {
                _engine = _engineFactory.Create(_config);
                _repository.SaveGame(_config, _engine.ToSaveDto());
            }

            _coachNudgeService.Dismiss();
            IsCoachNudgeVisible = false;

            _engine.VictoryAchieved += OnEngineVictoryAchieved;

            // Clear any victory overlay that might have been showing.
            _victoryViewModel.HideVictoryOverlayIfShowing();

            // Notify UI that BoardSize changed and refresh values.
            OnPropertyChanged(nameof(BoardSize));
            OnPropertyChanged(nameof(GameMode));
            UpdateUI();

            WeakReferenceMessenger.Default.Send(
                new RulesetChangedMessage(oldRulesetId, _config.RulesetId, oldSize, _config.Size)
            );
        }
        finally
        {
            _moveLock.Release();
        }
    }

    private async Task ApplyBoardSizeChangeRequestAsyncSafe(int newSize)
    {
        try
        {
            var config = new GameConfig
            {
                Size = newSize,
                WinTile = _config.WinTile,
                Mode = _config.Mode,
            };
            await ApplyRulesetAsync(config, startNew: false);
        }
        catch (Exception ex)
        {
            LogApplyBoardSizeFailed(ex);
        }
    }

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to load game state")]
    partial void LogLoadGameError(Exception ex);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Warning,
        Message = "Ignored invalid board size change request: {boardSize}"
    )]
    private partial void LogInvalidBoardSizeRequested(int boardSize);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Error,
        Message = "Failed to apply board size change"
    )]
    private partial void LogApplyBoardSizeFailed(Exception ex);

    private void OnEngineVictoryAchieved(object? sender, EventArgs e)
    {
        // Only forward if initialization is complete (avoid early MAUI issues)
        if (!_isInitialized)
        {
            return;
        }

        // The Core engine raises VictoryAchieved during Move(), which happens before the
        // ViewModel has copied the latest engine state (including Score) into observable properties.
        // Sync now so victory UI always sees the final, up-to-date values.
        UpdateUI();

        VictoryAnimationRequested?.Invoke(this, e);
    }

    private async Task ShowGameOverDialogAsync()
    {
        var tryAgain = await _feedbackService.ShowGameOverAsync(Score, BestScore, UndoCount);
        if (tryAgain)
        {
            await NewGameAsync();
        }
    }

    [RelayCommand]
    private Task ShowLeaderboard() => _sessionCoordinator.ShowLeaderboardAsync(_config);

    [RelayCommand]
    private Task ShowAchievements() => _sessionCoordinator.ShowAchievementsAsync();
}
