using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Serialization;
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
    private readonly IPreferencesService _preferencesService;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;
    private readonly IHapticService _hapticService;
    private Game2048Engine _engine;

    /// <summary>
    /// Semaphore to prevent concurrent move processing.
    /// Ensures only one move is processed at a time to avoid broken board state from fast swiping.
    /// </summary>
    private readonly SemaphoreSlim _moveLock = new(1, 1);

    /// <summary>
    /// Task completion source to signal when the current animation completes.
    /// </summary>
    private TaskCompletionSource<bool>? _animationCompletionSource;

    /// <summary>
    /// Debounce timer for saving best score to preferences.
    /// </summary>
    private CancellationTokenSource? _bestScoreSaveDebounce;

    /// <summary>
    /// Last announced score for screen reader, to avoid frequent announcements.
    /// </summary>
    private int _lastAnnouncedScore = 0;

    /// <summary>
    /// The collection of tiles for the game board.
    /// </summary>
    public ObservableCollection<TileViewModel> Tiles { get; }

    /// <summary>
    /// Event raised when tiles are updated and need animations.
    /// </summary>
    public event EventHandler<TileUpdateEventArgs>? TilesUpdated;

    /// <summary>
    /// Signals that the animation for the current move has completed.
    /// Called by the view layer after animations finish.
    /// </summary>
    public void SignalAnimationComplete()
    {
        _animationCompletionSource?.TrySetResult(true);
    }

    [ObservableProperty]
    private int _score;

    partial void OnScoreChanged(int value)
    {
        // Only announce score changes if:
        // 1. The score is greater than 0 (not a reset)
        // 2. The score increased by at least 10 points since last announcement
        // This prevents overwhelming screen reader users with frequent announcements
        if (value > 0 && value > _lastAnnouncedScore && value - _lastAnnouncedScore >= 10)
        {
            _screenReaderService.Announce($"Score: {value}");
        }
        
        // Always track the current score for accurate announcement logic
        // This ensures proper behavior with undo operations
        if (value >= 0)
        {
            _lastAnnouncedScore = value;
        }
    }

    [ObservableProperty]
    private int _bestScore;

    /// <summary>
    /// Gets the board size for UI layout calculations.
    /// </summary>
    public int BoardSize => _config.Size;

    partial void OnBestScoreChanged(int value)
    {
        // Debounce preference saving to avoid hammering storage during rapid undos
        _bestScoreSaveDebounce?.Cancel();
        _bestScoreSaveDebounce?.Dispose();
        _bestScoreSaveDebounce = new CancellationTokenSource();

        _ = DebouncedSaveBestScoreAsync(value, _bestScoreSaveDebounce.Token);
    }

    private async Task DebouncedSaveBestScoreAsync(int value, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(500, cancellationToken);
            _preferencesService.SetInt("BestScore", value);
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled by newer value - expected behavior
        }
    }

    [ObservableProperty]
    private int _moves;

    [ObservableProperty]
    private string _statusText = "";

    partial void OnStatusTextChanged(string value)
    {
        // Announce win status to screen readers
        if (!string.IsNullOrEmpty(value))
        {
            _screenReaderService.Announce(value);
        }
    }

    [ObservableProperty]
    private bool _isGameOver;

    partial void OnIsGameOverChanged(bool value)
    {
        if (value)
        {
            // Announce game over with final score
            _screenReaderService.Announce($"Game Over! Final score: {Score}");
        }
    }

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _isHowToPlayVisible;

    public GameViewModel(
        ILogger<GameViewModel> logger,
        IMoveAnalyzer moveAnalyzer,
        ISettingsService settingsService,
        IStatisticsTracker statisticsTracker,
        IRandomSource randomSource,
        IPreferencesService preferencesService,
        IAlertService alertService,
        INavigationService navigationService,
        ILocalizationService localizationService,
        IHapticService hapticService
    )
    {
        _logger = logger;
        _moveAnalyzer = moveAnalyzer;
        _settingsService = settingsService;
        _statisticsTracker = statisticsTracker;
        _randomSource = randomSource;
        _preferencesService = preferencesService;
        _alertService = alertService;
        _navigationService = navigationService;
        _localizationService = localizationService;
        _hapticService = hapticService;
        _config = new GameConfig();
        _engine = new Game2048Engine(_config, _randomSource, _statisticsTracker);

        // Initialize tiles collection (4x4 grid = 16 tiles)
        Tiles = new ObservableCollection<TileViewModel>();
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
    }

    [RelayCommand]
    private async Task NewGameAsync()
    {
        // Show confirmation if game is in progress (has moves and not game over)
        if (Moves > 0 && !IsGameOver)
        {
            bool confirm = await _alertService.ShowConfirmationAsync(
                _localizationService.RestartConfirmTitle,
                _localizationService.RestartConfirmMessage,
                _localizationService.StartNew,
                _localizationService.Cancel
            );

            if (!confirm)
            {
                return;
            }
        }

        _engine.NewGame();

        // Reset score announcement tracking before UI update to ensure consistency
        _lastAnnouncedScore = 0;

        UpdateUI();
        SaveGame();
    }

    [RelayCommand]
    private void ShowHowToPlay()
    {
        IsHowToPlayVisible = true;
    }

    [RelayCommand]
    private void CloseHowToPlay()
    {
        IsHowToPlayVisible = false;
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

            var moved = _engine.Move(direction);
            if (moved)
            {
                // Trigger haptic feedback if enabled and supported
                if (_settingsService.HapticsEnabled && _hapticService.IsSupported)
                {
                    _hapticService.PerformHaptic();
                }

                // Create a completion source to wait for animation
                _animationCompletionSource = new TaskCompletionSource<bool>();

                UpdateUI(previousBoard, direction);
                SaveGame();

                // Update best score
                if (Score > BestScore)
                {
                    BestScore = Score;
                }

                // Wait for animation to complete (with timeout to prevent deadlock)
                using CancellationTokenSource cts = new(GetAnimationWaitTimeout());
                try
                {
                    await _animationCompletionSource.Task.WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Animation timed out or was cancelled - continue anyway
                }
                finally
                {
                    _animationCompletionSource = null;
                }
            }
        }
        finally
        {
            _moveLock.Release();
        }
    }

    private TimeSpan GetAnimationWaitTimeout()
    {
        // Base animation durations (ms) from AnimationConstants, before speed scaling.
        const double baseSequenceMs = AnimationConstants.BaseTotalSequenceDuration;
        const double bufferMs = 300;

        var speed = _settingsService.AnimationSpeed;
        if (!double.IsFinite(speed) || speed <= 0)
        {
            speed = 1.0;
        }

        var timeoutMs = (baseSequenceMs / speed) + bufferMs;
        timeoutMs = Math.Clamp(timeoutMs, 250, 5000);
        return TimeSpan.FromMilliseconds(timeoutMs);
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_engine.Undo())
        {
            UpdateUI();
            SaveGame();
        }
    }

    [RelayCommand]
    private async Task OpenStatsAsync()
    {
        await _navigationService.NavigateToAsync("stats");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await _navigationService.NavigateToAsync("settings");
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

            var movedTiles = new HashSet<TileViewModel>();
            var newTiles = new HashSet<TileViewModel>();
            var mergedTiles = new HashSet<TileViewModel>();

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

                tile.UpdateValue(newValue);
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
                var movementsCopy = analysis.Movements.ToList();

                var eventArgs = new TileUpdateEventArgs
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
                Tiles[i].UpdateValue(state.Board[i]);
            }
        }

        // Update properties
        Score = state.Score;
        Moves = state.MoveCount;
        CanUndo = _engine.CanUndo;

        // Update game over state and status text
        IsGameOver = state.IsGameOver;

        if (state.IsWon)
        {
            StatusText = _localizationService.YouWin;
        }
        else
        {
            StatusText = "";
        }

        // Refresh command can execute states
        UndoCommand.NotifyCanExecuteChanged();
    }

    private void SaveGame()
    {
        try
        {
            var dto = GameStateDto.FromGameState(_engine.CurrentState);
            var json = JsonSerializer.Serialize(dto, GameSerializationContext.Default.GameStateDto);
            _preferencesService.SetString("SavedGame", json);
        }
        catch (Exception ex)
        {
            LogSaveGameError(ex);
        }
    }

    private void LoadGame()
    {
        try
        {
            // Load best score - use property to trigger OnBestScoreChanged
            BestScore = _preferencesService.GetInt("BestScore", 0);

            // Try to load saved game
            var savedJson = _preferencesService.GetString("SavedGame", string.Empty);
            if (!string.IsNullOrEmpty(savedJson))
            {
                var dto = JsonSerializer.Deserialize(
                    savedJson,
                    GameSerializationContext.Default.GameStateDto
                );
                if (dto != null)
                {
                    var state = dto.ToGameState();
                    _engine = new Game2048Engine(state, _config, _randomSource, _statisticsTracker);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            LogLoadGameError(ex);
        }

        // If loading failed or no saved game, start new game
        _engine.NewGame();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to save game state")]
    partial void LogSaveGameError(Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to load game state")]
    partial void LogLoadGameError(Exception ex);
}
