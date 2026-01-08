using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.ViewModels;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class SwipePreviewAnimationTimingTests
{
    [TestMethod]
    public void GetCompletionDuration_BaseLessThanRequestedMin_DoesNotThrow_AndNeverExceedsBase()
    {
        // Arrange
        const uint baseDuration = 150;

        // Act
        // Typical release ~55% => remaining ~45% would previously try to clamp min=180 > max=150.
        var duration = SwipePreviewAnimationTiming.GetCompletionDuration(
            baseDuration,
            progress: 0.55
        );

        // Assert
        Assert.IsGreaterThan(0u, duration);
        Assert.IsLessThanOrEqualTo(baseDuration, duration);
    }

    [TestMethod]
    public void GetCompletionDuration_ProgressAtOne_ReturnsZero()
    {
        var duration = SwipePreviewAnimationTiming.GetCompletionDuration(150, progress: 1);
        Assert.AreEqual((uint)0, duration);
    }

    [TestMethod]
    public void GetCompletionDuration_MidProgressWithLargerBase_UsesMinimumFinishDuration()
    {
        // Arrange
        const uint baseDuration = 220;

        // Act
        var duration = SwipePreviewAnimationTiming.GetCompletionDuration(
            baseDuration,
            progress: 0.55
        );

        // Assert
        // remaining ~0.45 => requested min is 180ms.
        Assert.IsGreaterThanOrEqualTo(180u, duration);
        Assert.IsLessThanOrEqualTo(baseDuration, duration);
    }
}
