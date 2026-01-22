using Godot;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Game statistics tracking and persistence for Godot.
/// Implements IStatisticsTracker interface from Core.
/// </summary>
public partial class GodotStatisticsTracker : Node, IStatisticsTracker
{
    private const string StatsPath = "user://statistics.cfg";
    private const string StatsSection = "statistics";

    private readonly ConfigFile _config = new();
    private readonly Core.GameStatistics _stats = new();

    public static GodotStatisticsTracker? Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
        Load();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnGameStarted()
    {
        _stats.CurrentGameWinCounted = false;
        _stats.CurrentGameEnded = false;
        _stats.GamesPlayed++;
        Save();
    }

    public void OnGameEnded(int finalScore, bool wasWon)
    {
        if (_stats.CurrentGameEnded)
            return;

        _stats.CurrentGameEnded = true;
        _stats.TotalScore += finalScore;
        _stats.CompletedGames++;

        if (wasWon && !_stats.CurrentGameWinCounted)
        {
            _stats.CurrentStreak++;
            if (_stats.CurrentStreak > _stats.BestStreak)
                _stats.BestStreak = _stats.CurrentStreak;
        }
        else if (!wasWon)
        {
            _stats.CurrentStreak = 0;
        }

        Save();
    }

    public void OnGameWon()
    {
        if (_stats.CurrentGameWinCounted)
            return;

        _stats.CurrentGameWinCounted = true;
        _stats.GamesWon++;
        Save();
    }

    public void OnMoveMade()
    {
        _stats.TotalMoves++;
        // Don't save on every move - too expensive
    }

    public void UpdateHighestTile(int value)
    {
        if (value > _stats.HighestTile)
        {
            _stats.HighestTile = value;
            Save();
        }
    }

    public void UpdateBestScore(GameMode mode, int score)
    {
        // For adversarial mode, lower is better - handled separately
        if (mode == GameMode.Adversarial)
            return;

        if (score > _stats.BestScore)
        {
            _stats.BestScore = score;
            Save();
        }
    }

    public Core.GameStatistics GetStatistics()
    {
        return _stats.Clone();
    }

    public void Reset()
    {
        _stats.GamesPlayed = 0;
        _stats.GamesWon = 0;
        _stats.BestScore = 0;
        _stats.TotalScore = 0;
        _stats.CompletedGames = 0;
        _stats.HighestTile = 0;
        _stats.TotalMoves = 0;
        _stats.CurrentStreak = 0;
        _stats.BestStreak = 0;
        _stats.CurrentGameWinCounted = false;
        _stats.CurrentGameEnded = false;
        Save();
    }

    private void Load()
    {
        var error = _config.Load(StatsPath);
        if (error == Error.Ok)
        {
            _stats.GamesPlayed = (int)_config.GetValue(StatsSection, "games_played", 0);
            _stats.GamesWon = (int)_config.GetValue(StatsSection, "games_won", 0);
            _stats.BestScore = (int)_config.GetValue(StatsSection, "best_score", 0);
            _stats.TotalScore = (long)_config.GetValue(StatsSection, "total_score", 0L);
            _stats.CompletedGames = (int)_config.GetValue(StatsSection, "completed_games", 0);
            _stats.HighestTile = (int)_config.GetValue(StatsSection, "highest_tile", 0);
            _stats.TotalMoves = (long)_config.GetValue(StatsSection, "total_moves", 0L);
            _stats.CurrentStreak = (int)_config.GetValue(StatsSection, "current_streak", 0);
            _stats.BestStreak = (int)_config.GetValue(StatsSection, "best_streak", 0);
        }
    }

    private void Save()
    {
        _config.SetValue(StatsSection, "games_played", _stats.GamesPlayed);
        _config.SetValue(StatsSection, "games_won", _stats.GamesWon);
        _config.SetValue(StatsSection, "best_score", _stats.BestScore);
        _config.SetValue(StatsSection, "total_score", _stats.TotalScore);
        _config.SetValue(StatsSection, "completed_games", _stats.CompletedGames);
        _config.SetValue(StatsSection, "highest_tile", _stats.HighestTile);
        _config.SetValue(StatsSection, "total_moves", _stats.TotalMoves);
        _config.SetValue(StatsSection, "current_streak", _stats.CurrentStreak);
        _config.SetValue(StatsSection, "best_streak", _stats.BestStreak);

        var error = _config.Save(StatsPath);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to save statistics: {error}");
        }
    }
}
