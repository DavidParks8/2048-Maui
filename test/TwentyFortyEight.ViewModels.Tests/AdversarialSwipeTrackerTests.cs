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
    public void RecordSwipeAttempt_FourthAttemptWithinCooldown_ReturnsFalse()
    {
        // Arrange
        _tracker.RecordSwipeAttempt(); // First attempt
        _tracker.RecordSwipeAttempt(); // Second attempt - triggers hint and resets
        _tracker.RecordSwipeAttempt(); // Third attempt (first after reset)

        // Act - Fourth attempt is within cooldown period
        bool shouldShowHint = _tracker.RecordSwipeAttempt(); // Fourth attempt (second after reset)

        // Assert
        Assert.IsFalse(shouldShowHint, "Should not trigger hint within cooldown period");
    }

    [TestMethod]
    public async Task RecordSwipeAttempt_FourthAttemptAfterCooldown_ReturnsTrue()
    {
        // Arrange
        _tracker.RecordSwipeAttempt(); // First attempt
        _tracker.RecordSwipeAttempt(); // Second attempt - triggers hint and resets
        _tracker.RecordSwipeAttempt(); // Third attempt (first after reset)

        // Wait for cooldown period to expire (3 seconds + buffer)
        await Task.Delay(3100);

        // Act - Fourth attempt after cooldown
        bool shouldShowHint = _tracker.RecordSwipeAttempt(); // Fourth attempt (second after reset)

        // Assert
        Assert.IsTrue(shouldShowHint, "Should trigger hint after cooldown period expires");
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
    public void RecordSwipeAttempt_ConsecutiveCalls_FollowsExpectedPatternWithCooldown()
    {
        // Test the pattern with cooldown: false, true, false (within cooldown), false (still within cooldown)
        Assert.IsFalse(_tracker.RecordSwipeAttempt(), "Attempt 1 should not trigger");
        Assert.IsTrue(_tracker.RecordSwipeAttempt(), "Attempt 2 should trigger");
        Assert.IsFalse(_tracker.RecordSwipeAttempt(), "Attempt 3 should not trigger");
        Assert.IsFalse(_tracker.RecordSwipeAttempt(), "Attempt 4 should not trigger (within cooldown)");
    }

    [TestMethod]
    public async Task RecordSwipeAttempt_ThreadSafety_MultipleThreads()
    {
        // Arrange
        const int threadCount = 10;
        const int attemptsPerThread = 10;
        int triggerCount = 0;
        var tasks = new List<Task>();

        // Act - Multiple threads calling RecordSwipeAttempt concurrently
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < attemptsPerThread; j++)
                {
                    if (_tracker.RecordSwipeAttempt())
                    {
                        Interlocked.Increment(ref triggerCount);
                    }
                    Thread.Sleep(10); // Small delay between attempts
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should have triggered at least once without crashes
        Assert.IsTrue(triggerCount > 0, "Should have triggered at least once");
        Assert.IsTrue(triggerCount <= (threadCount * attemptsPerThread) / 2, 
            "Should not trigger more than expected given the threshold and cooldown");
    }
}
