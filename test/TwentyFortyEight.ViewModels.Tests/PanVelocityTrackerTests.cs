using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class PanVelocityTrackerTests
{
    #region Basic Velocity Tracking Tests

    [TestMethod]
    public void GetVelocity_NoSamples_ReturnsZero()
    {
        // Arrange
        var tracker = new PanVelocityTracker();

        // Act
        var velocity = tracker.GetVelocity();

        // Assert
        Assert.AreEqual(0.0, velocity);
    }

    [TestMethod]
    public void GetVelocity_SingleSample_ReturnsZero()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var now = DateTime.UtcNow;

        // Act
        tracker.RecordSample(0, 0, now);
        var velocity = tracker.GetVelocity();

        // Assert
        Assert.AreEqual(0.0, velocity, "Single sample cannot compute velocity");
    }

    [TestMethod]
    public void GetVelocity_TwoSamplesDownward_ReturnsPositiveVelocity()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - simulate downward swipe: Y increases by 100 over 100ms
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(5, 100, startTime.AddMilliseconds(100));
        var velocity = tracker.GetVelocity();

        // Assert - 100px / 0.1s = 1000 px/s
        Assert.AreEqual(1000.0, velocity, 1.0, "Downward swipe should have positive velocity");
    }

    [TestMethod]
    public void GetVelocity_TwoSamplesUpward_ReturnsNegativeVelocity()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - simulate upward swipe: Y decreases by 100 over 100ms
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(5, -100, startTime.AddMilliseconds(100));
        var velocity = tracker.GetVelocity();

        // Assert - -100px / 0.1s = -1000 px/s
        Assert.AreEqual(-1000.0, velocity, 1.0, "Upward swipe should have negative velocity");
    }

    [TestMethod]
    public void GetVelocity_FastSwipeDown_ReturnsHighVelocity()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - fast swipe: 200px in 50ms = 4000 px/s
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(10, 200, startTime.AddMilliseconds(50));
        var velocity = tracker.GetVelocity();

        // Assert
        Assert.AreEqual(4000.0, velocity, 10.0, "Fast swipe should return high velocity");
    }

    [TestMethod]
    public void GetVelocity_SlowSwipe_ReturnsLowVelocity()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - slow swipe: 50px in 100ms = 500 px/s
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(5, 50, startTime.AddMilliseconds(100));
        var velocity = tracker.GetVelocity();

        // Assert
        Assert.AreEqual(500.0, velocity, 5.0, "Slow swipe should return low velocity");
    }

    #endregion

    #region Multi-Sample Tests

    [TestMethod]
    public void GetVelocity_MultipleSamples_ReturnsLastComputedVelocity()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - multiple samples with acceleration at end
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(2, 20, startTime.AddMilliseconds(50)); // 400 px/s
        tracker.RecordSample(4, 50, startTime.AddMilliseconds(100)); // 600 px/s
        tracker.RecordSample(6, 150, startTime.AddMilliseconds(150)); // 2000 px/s
        var velocity = tracker.GetVelocity();

        // Assert - should capture the last segment's velocity (2000 px/s)
        Assert.AreEqual(2000.0, velocity, 10.0, "Should return velocity from last segment");
    }

    [TestMethod]
    public void GetVelocity_SwipeWithPauseAtEnd_ReturnsLastValidVelocity()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - fast swipe then pause
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(5, 100, startTime.AddMilliseconds(50)); // 2000 px/s
        // Pause for 200ms (beyond maxTimeDelta of 150ms)
        tracker.RecordSample(5, 100, startTime.AddMilliseconds(250));
        var velocity = tracker.GetVelocity();

        // Assert - the pause sample (200ms gap) exceeds maxTimeDelta, so velocity from pause is 0
        // But we still keep the last computed velocity from the fast segment
        Assert.AreEqual(2000.0, velocity, 10.0, "Should retain velocity from before pause");
    }

    #endregion

    #region Reset Tests

    [TestMethod]
    public void Reset_ClearsVelocity()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(5, 100, startTime.AddMilliseconds(100));

        // Act
        tracker.Reset();
        var velocity = tracker.GetVelocity();

        // Assert
        Assert.AreEqual(0.0, velocity, "Reset should clear velocity");
    }

    [TestMethod]
    public void Reset_AllowsNewGesture()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // First gesture - downward
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(5, 100, startTime.AddMilliseconds(100));

        // Act - reset and new gesture (upward)
        tracker.Reset();
        var newStart = DateTime.UtcNow;
        tracker.RecordSample(0, 0, newStart);
        tracker.RecordSample(3, -50, newStart.AddMilliseconds(100));
        var velocity = tracker.GetVelocity();

        // Assert - should be negative (upward)
        Assert.AreEqual(
            -500.0,
            velocity,
            5.0,
            "New gesture after reset should track independently"
        );
    }

    #endregion

    #region Diagonal Swipe Tests

    [TestMethod]
    public void GetVelocity_DiagonalSwipe_ReturnsZero()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - diagonal swipe (45 degrees)
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(100, 100, startTime.AddMilliseconds(100));
        var velocity = tracker.GetVelocity();

        // Assert
        Assert.AreEqual(0.0, velocity, "Diagonal swipe should return 0 velocity");
    }

    [TestMethod]
    public void GetVelocity_MostlyHorizontalSwipe_ReturnsZero()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - mostly horizontal swipe
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(100, 30, startTime.AddMilliseconds(100));
        var velocity = tracker.GetVelocity();

        // Assert
        Assert.AreEqual(0.0, velocity, "Horizontal swipe should return 0 velocity");
    }

    #endregion

    #region Time Delta Edge Cases

    [TestMethod]
    public void GetVelocity_TooSmallTimeDelta_ReturnsZero()
    {
        // Arrange - use default min of 0.001s (1ms)
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - samples too close together (0.5ms)
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(5, 100, startTime.AddTicks(5000)); // 0.5ms in ticks
        var velocity = tracker.GetVelocity();

        // Assert
        Assert.AreEqual(0.0, velocity, "Too small time delta should return 0");
    }

    [TestMethod]
    public void GetVelocity_TooLargeTimeDelta_PreservesLastValidVelocity()
    {
        // Arrange
        var tracker = new PanVelocityTracker();
        var startTime = DateTime.UtcNow;

        // Act - first a valid sample, then a pause
        tracker.RecordSample(0, 0, startTime);
        tracker.RecordSample(5, 100, startTime.AddMilliseconds(50)); // Valid: 2000 px/s
        tracker.RecordSample(5, 100, startTime.AddMilliseconds(600)); // 550ms gap - too large (>150ms)
        var velocity = tracker.GetVelocity();

        // Assert - should keep the last valid velocity
        Assert.AreEqual(2000.0, velocity, 10.0);
    }

    #endregion
}
