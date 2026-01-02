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
    private readonly GameConfig _config;
    private readonly ILogger<GameViewModel> _logger;
    private readonly IMoveAnalyzer _moveAnalyzer;
    private readonly ISettingsService _settingsService;
    private readonly IStatisticsTracker _statisticsTracker;
    private readonly IRandomSource _randomSource;
    private readonly IGameStateRepository _repository;
    private readonly IGameSessionCoordinator _sessionCoordinator;
    private readonly IUserFeedbackService _feedbackService;
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

    /// <summary>
    /// Gets the board size for UI layout calculations.
    /// </summary>
    public int BoardSize => _config.Size;

    [ObservableProperty]
    private double _boardScaleFactor = 1.0;

    [ObservableProperty]
    private int _moves;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _isSocialGamingAvailable;

    public GameViewModel(
        ILogger<GameViewModel> logger,
        IMoveAnalyzer moveAnalyzer,
        ISettingsService settingsService,
        IStatisticsTracker statisticsTracker,
        IRandomSource randomSource,
        IGameStateRepository repository,
        IGameSessionCoordinator sessionCoordinator,
        IUserFeedbackService feedbackService
    )
    {
        _logger = logger;
        _moveAnalyzer = moveAnalyzer;
        _settingsService = settingsService;
        _statisticsTracker = statisticsTracker;
        _randomSource = randomSource;
        _repository = repository;
        _sessionCoordinator = sessionCoordinator;
        _feedbackService = feedbackService;
        _config = new GameConfig();
        _engine = new Game2048Engine(_config, _randomSource, _statisticsTracker);
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
        // Show confirmation if game is in progress (has moves and not game over)
        if (Moves > 0 && !_engine.CurrentState.IsGameOver)
        {
            if (!await _feedbackService.ConfirmNewGameAsync())
            {
                return;
            }
        }

        _engine.NewGame();
        UpdateUI();
        _repository.SaveGameState(_engine.CurrentState);
    }

    [RelayCommand]
    private Task ShowHowToPlay()
    {
        return _feedbackService.ShowHowToPlayAsync();
    }

    [RelayCommand]
    private async Task MoveAsync(Direction direction)
    {
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
            var previousScore = Score;

            var moved = _engine.Move(direction);
            if (moved)
            {
                // Trigger haptic feedback if enabled and supported
                _feedbackService.PerformMoveHaptic();

                UpdateUI(previousBoard, direction);
                _repository.SaveGameState(_engine.CurrentState);

                // Update best score and submit to social gaming service
                bool isNewBest = Score > BestScore;
                if (isNewBest)
                {
                    BestScore = Score;
                    _repository.UpdateBestScoreIfHigher(Score);
                }

                // Wait for the slide duration to block input, ensuring the game feels responsive
                // but prevents rapid-fire moves that could break the animation state.
                await Task.Delay(GetInputBlockDuration());

                // Check and report achievements and scores
                await _sessionCoordinator.OnMoveCompletedAsync(_engine.CurrentState);
                await _sessionCoordinator.OnScoreChangedAsync(Score, isNewBest);
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
        if (_engine.Undo())
        {
            UpdateUI();
            _repository.SaveGameState(_engine.CurrentState);
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

    private void UpdateUI(Board? previousBoard = null, Direction? moveDirection = null)
    {
        var state = _engine.CurrentState;

        if (previousBoard != null && moveDirection != null)
        {
            // Use Core MoveAnalyzer for all movement and categorization logic
            var analysis = _moveAnalyzer.Analyze(
                previousBoard.Value,
                state.Board,
                moveDirection.Value
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
    }

    private void LoadGame()
    {
        try
        {
            // Load best score from repository
            BestScore = _repository.GetBestScore();

            // Try to load saved game
            var state = _repository.LoadGameState();
            if (state != null)
            {
                // IMPORTANT: Unsubscribe before replacing engine to prevent leaks/double firing.
                _engine.VictoryAchieved -= OnEngineVictoryAchieved;

                _engine = new Game2048Engine(state, _config, _randomSource, _statisticsTracker);
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

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to load game state")]
    partial void LogLoadGameError(Exception ex);

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
        var tryAgain = await _feedbackService.ShowGameOverAsync(Score, BestScore);
        if (tryAgain)
        {
            await NewGameAsync();
        }
    }

    [RelayCommand]
    private Task ShowLeaderboard() => _sessionCoordinator.ShowLeaderboardAsync();

    [RelayCommand]
    private Task ShowAchievements() => _sessionCoordinator.ShowAchievementsAsync();
}
