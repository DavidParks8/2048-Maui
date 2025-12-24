namespace TwentyFortyEight.Core;

/// <summary>
/// Abstract base class for tracking gameplay statistics.
/// Platform-specific implementations provide Save/Load persistence.
/// </summary>
public abstract class StatisticsTracker : IStatisticsTracker
{
    private readonly Lock _lock = new();
    private GameStatistics? _statistics;

    /// <summary>
    /// Saves the current statistics to persistent storage.
    /// </summary>
    protected abstract void Save(GameStatistics statistics);

    /// <summary>
    /// Loads statistics from persistent storage.
    /// </summary>
    /// <returns>The loaded statistics, or null if none exist.</returns>
    protected abstract GameStatistics? Load();

    /// <summary>
    /// Gets or lazily initializes the statistics.
    /// </summary>
    private GameStatistics Statistics
    {
        get
        {
            if (_statistics is null)
            {
                lock (_lock)
                {
                    _statistics ??= Load() ?? new GameStatistics();
                }
            }
            return _statistics;
        }
    }

    /// <summary>
    /// Gets the current statistics. Returns a snapshot copy.
    /// </summary>
    public GameStatistics GetStatistics()
    {
        lock (_lock)
        {
            return Statistics.Clone();
        }
    }

    /// <summary>
    /// Called when a new game is started. Increments games played.
    /// </summary>
    public void OnGameStarted()
    {
        lock (_lock)
        {
            var stats = Statistics;
            stats.GamesPlayed++;
            stats.CurrentGameWinCounted = false;
            stats.CurrentGameEnded = false;

            Save(stats);
        }
    }

    /// <summary>
    /// Called when a move is made. Updates move count.
    /// </summary>
    public void OnMoveMade()
    {
        lock (_lock)
        {
            Statistics.TotalMoves++;
            // Don't save on every move to avoid performance issues
        }
    }

    /// <summary>
    /// Called when the win tile (2048) is reached. Increments games won (only once per game).
    /// </summary>
    public void OnGameWon()
    {
        lock (_lock)
        {
            var stats = Statistics;
            // Only count as win once per game session
            if (!stats.CurrentGameWinCounted)
            {
                stats.GamesWon++;
                stats.CurrentStreak++;

                if (stats.CurrentStreak > stats.BestStreak)
                {
                    stats.BestStreak = stats.CurrentStreak;
                }

                stats.CurrentGameWinCounted = true;
                Save(stats);
            }
        }
    }

    /// <summary>
    /// Called when a game ends (game over or new game started while in progress).
    /// Finalizes statistics for that session.
    /// </summary>
    /// <param name="finalScore">The final score for the game.</param>
    /// <param name="wasWon">Whether the game was won.</param>
    public void OnGameEnded(int finalScore, bool wasWon)
    {
        lock (_lock)
        {
            var stats = Statistics;
            // Only record final score once per game
            if (!stats.CurrentGameEnded)
            {
                stats.TotalScore += finalScore;
                stats.CompletedGames++;
                stats.CurrentGameEnded = true;

                // If game was lost (not won), reset streak
                if (!wasWon && !stats.CurrentGameWinCounted)
                {
                    stats.CurrentStreak = 0;
                }

                Save(stats);
            }
        }
    }

    /// <summary>
    /// Updates the best score if the new score is higher.
    /// </summary>
    /// <param name="score">The score to check.</param>
    public void UpdateBestScore(int score)
    {
        lock (_lock)
        {
            var stats = Statistics;
            if (score > stats.BestScore)
            {
                stats.BestScore = score;
                Save(stats);
            }
        }
    }

    /// <summary>
    /// Updates the highest tile if the new tile value is higher.
    /// </summary>
    /// <param name="tileValue">The tile value to check.</param>
    public void UpdateHighestTile(int tileValue)
    {
        lock (_lock)
        {
            var stats = Statistics;
            if (tileValue > stats.HighestTile)
            {
                stats.HighestTile = tileValue;
                Save(stats);
            }
        }
    }

    /// <summary>
    /// Resets all statistics to their default values.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _statistics = new GameStatistics();
            Save(_statistics);
        }
    }
}
