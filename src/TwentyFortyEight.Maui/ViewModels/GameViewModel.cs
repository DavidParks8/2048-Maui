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
    private readonly IStatisticsService _statisticsService;
    private Game2048Engine _engine;
    private bool _hasReached2048InCurrentGame;

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

    public GameViewModel(ILogger<GameViewModel> logger, IStatisticsService statisticsService)
    {
        _logger = logger;
        _statisticsService = statisticsService;
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
    }

    [RelayCommand]
    private void NewGame()
    {
        // Stop time tracking for previous game
        _statisticsService.StopTimeTracking();
        
        // Record game loss if previous game didn't win
        if (!_hasReached2048InCurrentGame && Moves > 0)
        {
            _statisticsService.RecordGameLoss();
        }
        
        _engine.NewGame();
        _hasReached2048InCurrentGame = false;
        
        // Increment games played and start time tracking
        _statisticsService.IncrementGamesPlayed();
        _statisticsService.StartTimeTracking();
        
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

            // Update best score
            if (Score > BestScore)
            {
                BestScore = Score;
            }
            
            // Update statistics
            _statisticsService.UpdateBestScore(Score);
            
            // Check for highest tile
            var highestTile = _engine.CurrentState.Board.Max();
            _statisticsService.UpdateHighestTile(highestTile);
            
            // Track moves
            _statisticsService.AddMoves(1);
            
            // Check if won (reached 2048 for the first time in this game)
            if (!_hasReached2048InCurrentGame && _engine.CurrentState.IsWon)
            {
                _hasReached2048InCurrentGame = true;
                _statisticsService.IncrementGamesWon();
            }
            
            // If game over, finalize stats
            if (_engine.CurrentState.IsGameOver)
            {
                _statisticsService.StopTimeTracking();
                _statisticsService.AddScore(Score);
                
                if (!_hasReached2048InCurrentGame)
                {
                    _statisticsService.RecordGameLoss();
                }
            }
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

    [RelayCommand]
    private async Task ShowStats()
    {
        await Shell.Current.GoToAsync(nameof(StatsPage));
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
                    
                    // Check if this loaded game already has 2048
                    _hasReached2048InCurrentGame = state.IsWon;
                    
                    // Resume time tracking if game is ongoing
                    if (!state.IsGameOver)
                    {
                        _statisticsService.StartTimeTracking();
                    }
                    
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
        _hasReached2048InCurrentGame = false;
        
        // Increment games played and start time tracking
        _statisticsService.IncrementGamesPlayed();
        _statisticsService.StartTimeTracking();
    }
}
