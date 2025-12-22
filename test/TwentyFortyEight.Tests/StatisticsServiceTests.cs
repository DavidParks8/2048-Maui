using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Maui.Services;
using TwentyFortyEight.Maui.Models;

namespace TwentyFortyEight.Tests;

[TestClass]
public class StatisticsServiceTests
{
    [TestMethod]
    public void GetStatistics_InitialState_ReturnsZeroValues()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics(); // Ensure clean state

        // Act
        var stats = service.GetStatistics();

        // Assert
        Assert.AreEqual(0, stats.GamesPlayed);
        Assert.AreEqual(0, stats.GamesWon);
        Assert.AreEqual(0, stats.BestScore);
        Assert.AreEqual(0, stats.HighestTile);
        Assert.AreEqual(0L, stats.TotalMoves);
        Assert.AreEqual(0, stats.CurrentStreak);
        Assert.AreEqual(0, stats.BestStreak);
    }

    [TestMethod]
    public void IncrementGamesPlayed_IncrementsCounter()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics();

        // Act
        service.IncrementGamesPlayed();
        service.IncrementGamesPlayed();

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(2, stats.GamesPlayed);
    }

    [TestMethod]
    public void IncrementGamesWon_UpdatesStreaks()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics();

        // Act
        service.IncrementGamesWon();
        service.IncrementGamesWon();

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(2, stats.GamesWon);
        Assert.AreEqual(2, stats.CurrentStreak);
        Assert.AreEqual(2, stats.BestStreak);
    }

    [TestMethod]
    public void RecordGameLoss_ResetsCurrentStreak()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics();
        service.IncrementGamesWon();
        service.IncrementGamesWon();

        // Act
        service.RecordGameLoss();

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(0, stats.CurrentStreak);
        Assert.AreEqual(2, stats.BestStreak); // Best streak should remain
    }

    [TestMethod]
    public void UpdateBestScore_OnlyUpdatesWhenHigher()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics();

        // Act
        service.UpdateBestScore(100);
        service.UpdateBestScore(50); // Should not update
        service.UpdateBestScore(200); // Should update

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(200, stats.BestScore);
    }

    [TestMethod]
    public void UpdateHighestTile_OnlyUpdatesWhenHigher()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics();

        // Act
        service.UpdateHighestTile(2048);
        service.UpdateHighestTile(1024); // Should not update
        service.UpdateHighestTile(4096); // Should update

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(4096, stats.HighestTile);
    }

    [TestMethod]
    public void AddMoves_AccumulatesTotal()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics();

        // Act
        service.AddMoves(10);
        service.AddMoves(20);

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(30L, stats.TotalMoves);
    }

    [TestMethod]
    public void WinRate_CalculatesCorrectly()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics();

        // Act
        service.IncrementGamesPlayed();
        service.IncrementGamesPlayed();
        service.IncrementGamesPlayed();
        service.IncrementGamesPlayed();
        service.IncrementGamesWon();

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(25.0, stats.WinRate, 0.01);
    }

    [TestMethod]
    public void AverageScore_CalculatesCorrectly()
    {
        // Arrange
        var service = new StatisticsService();
        service.ResetStatistics();

        // Act
        service.IncrementGamesPlayed();
        service.IncrementGamesPlayed();
        service.AddScore(100);
        service.AddScore(200);

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(150.0, stats.AverageScore, 0.01);
    }

    [TestMethod]
    public void ResetStatistics_ClearsAllData()
    {
        // Arrange
        var service = new StatisticsService();
        service.IncrementGamesPlayed();
        service.IncrementGamesWon();
        service.UpdateBestScore(1000);
        service.AddMoves(100);

        // Act
        service.ResetStatistics();

        // Assert
        var stats = service.GetStatistics();
        Assert.AreEqual(0, stats.GamesPlayed);
        Assert.AreEqual(0, stats.GamesWon);
        Assert.AreEqual(0, stats.BestScore);
        Assert.AreEqual(0L, stats.TotalMoves);
    }
}
