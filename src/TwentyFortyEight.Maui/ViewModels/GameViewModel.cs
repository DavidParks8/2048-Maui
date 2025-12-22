using System.Collections.Frozen;
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
    private bool _isGameOver;

    [ObservableProperty]
    private bool _canUndo;

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
            UpdateUI(previousBoard, direction);
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

    private void UpdateUI(int[]? previousBoard = null, Direction? moveDirection = null)
    {
        var state = _engine.CurrentState;

        if (previousBoard != null && moveDirection != null)
        {
            var movedTiles = new HashSet<TileViewModel>();
            var newTiles = new HashSet<TileViewModel>();
            var mergedTiles = new HashSet<TileViewModel>();
            var tileMovements = new List<TileMovement>();

            // Calculate tile movements by simulating the move logic
            CalculateTileMovements(previousBoard, state.Board, moveDirection.Value, tileMovements);

            for (int i = 0; i < state.Board.Length; i++)
            {
                var tile = Tiles[i];
                var newValue = state.Board[i];
                var oldValue = previousBoard[i];
                var row = i / _config.Size;
                var col = i % _config.Size;

                // Reset animation flags
                tile.IsNewTile = false;
                tile.IsMerged = false;

                // Check if a tile moved away from this position
                var movedAwayFrom = tileMovements.Any(m => m.FromRow == row && m.FromColumn == col);
                // Check if a tile moved to this position
                var isMovedHere = tileMovements.Any(m => m.ToRow == row && m.ToColumn == col);

                // Case 1: New tile spawned
                // Either: was empty and now has 2/4 (and nothing moved here)
                // Or: a tile moved away and there's still a value here (new spawn in vacated spot)
                if (newValue == 2 || newValue == 4)
                {
                    if ((oldValue == 0 && !isMovedHere) || (movedAwayFrom && !isMovedHere))
                    {
                        tile.IsNewTile = true;
                        newTiles.Add(tile);
                    }
                    else if (isMovedHere)
                    {
                        movedTiles.Add(tile);
                    }
                }
                // Case 2: Tile merged (check if this position received a merge)
                else if (newValue != 0)
                {
                    var mergingMovements = tileMovements
                        .Where(m => m.ToRow == row && m.ToColumn == col && m.IsMerging)
                        .ToList();
                    if (mergingMovements.Count > 0)
                    {
                        tile.IsMerged = true;
                        mergedTiles.Add(tile);
                    }
                    else if (oldValue != newValue)
                    {
                        movedTiles.Add(tile);
                    }
                }

                tile.UpdateValue(newValue);
            }

            // Create event args with frozen collections if there are changes
            if (
                movedTiles.Count > 0
                || newTiles.Count > 0
                || mergedTiles.Count > 0
                || tileMovements.Count > 0
            )
            {
                var eventArgs = new TileUpdateEventArgs
                {
                    MovedTiles = movedTiles.ToFrozenSet(),
                    NewTiles = newTiles.ToFrozenSet(),
                    MergedTiles = mergedTiles.ToFrozenSet(),
                    MoveDirection = moveDirection.Value,
                    TileMovements = tileMovements,
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

    /// <summary>
    /// Calculates tile movements from the previous board to the new board.
    /// This tracks where each tile came from, including merges.
    /// </summary>
    private void CalculateTileMovements(
        int[] previousBoard,
        int[] newBoard,
        Direction direction,
        List<TileMovement> movements
    )
    {
        var size = _config.Size;

        // Process each line (row or column) depending on direction
        for (int line = 0; line < size; line++)
        {
            // Get indices for this line based on direction
            var indices = GetLineIndices(line, size, direction);

            // Collect non-zero tiles from previous board with their positions
            var tiles = new List<(int index, int value)>();
            foreach (var idx in indices)
            {
                if (previousBoard[idx] != 0)
                {
                    tiles.Add((idx, previousBoard[idx]));
                }
            }

            if (tiles.Count == 0)
                continue;

            // Process tiles: merge and compact toward the direction
            int destPosition = 0;
            int i = 0;
            while (i < tiles.Count)
            {
                var (sourceIdx, value) = tiles[i];
                var sourceRow = sourceIdx / size;
                var sourceCol = sourceIdx % size;

                // Check if next tile can merge with this one
                if (i + 1 < tiles.Count && tiles[i + 1].value == value)
                {
                    // Merge: both tiles move to the destination
                    var destIdx = indices[destPosition];
                    var destRow = destIdx / size;
                    var destCol = destIdx % size;

                    // First tile moves and merges
                    movements.Add(
                        new TileMovement(sourceRow, sourceCol, destRow, destCol, value, true)
                    );

                    // Second tile also moves and merges
                    var (source2Idx, _) = tiles[i + 1];
                    var source2Row = source2Idx / size;
                    var source2Col = source2Idx % size;
                    movements.Add(
                        new TileMovement(source2Row, source2Col, destRow, destCol, value, true)
                    );

                    i += 2;
                }
                else
                {
                    // No merge: tile just moves (or stays)
                    var destIdx = indices[destPosition];
                    var destRow = destIdx / size;
                    var destCol = destIdx % size;

                    // Only record if actually moving
                    if (sourceRow != destRow || sourceCol != destCol)
                    {
                        movements.Add(
                            new TileMovement(sourceRow, sourceCol, destRow, destCol, value, false)
                        );
                    }

                    i++;
                }
                destPosition++;
            }
        }
    }

    /// <summary>
    /// Gets the board indices for a line (row or column) in the order they should be processed.
    /// </summary>
    private static int[] GetLineIndices(int line, int size, Direction direction)
    {
        var indices = new int[size];
        for (int i = 0; i < size; i++)
        {
            indices[i] = direction switch
            {
                Direction.Left => line * size + i,
                Direction.Right => line * size + (size - 1 - i),
                Direction.Up => i * size + line,
                Direction.Down => (size - 1 - i) * size + line,
                _ => 0,
            };
        }
        return indices;
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
