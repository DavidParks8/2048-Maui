using TwentyFortyEight.Core;

namespace TwentyFortyEight.Tests;

/// <summary>
/// Null implementation of IStatisticsTracker for tests that don't need statistics.
/// </summary>
internal sealed class NullStatisticsTracker : IStatisticsTracker
{
    public static NullStatisticsTracker Instance { get; } = new();

    public GameStatistics GetStatistics() => new();

    public void OnGameStarted() { }

    public void OnMoveMade() { }

    public void OnGameWon() { }

    public void OnGameEnded(int finalScore, bool wasWon) { }

    public void UpdateBestScore(int score) { }

    public void UpdateHighestTile(int tileValue) { }

    public void Reset() { }
}

/// <summary>
/// Test implementation of StatisticsTracker that stores data in memory.
/// </summary>
internal sealed class InMemoryStatisticsTracker : StatisticsTracker
{
    private GameStatistics? _savedStatistics;

    public int SaveCount { get; private set; }

    protected override void Save(GameStatistics statistics)
    {
        _savedStatistics = statistics.Clone();
        SaveCount++;
    }

    protected override GameStatistics? Load()
    {
        return _savedStatistics?.Clone();
    }
}

/// <summary>
/// Unit tests for the StatisticsTracker class.
/// </summary>
[TestClass]
[TestCategory("Statistics")]
public class StatisticsTrackerTests
{
    [TestMethod]
    public void OnGameStarted_IncrementsGamesPlayed()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();

        // Act
        tracker.OnGameStarted();

