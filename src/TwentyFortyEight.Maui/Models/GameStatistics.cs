namespace TwentyFortyEight.Maui.Models;

/// <summary>
/// Represents the game statistics tracked over time.
/// </summary>
public class GameStatistics
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
    /// Total cumulative time spent playing in seconds.
    /// </summary>
    public long TimePlayedSeconds { get; set; }

    /// <summary>
    /// Highest score ever achieved.
    /// </summary>
    public int BestScore { get; set; }

    /// <summary>
    /// Sum of all scores for calculating average.
    /// </summary>
    public long TotalScore { get; set; }

    /// <summary>
    /// The highest tile value ever achieved.
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
    /// Gets the win rate as a percentage.
    /// </summary>
    public double WinRate => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;

    /// <summary>
    /// Gets the average score across all completed games.
    /// </summary>
    public double AverageScore => GamesPlayed > 0 ? (double)TotalScore / GamesPlayed : 0;

    /// <summary>
    /// Gets formatted time played string.
    /// </summary>
    public string FormattedTimePlayed
    {
        get
        {
            var timeSpan = TimeSpan.FromSeconds(TimePlayedSeconds);
            if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
            }
            else
            {
                return $"{timeSpan.Seconds}s";
            }
        }
    }
}
