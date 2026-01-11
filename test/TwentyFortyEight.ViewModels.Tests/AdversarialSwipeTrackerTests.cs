using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for AdversarialSwipeTracker.
/// </summary>
[TestClass]
public class AdversarialSwipeTrackerTests
{
    private IAdversarialSwipeTracker _tracker = null!;

    [TestInitialize]
    public void Setup()
    {
        _tracker = new AdversarialSwipeTracker();
    }

    [TestMethod]
    public void RecordSwipeAttempt_FirstAttempt_ReturnsFalse()
    {
        // Act
        bool shouldShowHint = _tracker.RecordSwipeAttempt();

        // Assert
        Assert.IsFalse(shouldShowHint, "First swipe attempt should not trigger hint");
    }

    [TestMethod]
    public void RecordSwipeAttempt_SecondAttempt_ReturnsTrue()
    {
        // Arrange
        _tracker.RecordSwipeAttempt(); // First attempt

        // Act
        bool shouldShowHint = _tracker.RecordSwipeAttempt(); // Second attempt

        // Assert
        Assert.IsTrue(shouldShowHint, "Second swipe attempt should trigger hint");
    }

    [TestMethod]
    public void RecordSwipeAttempt_ThirdAttemptAfterHint_ReturnsFalse()
    {
        // Arrange
        _tracker.RecordSwipeAttempt(); // First attempt
        _tracker.RecordSwipeAttempt(); // Second attempt - triggers hint and resets

        // Act
        bool shouldShowHint = _tracker.RecordSwipeAttempt(); // Third attempt (first after reset)

        // Assert
        Assert.IsFalse(shouldShowHint, "First swipe attempt after reset should not trigger hint");
    }

    [TestMethod]
    public void RecordSwipeAttempt_FourthAttempt_ReturnsTrue()
    {
        // Arrange
        _tracker.RecordSwipeAttempt(); // First attempt
        _tracker.RecordSwipeAttempt(); // Second attempt - triggers hint and resets
        _tracker.RecordSwipeAttempt(); // Third attempt (first after reset)

        // Act
        bool shouldShowHint = _tracker.RecordSwipeAttempt(); // Fourth attempt (second after reset)

        // Assert
        Assert.IsTrue(shouldShowHint, "Second swipe attempt after reset should trigger hint");
    }

    [TestMethod]
    public void Reset_AfterOneAttempt_NextAttemptDoesNotTriggerHint()
    {
        // Arrange
        _tracker.RecordSwipeAttempt(); // First attempt

        // Act
        _tracker.Reset();
        bool shouldShowHint = _tracker.RecordSwipeAttempt(); // First attempt after reset

        // Assert
        Assert.IsFalse(shouldShowHint, "First swipe attempt after manual reset should not trigger hint");
    }

    [TestMethod]
    public void Reset_AfterTwoAttempts_NextAttemptDoesNotTriggerHint()
    {
        // Arrange
        _tracker.RecordSwipeAttempt(); // First attempt
        _tracker.RecordSwipeAttempt(); // Second attempt - triggers hint and auto-resets

        // Act
        _tracker.Reset(); // Explicit reset (redundant but valid)
        bool shouldShowHint = _tracker.RecordSwipeAttempt(); // First attempt after reset

        // Assert
        Assert.IsFalse(shouldShowHint, "First swipe attempt after reset should not trigger hint");
    }

    [TestMethod]
    public void MultipleResets_DoNotCauseIssues()
    {
        // Arrange & Act
        _tracker.Reset();
        _tracker.Reset();
        _tracker.Reset();
        bool shouldShowHint = _tracker.RecordSwipeAttempt();

        // Assert
        Assert.IsFalse(shouldShowHint, "Multiple resets should not cause issues");
    }

    [TestMethod]
    public void RecordSwipeAttempt_ConsecutiveCalls_FollowsExpectedPattern()
    {
        // Test the complete pattern: false, true, false, true, false, true
        Assert.IsFalse(_tracker.RecordSwipeAttempt(), "Attempt 1 should not trigger");
        Assert.IsTrue(_tracker.RecordSwipeAttempt(), "Attempt 2 should trigger");
        Assert.IsFalse(_tracker.RecordSwipeAttempt(), "Attempt 3 should not trigger");
        Assert.IsTrue(_tracker.RecordSwipeAttempt(), "Attempt 4 should trigger");
        Assert.IsFalse(_tracker.RecordSwipeAttempt(), "Attempt 5 should not trigger");
        Assert.IsTrue(_tracker.RecordSwipeAttempt(), "Attempt 6 should trigger");
    }
}
