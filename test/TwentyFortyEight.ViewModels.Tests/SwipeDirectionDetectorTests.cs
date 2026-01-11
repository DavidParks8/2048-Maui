using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class SwipeDirectionDetectorTests
{
    #region GetDirection Tests

    [TestMethod]
    [DataRow(50, 0, Direction.Right)]
    [DataRow(-50, 0, Direction.Left)]
    [DataRow(0, 50, Direction.Down)]
    [DataRow(0, -50, Direction.Up)]
    public void GetDirection_ClearDirection_ReturnsCorrectDirection(
        double deltaX,
        double deltaY,
        Direction expected
    )
    {
        // Act
        var result = SwipeDirectionDetector.GetDirection(deltaX, deltaY, threshold: 30);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetDirection_BelowThreshold_ReturnsNull()
    {
        // Arrange - movement below threshold
        double deltaX = 10;
        double deltaY = 5;
        double threshold = 30;

        // Act
        var result = SwipeDirectionDetector.GetDirection(deltaX, deltaY, threshold);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetDirection_DiagonalMovement_ReturnsDominantDirection()
    {
        // Arrange - diagonal but more horizontal
        double deltaX = 50;
        double deltaY = 30;

        // Act
        var result = SwipeDirectionDetector.GetDirection(deltaX, deltaY, threshold: 20);

        // Assert - horizontal is dominant
        Assert.AreEqual(Direction.Right, result);
    }

    [TestMethod]
    public void GetDirection_DiagonalMovementVerticalDominant_ReturnsVertical()
    {
        // Arrange - diagonal but more vertical
        double deltaX = 30;
        double deltaY = 50;

        // Act
        var result = SwipeDirectionDetector.GetDirection(deltaX, deltaY, threshold: 20);

        // Assert - vertical is dominant
        Assert.AreEqual(Direction.Down, result);
    }

    [TestMethod]
    public void GetDirection_ExactlyAtThreshold_ReturnsNull()
    {
        // Arrange - exactly at threshold (not over)
        double deltaX = 30;
        double deltaY = 0;
        double threshold = 30;

        // Act
        var result = SwipeDirectionDetector.GetDirection(deltaX, deltaY, threshold);

        // Assert - must exceed threshold, not equal
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetDirection_JustOverThreshold_ReturnsDirection()
    {
        // Arrange
        double deltaX = 30.1;
        double deltaY = 0;
        double threshold = 30;

        // Act
        var result = SwipeDirectionDetector.GetDirection(deltaX, deltaY, threshold);

        // Assert
        Assert.AreEqual(Direction.Right, result);
    }

    #endregion

    #region GetPreviewDirection Tests

    [TestMethod]
    public void GetPreviewDirection_SmallMovement_ReturnsDirection()
    {
        // Arrange - small movement but over preview threshold (8px)
        double deltaX = 15;
        double deltaY = 0;

        // Act
        var result = SwipeDirectionDetector.GetPreviewDirection(deltaX, deltaY);

        // Assert
        Assert.AreEqual(Direction.Right, result);
    }

    [TestMethod]
    public void GetPreviewDirection_VerySmallMovement_ReturnsNull()
    {
        // Arrange - too small even for preview
        double deltaX = 5;
        double deltaY = 3;

        // Act
        var result = SwipeDirectionDetector.GetPreviewDirection(deltaX, deltaY);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region GetSwipeDirection Tests

    [TestMethod]
    public void GetSwipeDirection_LargeMovement_ReturnsDirection()
    {
        // Arrange - clear swipe over threshold (30px)
        double deltaX = 0;
        double deltaY = -100;

        // Act
        var result = SwipeDirectionDetector.GetSwipeDirection(deltaX, deltaY);

        // Assert
        Assert.AreEqual(Direction.Up, result);
    }

    [TestMethod]
    public void GetSwipeDirection_BelowSwipeThreshold_ReturnsNull()
    {
        // Arrange - movement between preview and swipe threshold
        double deltaX = 20;
        double deltaY = 0;

        // Act
        var result = SwipeDirectionDetector.GetSwipeDirection(deltaX, deltaY);

        // Assert - 20 > 8 (preview) but 20 < 30 (swipe)
        Assert.IsNull(result);
    }

    #endregion

    #region CalculateSpeed Tests

    [TestMethod]
    public void CalculateSpeed_SimpleHorizontalMovement_ReturnsCorrectSpeed()
    {
        // Arrange - 100px in 100ms = 1 px/ms
        double deltaX = 100;
        double deltaY = 0;
        double elapsedMs = 100;

        // Act
        double result = SwipeDirectionDetector.CalculateSpeed(deltaX, deltaY, elapsedMs);

        // Assert
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void CalculateSpeed_DiagonalMovement_UsesEuclideanDistance()
    {
        // Arrange - 30-40-50 right triangle
        double deltaX = 30;
        double deltaY = 40;
        double elapsedMs = 100;

        // Act
        double result = SwipeDirectionDetector.CalculateSpeed(deltaX, deltaY, elapsedMs);

        // Assert - sqrt(30^2 + 40^2) = 50, 50/100 = 0.5
        Assert.AreEqual(0.5, result);
    }

    [TestMethod]
    public void CalculateSpeed_ZeroTime_HandlesGracefully()
    {
        // Arrange - edge case with zero elapsed time
        double deltaX = 100;
        double deltaY = 0;
        double elapsedMs = 0;

        // Act
        double result = SwipeDirectionDetector.CalculateSpeed(deltaX, deltaY, elapsedMs);

        // Assert - should use minimum of 1ms
        Assert.AreEqual(100.0, result);
    }

    [TestMethod]
    public void CalculateSpeed_NegativeTime_HandlesGracefully()
    {
        // Arrange - edge case with negative elapsed time
        double deltaX = 100;
        double deltaY = 0;
        double elapsedMs = -50;

        // Act
        double result = SwipeDirectionDetector.CalculateSpeed(deltaX, deltaY, elapsedMs);

        // Assert - should use minimum of 1ms
        Assert.AreEqual(100.0, result);
    }

    #endregion

    #region IsFastSwipe Tests

    [TestMethod]
    public void IsFastSwipe_AboveThreshold_ReturnsTrue()
    {
        // Arrange - 100px in 50ms = 2 px/ms (above 0.8 threshold)
        double deltaX = 100;
        double deltaY = 0;
        double elapsedMs = 50;

        // Act
        bool result = SwipeDirectionDetector.IsFastSwipe(deltaX, deltaY, elapsedMs);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsFastSwipe_BelowThreshold_ReturnsFalse()
    {
        // Arrange - 50px in 200ms = 0.25 px/ms (below 0.8 threshold)
        double deltaX = 50;
        double deltaY = 0;
        double elapsedMs = 200;

        // Act
        bool result = SwipeDirectionDetector.IsFastSwipe(deltaX, deltaY, elapsedMs);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsFastSwipe_ExactlyAtThreshold_ReturnsTrue()
    {
        // Arrange - exactly at 0.8 px/ms
        double deltaX = 80;
        double deltaY = 0;
        double elapsedMs = 100;

        // Act
        bool result = SwipeDirectionDetector.IsFastSwipe(deltaX, deltaY, elapsedMs);

        // Assert - >= threshold returns true
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsFastSwipe_CustomThreshold_UsesProvidedValue()
    {
        // Arrange - 50px in 100ms = 0.5 px/ms
        double deltaX = 50;
        double deltaY = 0;
        double elapsedMs = 100;
        double customThreshold = 0.4;

        // Act
        bool result = SwipeDirectionDetector.IsFastSwipe(
            deltaX,
            deltaY,
            elapsedMs,
            customThreshold
        );

        // Assert - 0.5 >= 0.4
        Assert.IsTrue(result);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void GetDirection_ZeroMovement_ReturnsNull()
    {
        // Arrange
        double deltaX = 0;
        double deltaY = 0;

        // Act
        var result = SwipeDirectionDetector.GetDirection(deltaX, deltaY, threshold: 10);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetDirection_EqualMovementBothAxes_ReturnsVertical()
    {
        // Arrange - when equal, vertical is chosen (else branch)
        double deltaX = 50;
        double deltaY = 50;

        // Act
        var result = SwipeDirectionDetector.GetDirection(deltaX, deltaY, threshold: 30);

        // Assert - when abs(x) == abs(y), the else branch (vertical) is taken
        Assert.AreEqual(Direction.Down, result);
    }

    #endregion
}
