using System.Collections.Generic;
using Godot;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Main game controller that manages the 2048 game logic and UI.
/// </summary>
public partial class GameController : Node
{
    private GameConfig _config = null!;
    private Game2048Engine _engine = null!;
    private readonly IRandomSource _random = new GodotRandomSource();
    private readonly IBoardSimulator _boardSimulator = new GodotBoardSimulator();
    private readonly ISpawnStrategyFactory _spawnStrategyFactory = new GodotSpawnStrategyFactory();
    private readonly GodotMoveAnalyzer _moveAnalyzer = new();
    private readonly CoachNudgeService _coachNudgeService = new();
    private readonly IMoveAdvisor _moveAdvisor;

    private bool _isMoveLocked;
    private bool _isInitialized;

    public GameController()
    {
        _moveAdvisor = new GodotMoveAdvisor(_boardSimulator);
    }

    // UI References (set by MainScene)
    public BoardVisual? BoardVisual { get; set; }
    public Label? ScoreLabel { get; set; }
    public Label? BestScoreLabel { get; set; }
    public Label? SizeLabel { get; set; }
    public Label? ModeLabel { get; set; }
    public Control? GameOverOverlay { get; set; }
    public Control? VictoryOverlay { get; set; }
    public Button? UndoButton { get; set; }
    public Control? CoachNudge { get; set; }
    public Control? CoachHint { get; set; }

    // Events
    public event Action? ScoreChanged;
    public event Action? GameEnded;
    public event Action? VictoryAchieved;
    public event Action<Direction>? CoachSuggestionChanged;

    // State
    public int Score => _engine.CurrentState.Score;
    public int BestScore { get; private set; }
    public int BoardSize => _config.Size;
    public GameMode GameMode => _config.Mode;
    public bool CanUndo => _engine.CanUndo;
    public bool IsGameOver => _engine.CurrentState.IsGameOver;
    public bool IsAdversarialMode => _config.Mode == GameMode.Adversarial;

    public override void _Ready()
    {
        Initialize();
    }

    private void Initialize()
    {
        var settings = GameSettings.Instance;
        if (settings == null)
        {
            _config = new GameConfig();
        }
        else
        {
            _config = settings.GetLastActiveGameConfig();
        }

        LoadOrCreateEngine();
        _isInitialized = true;
        _coachNudgeService.RestartSession();

        UpdateUI();
    }

    private void LoadOrCreateEngine()
    {
        var saveManager = GameSaveManager.Instance;
        var statistics =
            GodotStatisticsTracker.Instance ?? (IStatisticsTracker)new NullStatisticsTracker();

        BestScore = saveManager?.GetBestScore(_config) ?? 0;

        // Try to load saved game
        var save = saveManager?.LoadGame(_config);
        if (save != null)
        {
            _engine = new Game2048Engine(
                save,
                _config,
                _random,
                statistics,
                _boardSimulator,
                _spawnStrategyFactory
            );
        }
        else
        {
            _engine = new Game2048Engine(
                _config,
                _random,
                statistics,
                _boardSimulator,
                _spawnStrategyFactory
            );
        }

        _engine.VictoryAchieved += OnEngineVictoryAchieved;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isInitialized || _isMoveLocked || IsGameOver)
            return;

