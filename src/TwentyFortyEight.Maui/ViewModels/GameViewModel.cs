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
    private readonly IGameCenterService _gameCenterService;
    private Game2048Engine _engine;
    private readonly HashSet<int> _reportedTiles = new();
    private readonly HashSet<int> _reportedScores = new();

    public ObservableCollection<TileViewModel> Tiles { get; }

    [ObservableProperty]
    private int _score;

    [ObservableProperty]
    private int _bestScore;
    
    partial void OnBestScoreChanged(int value)
    {
        Preferences.Set("BestScore", value);
    }

    [ObservableProperty]
    private int _moves;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private bool _isGameCenterAvailable;

    public GameViewModel(ILogger<GameViewModel> logger, IGameCenterService gameCenterService)
    {
        _logger = logger;
        _gameCenterService = gameCenterService;
        _config = new GameConfig();
        _engine = new Game2048Engine(_config, new SystemRandomSource());
        
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
        
        // Check Game Center availability periodically
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(1000);
                IsGameCenterAvailable = _gameCenterService.IsAvailable;
                if (IsGameCenterAvailable)
                    break;
            }
        });
    }

    [RelayCommand]
    private void NewGame()
    {
        _engine.NewGame();
        UpdateUI();
        SaveGame();
    }

    [RelayCommand]
    private void Move(Direction direction)
    {
        var moved = _engine.Move(direction);
        if (moved)
        {
            UpdateUI();
            SaveGame();

            // Update best score and submit to Game Center if it's a new high score
            if (Score > BestScore)
            {
                BestScore = Score;
                _ = SubmitScoreToGameCenter(Score);
            }
            
            // Check and report achievements
            _ = CheckAndReportAchievements();
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

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_engine.Redo())
        {
            UpdateUI();
            SaveGame();
        }
    }

    private void UpdateUI()
    {
        var state = _engine.CurrentState;
        
        // Update tiles
        for (int i = 0; i < state.Board.Length; i++)
        {
            Tiles[i].UpdateValue(state.Board[i]);
        }

        // Update properties
        Score = state.Score;
        Moves = state.MoveCount;
        CanUndo = _engine.CanUndo;
        CanRedo = _engine.CanRedo;

        // Update status text
        if (state.IsGameOver)
        {
            StatusText = Resources.Strings.AppStrings.GameOver;
            // Submit score to Game Center on game over
            _ = SubmitScoreToGameCenter(Score);
        }
        else if (state.IsWon)
        {
            StatusText = Resources.Strings.AppStrings.YouWin;
        }
        else
        {
            StatusText = "";
        }

        // Refresh command can execute states
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
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
                var dto = JsonSerializer.Deserialize(savedJson, GameSerializationContext.Default.GameStateDto);
                if (dto != null)
                {
                    var state = dto.ToGameState();
                    _engine = new Game2048Engine(state, _config, new SystemRandomSource());
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

    private async Task SubmitScoreToGameCenter(int score)
    {
        try
        {
            await _gameCenterService.SubmitScoreAsync(score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit score to Game Center");
        }
    }

    private async Task CheckAndReportAchievements()
    {
        try
        {
            var state = _engine.CurrentState;
            
            // Check for tile achievements
            var maxTile = state.Board.Max();
            await ReportTileAchievement(maxTile);
            
            // Check for first win achievement (reaching 2048)
            if (state.IsWon && !_reportedTiles.Contains(2048))
            {
#if IOS
                await _gameCenterService.ReportAchievementAsync(
                    Services.GameCenterService.Achievement_FirstWin, 100.0);
#endif
            }
            
            // Check for score achievements
            await ReportScoreAchievement(state.Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report achievements to Game Center");
        }
    }

    private async Task ReportTileAchievement(int maxTile)
    {
        // Only report each tile achievement once
        if (maxTile < 128 || _reportedTiles.Contains(maxTile))
            return;

#if IOS
        var achievementId = maxTile switch
        {
            >= 4096 => Services.GameCenterService.Achievement_Tile4096,
            >= 2048 => Services.GameCenterService.Achievement_Tile2048,
            >= 1024 => Services.GameCenterService.Achievement_Tile1024,
            >= 512 => Services.GameCenterService.Achievement_Tile512,
            >= 256 => Services.GameCenterService.Achievement_Tile256,
            >= 128 => Services.GameCenterService.Achievement_Tile128,
            _ => null
        };

        if (achievementId != null)
        {
            await _gameCenterService.ReportAchievementAsync(achievementId, 100.0);
            _reportedTiles.Add(maxTile);
        }
#else
        await Task.CompletedTask;
#endif
    }

    private async Task ReportScoreAchievement(int score)
    {
#if IOS
        var milestones = new[] { 10000, 25000, 50000, 100000 };
        foreach (var milestone in milestones)
        {
            if (score >= milestone && !_reportedScores.Contains(milestone))
            {
                var achievementId = milestone switch
                {
                    10000 => Services.GameCenterService.Achievement_Score10000,
                    25000 => Services.GameCenterService.Achievement_Score25000,
                    50000 => Services.GameCenterService.Achievement_Score50000,
                    100000 => Services.GameCenterService.Achievement_Score100000,
                    _ => null
                };

                if (achievementId != null)
                {
                    await _gameCenterService.ReportAchievementAsync(achievementId, 100.0);
                    _reportedScores.Add(milestone);
                }
            }
        }
#else
        await Task.CompletedTask;
#endif
    }

    [RelayCommand]
    private async Task ShowLeaderboard()
    {
        try
        {
            await _gameCenterService.ShowLeaderboardAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show Game Center leaderboard");
        }
    }

    [RelayCommand]
    private async Task ShowAchievements()
    {
        try
        {
            await _gameCenterService.ShowAchievementsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show Game Center achievements");
        }
    }
}
