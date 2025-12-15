using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Models;

namespace TwentyFortyEight.Maui.ViewModels;

/// <summary>
/// ViewModel for the 2048 game.
/// </summary>
public class GameViewModel : BaseViewModel
{
    private readonly GameConfig _config;
    private Game2048Engine _engine;
    private int _bestScore;
    private string _statusText = "";

    public ObservableCollection<TileViewModel> Tiles { get; }

    private int _score;
    public int Score
    {
        get => _score;
        private set => SetProperty(ref _score, value);
    }

    public int BestScore
    {
        get => _bestScore;
        private set
        {
            if (SetProperty(ref _bestScore, value))
            {
                Preferences.Set("BestScore", value);
            }
        }
    }

    private int _moves;
    public int Moves
    {
        get => _moves;
        private set => SetProperty(ref _moves, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private bool _canUndo;
    public bool CanUndo
    {
        get => _canUndo;
        private set => SetProperty(ref _canUndo, value);
    }

    private bool _canRedo;
    public bool CanRedo
    {
        get => _canRedo;
        private set => SetProperty(ref _canRedo, value);
    }

    public ICommand NewGameCommand { get; }
    public ICommand MoveCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }

    public GameViewModel()
    {
        _config = new GameConfig();
        _engine = new Game2048Engine(_config);
        
        // Initialize tiles collection (4x4 grid = 16 tiles)
        Tiles = new ObservableCollection<TileViewModel>();
        for (int row = 0; row < _config.Size; row++)
        {
            for (int col = 0; col < _config.Size; col++)
            {
                Tiles.Add(new TileViewModel { Row = row, Column = col });
            }
        }

        // Commands
        NewGameCommand = new Command(NewGame);
        MoveCommand = new Command<Direction>(Move);
        UndoCommand = new Command(Undo, () => CanUndo);
        RedoCommand = new Command(Redo, () => CanRedo);

        // Load saved state or start new game
        LoadGame();
        UpdateUI();
    }

    private void NewGame()
    {
        _engine.NewGame();
        UpdateUI();
        SaveGame();
    }

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
        }
    }

    private void Undo()
    {
        if (_engine.Undo())
        {
            UpdateUI();
            SaveGame();
        }
    }

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
            StatusText = "Game Over!";
        }
        else if (state.IsWon)
        {
            StatusText = "You Win!";
        }
        else
        {
            StatusText = "";
        }

        // Refresh command can execute states
        ((Command)UndoCommand).ChangeCanExecute();
        ((Command)RedoCommand).ChangeCanExecute();
    }

    private void SaveGame()
    {
        try
        {
            var dto = GameStateDto.FromGameState(_engine.CurrentState);
            var json = JsonSerializer.Serialize(dto);
            Preferences.Set("SavedGame", json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private void LoadGame()
    {
        try
        {
            // Load best score
            _bestScore = Preferences.Get("BestScore", 0);

            // Try to load saved game
            var savedJson = Preferences.Get("SavedGame", string.Empty);
            if (!string.IsNullOrEmpty(savedJson))
            {
                var dto = JsonSerializer.Deserialize<GameStateDto>(savedJson);
                if (dto != null)
                {
                    var state = dto.ToGameState();
                    _engine = new Game2048Engine(state, _config);
                    return;
                }
            }
        }
        catch
        {
            // Ignore load errors and start new game
        }

        // If loading failed or no saved game, start new game
        _engine.NewGame();
    }
}
