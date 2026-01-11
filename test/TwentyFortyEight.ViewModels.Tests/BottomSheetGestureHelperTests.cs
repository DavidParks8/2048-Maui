using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class BottomSheetGestureHelperTests
{
    #region IsSwipeMostlyVertical Tests

    [TestMethod]
    public void IsSwipeMostlyVertical_PureVerticalSwipe_ReturnsTrue()
    {
        // Arrange - pure vertical movement
        double deltaX = 0;
        double deltaY = 100;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_NegligibleHorizontalMovement_ReturnsTrue()
    {
        // Arrange - very small horizontal movement
        double deltaX = 0.0005;
        double deltaY = 100;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_MostlyVertical_ReturnsTrue()
    {
        // Arrange - vertical movement is 2x horizontal (ratio = 2.0)
        double deltaX = 30;
        double deltaY = 60;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsTrue(result, "Swipe with vertical/horizontal ratio of 2.0 should be considered vertical");
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_DiagonalSwipe_ReturnsFalse()
    {
        // Arrange - equal horizontal and vertical movement (45 degree angle)
        double deltaX = 50;
        double deltaY = 50;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsFalse(result, "45-degree diagonal swipe should not be considered vertical");
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_MostlyHorizontal_ReturnsFalse()
    {
        // Arrange - horizontal movement is greater than vertical
        double deltaX = 100;
        double deltaY = 30;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_NearThreshold_ReturnsCorrectly()
    {
        // Arrange - test ratio exactly at threshold (1.5)
        double deltaX = 40;
        double deltaY = 60; // ratio = 1.5

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert - at threshold, should not be vertical (threshold is exclusive)
        Assert.IsFalse(result, "Swipe exactly at threshold should not be considered vertical");
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_JustAboveThreshold_ReturnsTrue()
    {
        // Arrange - test ratio just above threshold (1.51)
        double deltaX = 40;
        double deltaY = 60.4; // ratio = 1.51

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsTrue(result, "Swipe just above threshold should be considered vertical");
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_NegativeValues_HandleCorrectly()
    {
        // Arrange - negative values (upward or leftward movement)
        double deltaX = -30;
        double deltaY = -60;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsTrue(result, "Should use absolute values and detect vertical movement");
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_MixedSigns_HandleCorrectly()
    {
        // Arrange - mixed signs (different directions)
        double deltaX = 25;
        double deltaY = -50;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsTrue(result, "Should use absolute values for ratio calculation");
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_ZeroVertical_ReturnsFalse()
    {
        // Arrange - pure horizontal movement
        double deltaX = 100;
        double deltaY = 0;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsSwipeMostlyVertical_BothZero_ReturnsTrue()
    {
        // Arrange - no movement at all
        double deltaX = 0;
        double deltaY = 0;

        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.IsTrue(result, "Zero horizontal movement should return true");
    }

    [TestMethod]
    [DataRow(0, 100, true)]      // Pure vertical
    [DataRow(10, 100, true)]     // Mostly vertical (ratio 10)
    [DataRow(30, 60, true)]      // Mostly vertical (ratio 2)
    [DataRow(40, 61, true)]      // Just above threshold (ratio 1.525)
    [DataRow(50, 50, false)]     // Diagonal (ratio 1)
    [DataRow(60, 30, false)]     // Mostly horizontal (ratio 0.5)
    [DataRow(100, 10, false)]    // Mostly horizontal (ratio 0.1)
    [DataRow(-30, -60, true)]    // Negative vertical
    [DataRow(25, -50, true)]     // Mixed signs vertical
    [DataRow(-50, 25, false)]    // Mixed signs horizontal
    public void IsSwipeMostlyVertical_DataDriven_ReturnsExpected(
        double deltaX,
        double deltaY,
        bool expected)
    {
        // Act
        var result = BottomSheetGestureHelper.IsSwipeMostlyVertical(deltaX, deltaY);

        // Assert
        Assert.AreEqual(expected, result,
            $"For deltaX={deltaX}, deltaY={deltaY}, expected {expected} but got {result}");
    }

    #endregion

    #region CalculateSwipeVelocity Tests

    [TestMethod]
    public void CalculateSwipeVelocity_VerticalSwipeWithValidTime_ReturnsVelocity()
    {
        // Arrange - mostly vertical swipe with reasonable time
        double deltaX = 10;
        double deltaY = 100;
        double timeDelta = 0.1; // 100ms

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(deltaX, deltaY, timeDelta);

        // Assert
        Assert.AreEqual(1000.0, result, 0.01, "Velocity should be 100px / 0.1s = 1000 px/s");
    }

    [TestMethod]
    public void CalculateSwipeVelocity_DiagonalSwipe_ReturnsZero()
    {
        // Arrange - diagonal swipe (50/50)
        double deltaX = 50;
        double deltaY = 50;
        double timeDelta = 0.1;

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(deltaX, deltaY, timeDelta);

        // Assert
        Assert.AreEqual(0.0, result, "Diagonal swipe should return 0 velocity");
    }

    [TestMethod]
    public void CalculateSwipeVelocity_TimeTooSmall_ReturnsZero()
    {
        // Arrange - time delta below minimum
        double deltaX = 10;
        double deltaY = 100;
        double timeDelta = 0.0005; // 0.5ms (below default 1ms minimum)

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(deltaX, deltaY, timeDelta);

        // Assert
        Assert.AreEqual(0.0, result, "Time delta below minimum should return 0");
    }

    [TestMethod]
    public void CalculateSwipeVelocity_TimeTooLarge_ReturnsZero()
    {
        // Arrange - time delta above maximum
        double deltaX = 10;
        double deltaY = 100;
        double timeDelta = 0.6; // 600ms (above default 500ms maximum)

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(deltaX, deltaY, timeDelta);

        // Assert
        Assert.AreEqual(0.0, result, "Time delta above maximum should return 0");
    }

    [TestMethod]
    public void CalculateSwipeVelocity_FastDownwardSwipe_ReturnsHighVelocity()
    {
        // Arrange - fast downward swipe
        double deltaX = 5;
        double deltaY = 200;
        double timeDelta = 0.1; // 100ms

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(deltaX, deltaY, timeDelta);

        // Assert
        Assert.AreEqual(2000.0, result, 0.01, "Fast swipe should return high velocity");
    }

    [TestMethod]
    public void CalculateSwipeVelocity_SlowVerticalSwipe_ReturnsLowVelocity()
    {
        // Arrange - slow vertical swipe
        double deltaX = 5;
        double deltaY = 50;
        double timeDelta = 0.4; // 400ms

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(deltaX, deltaY, timeDelta);

        // Assert
        Assert.AreEqual(125.0, result, 0.01, "Slow swipe should return low velocity");
    }

    [TestMethod]
    public void CalculateSwipeVelocity_UpwardSwipe_ReturnsNegativeVelocity()
    {
        // Arrange - upward vertical swipe (negative Y)
        double deltaX = 5;
        double deltaY = -100;
        double timeDelta = 0.1;

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(deltaX, deltaY, timeDelta);

        // Assert
        Assert.AreEqual(-1000.0, result, 0.01, "Upward swipe should return negative velocity");
    }

    [TestMethod]
    public void CalculateSwipeVelocity_CustomTimeThresholds_RespectsLimits()
    {
        // Arrange - use custom time thresholds
        double deltaX = 5;
        double deltaY = 100;
        double timeDelta = 0.02; // 20ms
        double minTime = 0.01; // 10ms minimum
        double maxTime = 0.03; // 30ms maximum

        // Act - should work because 20ms is within [10ms, 30ms]
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(
            deltaX, deltaY, timeDelta, minTime, maxTime);

        // Assert
        Assert.AreEqual(5000.0, result, 0.01);
    }

    [TestMethod]
    public void CalculateSwipeVelocity_BelowCustomMinTime_ReturnsZero()
    {
        // Arrange
        double deltaX = 5;
        double deltaY = 100;
        double timeDelta = 0.005; // 5ms
        double minTime = 0.01; // 10ms minimum
        double maxTime = 0.5;

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(
            deltaX, deltaY, timeDelta, minTime, maxTime);

        // Assert
        Assert.AreEqual(0.0, result, "Below custom minimum should return 0");
    }

    [TestMethod]
    public void CalculateSwipeVelocity_AboveCustomMaxTime_ReturnsZero()
    {
        // Arrange
        double deltaX = 5;
        double deltaY = 100;
        double timeDelta = 0.4; // 400ms
        double minTime = 0.001;
        double maxTime = 0.3; // 300ms maximum

        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(
            deltaX, deltaY, timeDelta, minTime, maxTime);

        // Assert
        Assert.AreEqual(0.0, result, "Above custom maximum should return 0");
    }

    [TestMethod]
    [DataRow(0, 100, 0.1, 1000.0)]      // Pure vertical, moderate speed
    [DataRow(10, 100, 0.1, 1000.0)]     // Mostly vertical, moderate speed
    [DataRow(5, 200, 0.1, 2000.0)]      // Mostly vertical, fast
    [DataRow(5, 50, 0.4, 125.0)]        // Mostly vertical, slow
    [DataRow(50, 50, 0.1, 0.0)]         // Diagonal - should be ignored
    [DataRow(100, 30, 0.1, 0.0)]        // Mostly horizontal - should be ignored
    [DataRow(5, -100, 0.1, -1000.0)]    // Upward swipe
    public void CalculateSwipeVelocity_VariousScenarios_ReturnsExpected(
        double deltaX,
        double deltaY,
        double timeDelta,
        double expectedVelocity)
    {
        // Act
        var result = BottomSheetGestureHelper.CalculateSwipeVelocity(deltaX, deltaY, timeDelta);

        // Assert
        Assert.AreEqual(expectedVelocity, result, 0.01,
            $"For deltaX={deltaX}, deltaY={deltaY}, timeDelta={timeDelta}, " +
            $"expected {expectedVelocity} but got {result}");
    }

    #endregion
}