        if (@event.IsActionPressed("move_up"))
        {
            _ = MoveAsync(Direction.Up);
        }
        else if (@event.IsActionPressed("move_down"))
        {
            _ = MoveAsync(Direction.Down);
        }
        else if (@event.IsActionPressed("move_left"))
        {
            _ = MoveAsync(Direction.Left);
        }
        else if (@event.IsActionPressed("move_right"))
        {
            _ = MoveAsync(Direction.Right);
        }
        else if (@event.IsActionPressed("undo"))
        {
            Undo();
        }
        else if (@event.IsActionPressed("new_game"))
        {
            NewGame();
        }
    }

    public async Task MoveAsync(Direction direction)
    {
        if (_isMoveLocked || IsGameOver)
            return;

        // In adversarial mode, player taps to spawn, not swipe to move
        if (IsAdversarialMode)
            return;

        _isMoveLocked = true;

        try
        {
            var previousBoard = _engine.CurrentState.Board.Clone();

            bool moved = _engine.Move(direction);

            if (moved)
            {
                _coachNudgeService.Reset();
                CoachNudge?.Hide();
                HapticsService.PlayMove();

                // Analyze the move for animations
                var analysis = _moveAnalyzer.Analyze(
                    previousBoard,
                    _engine.CurrentState.Board,
                    direction
                );

                // Animate tiles
                if (BoardVisual != null && analysis.Movements.Count > 0)
                {
                    await BoardVisual.AnimateMoveAsync(analysis.Movements.ToList());
                }

                UpdateUI(analysis.SpawnedIndices, analysis.MergedIndices);
                SaveGame();

                // Update best score
                if (Score > BestScore)
                {
                    BestScore = Score;
                    GameSaveManager.Instance?.UpdateBestScoreIfHigher(_config, Score);
                }

                // Block for a short time to prevent rapid moves
                await Task.Delay(100);
            }
            else
            {
                _coachNudgeService.TrackInvalidMove();

                if (_coachNudgeService.ShouldShowNudge(GameSettings.Instance))
                {
                    CoachNudge?.Show();
                }
            }

            if (IsGameOver)
            {
                GameEnded?.Invoke();
            }
        }
        finally
        {
            _isMoveLocked = false;
        }
    }

    /// <summary>
    /// Handles tile tap in Adversarial mode (player spawns tile, AI moves).
    /// </summary>
    public async Task TapEmptyCellAsync(int tileIndex)
    {
        if (!IsAdversarialMode || _isMoveLocked || IsGameOver)
            return;

        _isMoveLocked = true;

        try
        {
            int row = tileIndex / BoardSize;
            int col = tileIndex % BoardSize;
            var position = new Position(row, col);

            if (!_engine.TrySpawnExternalTile(position, out var spawnedValue))
            {
                return;
            }

            var previousBoard = _engine.CurrentState.Board.Clone();

            // Get AI's recommended move
            var recommendation = _moveAdvisor.Recommend(
                new MoveAdvisorRequest(
                    new PlayfieldSnapshot(_engine.CurrentState.Board, _engine.CurrentState.Wall),
                    _config
                )
            );

            if (recommendation != null)
            {
                bool moved = _engine.Move(recommendation.Value.Direction);

                if (moved)
                {
                    HapticsService.PlayMove();

                    var analysis = _moveAnalyzer.Analyze(
                        previousBoard,
                        _engine.CurrentState.Board,
                        recommendation.Value.Direction
                    );

                    if (BoardVisual != null && analysis.Movements.Count > 0)
                    {
                        await BoardVisual.AnimateMoveAsync(analysis.Movements.ToList());
                    }

                    UpdateUI(analysis.SpawnedIndices, analysis.MergedIndices);
                    SaveGame();
                }
            }
            else
            {
                // AI can't move - player wins
                UpdateUI();
            }

            if (IsGameOver)
            {
                GameEnded?.Invoke();
            }

            await Task.Delay(100);
        }
        finally
        {
            _isMoveLocked = false;
        }
    }

    public void Undo()
    {
        if (_engine.Undo())
        {
            UpdateUI();
            SaveGame();
        }
    }

    public void NewGame()
    {
        _engine.NewGame();
        _coachNudgeService.RestartSession();
        CoachNudge?.Hide();
        UpdateUI();
        SaveGame();
    }

    public void ChangeMode(int boardSize, GameMode mode)
    {
        var settings = GameSettings.Instance;

        _config = new GameConfig
        {
            Size = boardSize,
            Mode = mode,
            WinTile = 2048,
        };

        settings?.SetLastActiveGameConfig(_config);

        // Unsubscribe from old engine
        _engine.VictoryAchieved -= OnEngineVictoryAchieved;

        // Load or create new engine
        LoadOrCreateEngine();
        _coachNudgeService.RestartSession();
        CoachNudge?.Hide();

        // Rebuild board if size changed
        if (BoardVisual != null)
        {
            BoardVisual.BoardSize = boardSize;
        }

        UpdateUI();
    }

    public void EnableCoachFromNudge()
    {
        var settings = GameSettings.Instance;
        if (settings == null)
            return;

        settings.CoachEnabled = true;
        _coachNudgeService.Dismiss();
        CoachNudge?.Hide();
        UpdateCoachSuggestion();
    }

    public void DismissCoachNudge()
    {
        _coachNudgeService.Dismiss();
        CoachNudge?.Hide();
    }

    private void UpdateUI(
        IReadOnlySet<int>? newTileIndices = null,
        IReadOnlySet<int>? mergedIndices = null
    )
    {
        BoardVisual?.UpdateFromBoard(_engine.CurrentState.Board, newTileIndices, mergedIndices);

        // Update wall for Walltastrophy mode
        BoardVisual?.UpdateWall(_engine.CurrentState.Wall);

        if (ScoreLabel != null)
            ScoreLabel.Text = Score.ToString();

        if (BestScoreLabel != null)
            BestScoreLabel.Text = BestScore.ToString();

        if (SizeLabel != null)
            SizeLabel.Text = $"{BoardSize}x{BoardSize}";

        if (ModeLabel != null)
            ModeLabel.Text = GetModeDisplayName(_config.Mode);

        if (UndoButton != null)
            UndoButton.Disabled = !CanUndo;

        ScoreChanged?.Invoke();

        // Update coach suggestion if enabled
        UpdateCoachSuggestion();
    }

    private void UpdateCoachSuggestion()
    {
        var settings = GameSettings.Instance;
        if (settings?.CoachEnabled != true || IsGameOver)
        {
            CoachHint?.Hide();
            return;
        }

        var recommendation = _moveAdvisor.Recommend(
            new MoveAdvisorRequest(
                new PlayfieldSnapshot(_engine.CurrentState.Board, _engine.CurrentState.Wall),
                _config
            )
        );

        if (recommendation != null)
        {
            CoachSuggestionChanged?.Invoke(recommendation.Value.Direction);
            CoachHint?.Show();
        }
        else
        {
            CoachHint?.Hide();
        }
    }

    private void SaveGame()
    {
        GameSaveManager.Instance?.SaveGame(_config, _engine.ToSaveDto());
    }

    private void OnEngineVictoryAchieved(object? sender, EventArgs e)
    {
        if (_isInitialized)
        {
            VictoryAchieved?.Invoke();

            // Update best score for adversarial mode
            if (IsAdversarialMode)
            {
                if (BestScore == 0 || Score < BestScore)
                {
                    BestScore = Score;
                    GameSaveManager.Instance?.UpdateBestScoreIfHigher(_config, Score);
                }
            }
        }
    }

    public static string GetModeDisplayName(GameMode mode)
    {
        return mode switch
        {
            GameMode.Classic => Strings.ClassicMode,
            GameMode.Modern => Strings.ModernMode,
            GameMode.Walltastrophy => Strings.WalltastrophyMode,
            GameMode.Adversarial => Strings.AdversarialMode,
            _ => Strings.ClassicMode,
        };
    }
}
