using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Tests;

[TestClass]
public class AchievementTrackerTests
{
    [TestMethod]
    public void CheckTileAchievement_ReportsFirstMilestone()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        var unlocked = tracker.CheckTileAchievement(128);

        // Assert
        Assert.IsTrue(unlocked);
        Assert.AreEqual(128, tracker.LastUnlockedTileValue);
    }

    [TestMethod]
    public void CheckTileAchievement_DoesNotReportSameMilestoneTwice()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        tracker.CheckTileAchievement(128);
        var unlocked = tracker.CheckTileAchievement(128);

        // Assert
        Assert.IsFalse(unlocked);
        Assert.IsNull(tracker.LastUnlockedTileValue);
    }

    [TestMethod]
    public void CheckTileAchievement_ReportsMultipleMilestonesInSequence()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act & Assert
        Assert.IsTrue(tracker.CheckTileAchievement(128));
        Assert.AreEqual(128, tracker.LastUnlockedTileValue);

        Assert.IsTrue(tracker.CheckTileAchievement(256));
        Assert.AreEqual(256, tracker.LastUnlockedTileValue);

        Assert.IsTrue(tracker.CheckTileAchievement(512));
        Assert.AreEqual(512, tracker.LastUnlockedTileValue);
    }

    [TestMethod]
    public void CheckTileAchievement_IgnoresBelowMinimum()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        var unlocked = tracker.CheckTileAchievement(64);

        // Assert
        Assert.IsFalse(unlocked);
        Assert.IsNull(tracker.LastUnlockedTileValue);
    }

    [TestMethod]
    public void CheckScoreAchievement_ReportsFirstMilestone()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        var unlocked = tracker.CheckScoreAchievement(10000);

        // Assert
        Assert.IsTrue(unlocked);
        Assert.AreEqual(10000, tracker.LastUnlockedScoreMilestone);
    }

    [TestMethod]
    public void CheckScoreAchievement_DoesNotReportSameMilestoneTwice()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        tracker.CheckScoreAchievement(10000);
        var unlocked = tracker.CheckScoreAchievement(10000);

        // Assert
        Assert.IsFalse(unlocked);
        Assert.IsNull(tracker.LastUnlockedScoreMilestone);
    }

    [TestMethod]
    public void CheckScoreAchievement_ReportsMultipleMilestonesInOneCheck()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        var unlocked = tracker.CheckScoreAchievement(50000);

        // Assert - should unlock multiple at once
        Assert.IsTrue(unlocked);
        // The last one reported should be 50000
        Assert.AreEqual(50000, tracker.LastUnlockedScoreMilestone);
    }

    [TestMethod]
    public void CheckFirstWinAchievement_UnlocksOnFirstWin()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        var unlocked = tracker.CheckFirstWinAchievement(true);

        // Assert
        Assert.IsTrue(unlocked);
        Assert.IsTrue(tracker.FirstWinJustUnlocked);
    }

    [TestMethod]
    public void CheckFirstWinAchievement_DoesNotUnlockTwice()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        tracker.CheckFirstWinAchievement(true);
        var unlocked = tracker.CheckFirstWinAchievement(true);

        // Assert
        Assert.IsFalse(unlocked);
        Assert.IsFalse(tracker.FirstWinJustUnlocked);
    }

    [TestMethod]
    public void CheckFirstWinAchievement_DoesNotUnlockWhenNotWon()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act
        var unlocked = tracker.CheckFirstWinAchievement(false);

        // Assert
        Assert.IsFalse(unlocked);
        Assert.IsFalse(tracker.FirstWinJustUnlocked);
    }

    [TestMethod]
    public void ResetJustUnlocked_ClearsAllJustUnlockedFlags()
    {
        // Arrange
        var tracker = new AchievementTracker();
        tracker.CheckTileAchievement(128);
        tracker.CheckScoreAchievement(10000);
        tracker.CheckFirstWinAchievement(true);

        // Act
        tracker.ResetJustUnlocked();

        // Assert
        Assert.IsNull(tracker.LastUnlockedTileValue);
        Assert.IsNull(tracker.LastUnlockedScoreMilestone);
        Assert.IsFalse(tracker.FirstWinJustUnlocked);
    }

    [TestMethod]
    public void AchievementTracker_MaintainsStateAcrossMultipleChecks()
    {
        // Arrange
        var tracker = new AchievementTracker();

        // Act & Assert - Simulate game progression
        Assert.IsTrue(tracker.CheckTileAchievement(128));
        tracker.ResetJustUnlocked();

        Assert.IsFalse(tracker.CheckTileAchievement(128)); // Already unlocked
        Assert.IsTrue(tracker.CheckScoreAchievement(10000));
        tracker.ResetJustUnlocked();

        Assert.IsTrue(tracker.CheckTileAchievement(256));
        Assert.IsFalse(tracker.CheckTileAchievement(256)); // Already unlocked
        tracker.ResetJustUnlocked();

        Assert.IsTrue(tracker.CheckFirstWinAchievement(true));
        Assert.IsFalse(tracker.CheckFirstWinAchievement(true)); // Already unlocked
    }
}
