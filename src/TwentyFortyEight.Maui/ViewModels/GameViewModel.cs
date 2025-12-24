using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Models;
using TwentyFortyEight.Maui.Serialization;
using TwentyFortyEight.Maui.Services;

namespace TwentyFortyEight.Maui.ViewModels;

/// <summary>
/// ViewModel for the 2048 game.
/// </summary>
public partial class GameViewModel : ObservableObject
{
    private readonly GameConfig _config;
    private readonly ILogger<GameViewModel> _logger;
    private readonly IMoveAnalyzer _moveAnalyzer;
    private readonly IStatisticsTracker _statisticsTracker;
    private readonly IAchievementTracker _achievementTracker;
    private readonly ISocialGamingService _socialGamingService;
    private Game2048Engine _engine;

    /// <summary>
    /// Lock to prevent concurrent move processing.
    /// When true, a move is currently being processed and new moves should be queued or ignored.
    /// </summary>
    private volatile bool _isProcessingMove;

    /// <summary>
    /// Task completion source to signal when the current animation completes.
    /// </summary>
    private TaskCompletionSource? _animationCompletionSource;

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
        _animationCompletionSource?.TrySetResult();
    }

    [ObservableProperty]
    private int _score;

    [ObservableProperty]
    private int _bestScore;

    /// <summary>
    /// Gets the board size for UI layout calculations.
    /// </summary>
    public int BoardSize => _config.Size;

    partial void OnBestScoreChanged(int value)
    {
        Preferences.Set("BestScore", value);
    }

    [ObservableProperty]
    private int _moves;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isGameOver;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _isHowToPlayVisible;

    [ObservableProperty]
    private bool _isSocialGamingAvailable;

    public GameViewModel(
        ILogger<GameViewModel> logger,
        IMoveAnalyzer moveAnalyzer,
        IStatisticsTracker statisticsTracker,
        IAchievementTracker achievementTracker,
        ISocialGamingService socialGamingService
    )
    {
        _logger = logger;
        _moveAnalyzer = moveAnalyzer;
        _statisticsTracker = statisticsTracker;
        _achievementTracker = achievementTracker;
        _socialGamingService = socialGamingService;
        _config = new GameConfig();
        _engine = new Game2048Engine(_config, new SystemRandomSource(), _statisticsTracker);

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

        // Update social gaming availability when service becomes available
        UpdateSocialGamingAvailability();
    }

    private void UpdateSocialGamingAvailability()
    {
        IsSocialGamingAvailable = _socialGamingService.IsAvailable;
    }

    [RelayCommand]
    private async Task NewGameAsync()
    {
        // Show confirmation if game is in progress (has moves and not game over)
        if (Moves > 0 && !IsGameOver)
        {
            // Use Shell.Current.CurrentPage for displaying alerts
            var page = Shell.Current?.CurrentPage;
            if (page != null)
            {
                bool confirm = await page.DisplayAlertAsync(
                    Resources.Strings.AppStrings.RestartConfirmTitle,
                    Resources.Strings.AppStrings.RestartConfirmMessage,
                    Resources.Strings.AppStrings.StartNew,
                    Resources.Strings.AppStrings.Cancel
                );

                if (!confirm)
                {
                    return;
                }
            }
        }

        _engine.NewGame();

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
        // Prevent concurrent move processing to avoid broken board state from fast swiping
        if (_isProcessingMove)
        {
            return;
        }

        _isProcessingMove = true;
        try
        {
            // Capture previous state before the move
            var previousBoard = _engine.CurrentState.Board.Clone();

            var moved = _engine.Move(direction);
            if (moved)
            {
                // Create a completion source to wait for animation
                _animationCompletionSource = new TaskCompletionSource();

                UpdateUI(previousBoard, direction);
                SaveGame();

                // Update best score and submit to social gaming service
                if (Score > BestScore)
                {
                    BestScore = Score;
                    _ = SubmitScoreToSocialGaming(Score);
                }

                // Wait for animation to complete (with timeout to prevent deadlock)
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(500));
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

            // Check and report achievements
            _ = CheckAndReportAchievements();

            // Update social gaming availability
            UpdateSocialGamingAvailability();
        }
        finally
        {
            _isProcessingMove = false;
        }
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
        await Shell.Current.GoToAsync("stats");
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync("settings");
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
                var eventArgs = new TileUpdateEventArgs
                {
                    MovedTiles = movedTiles.ToFrozenSet(),
                    NewTiles = newTiles.ToFrozenSet(),
                    MergedTiles = mergedTiles.ToFrozenSet(),
                    MoveDirection = moveDirection.Value,
                    TileMovements = analysis.Movements,
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
            StatusText = Resources.Strings.AppStrings.YouWin;
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
            Preferences.Set("SavedGame", json);
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
            BestScore = Preferences.Get("BestScore", 0);

            // Try to load saved game
            var savedJson = Preferences.Get("SavedGame", string.Empty);
            if (!string.IsNullOrEmpty(savedJson))
            {
                var dto = JsonSerializer.Deserialize(
                    savedJson,
                    GameSerializationContext.Default.GameStateDto
                );
                if (dto != null)
                {
                    var state = dto.ToGameState();
                    _engine = new Game2048Engine(
                        state,
                        _config,
                        new SystemRandomSource(),
                        _statisticsTracker
                    );
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

    private async Task SubmitScoreToSocialGaming(int score)
    {
        try
        {
            await _socialGamingService.SubmitScoreAsync(score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit score to social gaming service");
        }
    }

    private async Task CheckAndReportAchievements()
    {
        var state = _engine.CurrentState;

        // Check for tile achievements using the core tracker
        if (_achievementTracker.CheckTileAchievement(state.MaxTileValue))
        {
            var tileValue = _achievementTracker.LastUnlockedTileValue!.Value;
            var achievementId = GetTileAchievementId(tileValue);
            if (achievementId != null)
            {
                await _socialGamingService.ReportAchievementAsync(achievementId, 100.0);
            }
        }

        // Check for first win achievement
        if (_achievementTracker.CheckFirstWinAchievement(state.IsWon))
        {
            var achievementId = GetFirstWinAchievementId();
            if (achievementId != null)
            {
                await _socialGamingService.ReportAchievementAsync(achievementId, 100.0);
            }
        }

        // Check for score achievements
        if (_achievementTracker.CheckScoreAchievement(state.Score))
        {
            var scoreMilestone = _achievementTracker.LastUnlockedScoreMilestone!.Value;
            var achievementId = GetScoreAchievementId(scoreMilestone);
            if (achievementId != null)
            {
                await _socialGamingService.ReportAchievementAsync(achievementId, 100.0);
            }
        }

        // Reset the "just unlocked" flags after reporting
        _achievementTracker.ResetJustUnlocked();
    }

    private static string? GetTileAchievementId(int tileValue)
    {
#if IOS || MACCATALYST
        return tileValue switch
        {
            4096 => PlatformAchievementIds.iOS.Achievement_Tile4096,
            2048 => PlatformAchievementIds.iOS.Achievement_Tile2048,
            1024 => PlatformAchievementIds.iOS.Achievement_Tile1024,
            512 => PlatformAchievementIds.iOS.Achievement_Tile512,
            256 => PlatformAchievementIds.iOS.Achievement_Tile256,
            128 => PlatformAchievementIds.iOS.Achievement_Tile128,
            _ => null,
        };
#else
        return null;
#endif
    }

    private static string? GetFirstWinAchievementId()
    {
#if IOS || MACCATALYST
        return PlatformAchievementIds.iOS.Achievement_FirstWin;
#else
        return null;
#endif
    }

    private static string? GetScoreAchievementId(int score)
    {
#if IOS || MACCATALYST
        return score switch
        {
            10000 => PlatformAchievementIds.iOS.Achievement_Score10000,
            25000 => PlatformAchievementIds.iOS.Achievement_Score25000,
            50000 => PlatformAchievementIds.iOS.Achievement_Score50000,
            100000 => PlatformAchievementIds.iOS.Achievement_Score100000,
            _ => null,
        };
#else
        return null;
#endif
    }

    [RelayCommand]
    private async Task ShowLeaderboard()
    {
        await _socialGamingService.ShowLeaderboardAsync();
    }

    [RelayCommand]
    private async Task ShowAchievements()
    {
        await _socialGamingService.ShowAchievementsAsync();
    }
}
