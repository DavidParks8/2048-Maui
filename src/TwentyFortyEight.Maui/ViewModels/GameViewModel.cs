using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Models;
using TwentyFortyEight.Maui.Serialization;

namespace TwentyFortyEight.Maui.ViewModels;

/// <summary>
/// ViewModel for the 2048 game.
/// </summary>
public partial class GameViewModel : ObservableObject
{
    private readonly GameConfig _config;
    private readonly ILogger<GameViewModel> _logger;
    private Game2048Engine _engine;

    public ObservableCollection<TileViewModel> Tiles { get; }

    /// <summary>
    /// Event raised when tiles are updated and need animations.
    /// </summary>
    public event EventHandler<TileUpdateEventArgs>? TilesUpdated;

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

    public GameViewModel(ILogger<GameViewModel> logger)
    {
        _logger = logger;
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
        _engine.NewGame();
        UpdateUI();
        SaveGame();
    }

    [RelayCommand]
    private void Move(Direction direction)
    {
        // Capture previous state before the move
        var previousBoard = (int[])_engine.CurrentState.Board.Clone();
        
        var moved = _engine.Move(direction);
        if (moved)
        {
            UpdateUI(previousBoard);
            SaveGame();

            // Update best score
            if (Score > BestScore)
            {
                BestScore = Score;
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

    private void UpdateUI(int[]? previousBoard = null)
    {
        var state = _engine.CurrentState;
        var eventArgs = new TileUpdateEventArgs();
        
        if (previousBoard != null)
        {
            for (int i = 0; i < state.Board.Length; i++)
            {
                var tile = Tiles[i];
                var newValue = state.Board[i];
                var oldValue = previousBoard[i];

                // Reset animation flags
                tile.IsNewTile = false;
                tile.IsMerged = false;

                // Case 1: New tile spawned (0 -> 2 or 0 -> 4)
                if (oldValue == 0 && (newValue == 2 || newValue == 4))
                {
                    tile.IsNewTile = true;
                    eventArgs.NewTiles.Add(tile);
                }
                // Case 2: Tile merged (doubled in value)
                else if (oldValue != 0 && newValue == oldValue * 2)
                {
                    tile.IsMerged = true;
                    eventArgs.MergedTiles.Add(tile);
                }
                // Case 3: Tile changed (for sliding - any other value change)
                else if (oldValue != newValue)
                {
                    eventArgs.MovedTiles.Add(tile);
                }

                tile.UpdateValue(newValue);
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

        // Raise event for animations if there are changes
        if (previousBoard != null && (eventArgs.NewTiles.Count > 0 || eventArgs.MergedTiles.Count > 0 || eventArgs.MovedTiles.Count > 0))
        {
            TilesUpdated?.Invoke(this, eventArgs);
        }
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
}
