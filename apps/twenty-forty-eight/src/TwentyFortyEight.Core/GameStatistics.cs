namespace TwentyFortyEight.Core;

/// <summary>
/// Data model for storing comprehensive gameplay statistics.
/// Platform-agnostic - can be used by any UI framework.
/// </summary>
public sealed class GameStatistics
{
    /// <summary>
    /// Total number of games started.
    /// </summary>
    public int GamesPlayed { get; set; }

    /// <summary>
    /// Number of games where the player reached the 2048 tile.
    /// </summary>
    public int GamesWon { get; set; }

    /// <summary>
    /// Highest score ever achieved.
    /// </summary>
    public int BestScore { get; set; }

    /// <summary>
    /// Total accumulated score across all completed games.
    /// </summary>
    public long TotalScore { get; set; }

    /// <summary>
    /// Number of completed games (for average calculation).
    /// </summary>
    public int CompletedGames { get; set; }

    /// <summary>
    /// The highest tile value ever achieved (2048, 4096, 8192, etc.).
    /// </summary>
    public int HighestTile { get; set; }

    /// <summary>
    /// Cumulative number of moves made across all games.
    /// </summary>
    public long TotalMoves { get; set; }

    /// <summary>
    /// Current consecutive win streak.
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Longest consecutive win streak.
    /// </summary>
    public int BestStreak { get; set; }

    /// <summary>
    /// Whether the current game session has already been counted as a win.
    /// Prevents counting the same game as a win multiple times.
    /// </summary>
    public bool CurrentGameWinCounted { get; set; }

    /// <summary>
    /// Whether the current game's final score has been recorded.
    /// Prevents counting the same game multiple times in statistics.
    /// </summary>
    public bool CurrentGameEnded { get; set; }

    /// <summary>
    /// Calculates the win rate as a percentage.
    /// </summary>
    public double WinRate => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;

    /// <summary>
    /// Calculates the average score across all completed games.
    /// </summary>
    public int AverageScore => CompletedGames > 0 ? (int)(TotalScore / CompletedGames) : 0;

    /// <summary>
    /// Creates a deep copy of the statistics.
    /// </summary>
    public GameStatistics Clone() =>
        new()
        {
            GamesPlayed = GamesPlayed,
            GamesWon = GamesWon,
            BestScore = BestScore,
            TotalScore = TotalScore,
            CompletedGames = CompletedGames,
            HighestTile = HighestTile,
            TotalMoves = TotalMoves,
            CurrentStreak = CurrentStreak,
            BestStreak = BestStreak,
            CurrentGameWinCounted = CurrentGameWinCounted,
            CurrentGameEnded = CurrentGameEnded,
        };
}
