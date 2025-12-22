using System.Diagnostics;
using TwentyFortyEight.Maui.Models;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Implementation of statistics service using Preferences for persistence.
/// </summary>
public class StatisticsService : IStatisticsService
{
    private const string KeyGamesPlayed = "Stats_GamesPlayed";
    private const string KeyGamesWon = "Stats_GamesWon";
    private const string KeyTimePlayedSeconds = "Stats_TimePlayedSeconds";
    private const string KeyBestScore = "Stats_BestScore";
    private const string KeyTotalScore = "Stats_TotalScore";
    private const string KeyHighestTile = "Stats_HighestTile";
    private const string KeyTotalMoves = "Stats_TotalMoves";
    private const string KeyCurrentStreak = "Stats_CurrentStreak";
    private const string KeyBestStreak = "Stats_BestStreak";

    private readonly Lock _lock = new();
    private Stopwatch? _gameTimer;
    private long _sessionStartSeconds;

    public GameStatistics GetStatistics()
    {
        lock (_lock)
        {
            // Calculate current session time if timer is running
            var currentTimePlayed = Preferences.Get(KeyTimePlayedSeconds, 0L);
            if (_gameTimer?.IsRunning == true)
            {
                currentTimePlayed += (long)_gameTimer.Elapsed.TotalSeconds;
            }

            return new GameStatistics
            {
                GamesPlayed = Preferences.Get(KeyGamesPlayed, 0),
                GamesWon = Preferences.Get(KeyGamesWon, 0),
                TimePlayedSeconds = currentTimePlayed,
                BestScore = Preferences.Get(KeyBestScore, 0),
                TotalScore = Preferences.Get(KeyTotalScore, 0L),
                HighestTile = Preferences.Get(KeyHighestTile, 0),
                TotalMoves = Preferences.Get(KeyTotalMoves, 0L),
                CurrentStreak = Preferences.Get(KeyCurrentStreak, 0),
                BestStreak = Preferences.Get(KeyBestStreak, 0)
            };
        }
    }

    public void IncrementGamesPlayed()
    {
        lock (_lock)
        {
            var current = Preferences.Get(KeyGamesPlayed, 0);
            Preferences.Set(KeyGamesPlayed, current + 1);
        }
    }

    public void IncrementGamesWon()
    {
        lock (_lock)
        {
            // Increment games won
            var gamesWon = Preferences.Get(KeyGamesWon, 0);
            Preferences.Set(KeyGamesWon, gamesWon + 1);

            // Update current streak
            var currentStreak = Preferences.Get(KeyCurrentStreak, 0);
            currentStreak++;
            Preferences.Set(KeyCurrentStreak, currentStreak);

            // Update best streak if needed
            var bestStreak = Preferences.Get(KeyBestStreak, 0);
            if (currentStreak > bestStreak)
            {
                Preferences.Set(KeyBestStreak, currentStreak);
            }
        }
    }

    public void RecordGameLoss()
    {
        lock (_lock)
        {
            // Reset current streak
            Preferences.Set(KeyCurrentStreak, 0);
        }
    }

    public void UpdateBestScore(int score)
    {
        lock (_lock)
        {
            var bestScore = Preferences.Get(KeyBestScore, 0);
            if (score > bestScore)
            {
                Preferences.Set(KeyBestScore, score);
            }
        }
    }

    public void AddScore(int score)
    {
        lock (_lock)
        {
            var totalScore = Preferences.Get(KeyTotalScore, 0L);
            Preferences.Set(KeyTotalScore, totalScore + score);
        }
    }

    public void UpdateHighestTile(int tile)
    {
        lock (_lock)
        {
            var highestTile = Preferences.Get(KeyHighestTile, 0);
            if (tile > highestTile)
            {
                Preferences.Set(KeyHighestTile, tile);
            }
        }
    }

    public void AddMoves(int moves)
    {
        lock (_lock)
        {
            var totalMoves = Preferences.Get(KeyTotalMoves, 0L);
            Preferences.Set(KeyTotalMoves, totalMoves + moves);
        }
    }

    public void AddTimePlayed(long seconds)
    {
        lock (_lock)
        {
            var timePlayed = Preferences.Get(KeyTimePlayedSeconds, 0L);
            Preferences.Set(KeyTimePlayedSeconds, timePlayed + seconds);
        }
    }

    public void StartTimeTracking()
    {
        lock (_lock)
        {
            if (_gameTimer == null)
            {
                _gameTimer = new Stopwatch();
            }

            if (!_gameTimer.IsRunning)
            {
                _sessionStartSeconds = Preferences.Get(KeyTimePlayedSeconds, 0L);
                _gameTimer.Restart();
            }
        }
    }

    public void StopTimeTracking()
    {
        lock (_lock)
        {
            if (_gameTimer?.IsRunning == true)
            {
                _gameTimer.Stop();
                
                // Persist accumulated time
                var elapsedSeconds = (long)_gameTimer.Elapsed.TotalSeconds;
                AddTimePlayed(elapsedSeconds);
                
                _gameTimer.Reset();
            }
        }
    }

    public void ResetStatistics()
    {
        lock (_lock)
        {
            Preferences.Remove(KeyGamesPlayed);
            Preferences.Remove(KeyGamesWon);
            Preferences.Remove(KeyTimePlayedSeconds);
            Preferences.Remove(KeyBestScore);
            Preferences.Remove(KeyTotalScore);
            Preferences.Remove(KeyHighestTile);
            Preferences.Remove(KeyTotalMoves);
            Preferences.Remove(KeyCurrentStreak);
            Preferences.Remove(KeyBestStreak);

            // Stop and reset timer
            _gameTimer?.Stop();
            _gameTimer?.Reset();
        }
    }
}