        // Assert
        var stats = tracker.GetStatistics();
        Assert.AreEqual(1, stats.GamesPlayed);
        Assert.IsFalse(stats.CurrentGameEnded);
        Assert.IsFalse(stats.CurrentGameWinCounted);
    }

    [TestMethod]
    public void OnMoveMade_IncrementsTotalMoves()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();
        tracker.OnGameStarted();

        // Act
        tracker.OnMoveMade();
        tracker.OnMoveMade();
        tracker.OnMoveMade();

        // Assert
        var stats = tracker.GetStatistics();
        Assert.AreEqual(3, stats.TotalMoves);
    }

    [TestMethod]
    public void OnGameWon_IncrementsGamesWonAndStreak()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();
        tracker.OnGameStarted();

        // Act
        tracker.OnGameWon();

        // Assert
        var stats = tracker.GetStatistics();
        Assert.AreEqual(1, stats.GamesWon);
        Assert.AreEqual(1, stats.CurrentStreak);
        Assert.AreEqual(1, stats.BestStreak);
        Assert.IsTrue(stats.CurrentGameWinCounted);
    }

    [TestMethod]
    public void OnGameWon_OnlyCountsOncePerGame()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();
        tracker.OnGameStarted();

        // Act - call OnGameWon multiple times
        tracker.OnGameWon();
        tracker.OnGameWon();
        tracker.OnGameWon();

        // Assert - should only count once
        var stats = tracker.GetStatistics();
        Assert.AreEqual(1, stats.GamesWon);
        Assert.AreEqual(1, stats.CurrentStreak);
    }

    [TestMethod]
    public void OnGameEnded_UpdatesCompletedGamesAndTotalScore()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();
        tracker.OnGameStarted();

        // Act
        tracker.OnGameEnded(1000, wasWon: false);

        // Assert
        var stats = tracker.GetStatistics();
        Assert.AreEqual(1, stats.CompletedGames);
        Assert.AreEqual(1000, stats.TotalScore);
        Assert.IsTrue(stats.CurrentGameEnded);
    }

    [TestMethod]
    public void OnGameEnded_ResetsStreakOnLoss()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();

        // Win two games to build streak
        tracker.OnGameStarted();
        tracker.OnGameWon();
        tracker.OnGameEnded(500, wasWon: true);

        tracker.OnGameStarted();
        tracker.OnGameWon();
        tracker.OnGameEnded(600, wasWon: true);

        // Verify streak is 2
        Assert.AreEqual(2, tracker.GetStatistics().CurrentStreak);

        // Start and lose a game
        tracker.OnGameStarted();

        // Act
        tracker.OnGameEnded(300, wasWon: false);

        // Assert
        var stats = tracker.GetStatistics();
        Assert.AreEqual(0, stats.CurrentStreak);
        Assert.AreEqual(2, stats.BestStreak);
    }

    [TestMethod]
    public void UpdateBestScore_UpdatesOnlyWhenHigher()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();

        // Act
        tracker.UpdateBestScore(500);
        tracker.UpdateBestScore(300); // Lower, should not update
        tracker.UpdateBestScore(800); // Higher, should update

        // Assert
        var stats = tracker.GetStatistics();
        Assert.AreEqual(800, stats.BestScore);
    }

    [TestMethod]
    public void UpdateHighestTile_UpdatesOnlyWhenHigher()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();

        // Act
        tracker.UpdateHighestTile(256);
        tracker.UpdateHighestTile(128); // Lower, should not update
        tracker.UpdateHighestTile(2048); // Higher, should update

        // Assert
        var stats = tracker.GetStatistics();
        Assert.AreEqual(2048, stats.HighestTile);
    }

    [TestMethod]
    public void Reset_ClearsAllStatistics()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();
        tracker.OnGameStarted();
        tracker.OnMoveMade();
        tracker.UpdateBestScore(1000);
        tracker.OnGameWon();
        tracker.OnGameEnded(1000, wasWon: true);

        // Act
        tracker.Reset();

        // Assert
        var stats = tracker.GetStatistics();
        Assert.AreEqual(0, stats.GamesPlayed);
        Assert.AreEqual(0, stats.GamesWon);
        Assert.AreEqual(0, stats.TotalMoves);
        Assert.AreEqual(0, stats.BestScore);
        Assert.AreEqual(0, stats.CurrentStreak);
    }

    [TestMethod]
    public void GetStatistics_ReturnsSnapshot()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();
        tracker.OnGameStarted();

        // Act
        var stats1 = tracker.GetStatistics();
        tracker.OnGameStarted(); // Modify tracker after getting stats
        var stats2 = tracker.GetStatistics();

        // Assert - stats1 should not be affected by subsequent changes
        Assert.AreEqual(1, stats1.GamesPlayed);
        Assert.AreEqual(2, stats2.GamesPlayed);
    }

    [TestMethod]
    public void Save_CalledOnSignificantChanges()
    {
        // Arrange
        var tracker = new InMemoryStatisticsTracker();

        // Act - perform operations that should trigger saves
        tracker.OnGameStarted(); // Should save (1)
        tracker.OnGameWon(); // Should save (2)
        tracker.UpdateBestScore(100); // Should save (3)
        tracker.OnGameEnded(100, true); // Should save (4)

        // Assert - at least 4 saves should have occurred
        Assert.AreEqual(4, tracker.SaveCount);
    }

    [TestMethod]
    public void WinRate_CalculatesCorrectly()
    {
        // Arrange
        var stats = new GameStatistics { GamesPlayed = 10, GamesWon = 3 };

        // Assert
        Assert.AreEqual(30.0, stats.WinRate);
    }

    [TestMethod]
    public void AverageScore_CalculatesCorrectly()
    {
        // Arrange
        var stats = new GameStatistics { CompletedGames = 5, TotalScore = 5000 };

        // Assert
        Assert.AreEqual(1000, stats.AverageScore);
    }

    [TestMethod]
    public void Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new GameStatistics
        {
            GamesPlayed = 10,
            GamesWon = 5,
            BestScore = 50000,
            CurrentStreak = 3,
        };

        // Act
        var clone = original.Clone();
        clone.GamesPlayed = 20; // Modify clone

        // Assert - original should be unchanged
        Assert.AreEqual(10, original.GamesPlayed);
        Assert.AreEqual(20, clone.GamesPlayed);
    }
}
