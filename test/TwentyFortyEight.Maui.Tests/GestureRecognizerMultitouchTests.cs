using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 0)]

namespace TwentyFortyEight.Maui.Tests;

/// <summary>
/// Tests for gesture recognizer multitouch handling.
///
/// These tests document the expected behavior when multiple fingers touch the screen
/// during a swipe preview. The actual GestureRecognizerService is tested through
/// integration tests since it depends on MAUI gesture recognizers.
/// </summary>
[TestClass]
public class GestureRecognizerMultitouchTests
{
    /// <summary>
    /// Simulates the multitouch pointer count tracking logic.
    /// This is the core logic that prevents secondary touches from interrupting gestures.
    /// </summary>
    private class PointerCountTracker
    {
        private int _activePointerCount;
        private bool _isGestureActive;

        public int ActivePointerCount => _activePointerCount;
        public bool IsGestureActive => _isGestureActive;

        public bool ShouldStartGesture()
        {
            _activePointerCount++;
            if (_activePointerCount == 1)
            {
                _isGestureActive = true;
                return true;
            }
            return false;
        }

        public bool ShouldProcessMovement()
        {
            return _isGestureActive && _activePointerCount == 1;
        }

        public bool ShouldCompleteGesture()
        {
            if (_activePointerCount > 0)
            {
                _activePointerCount--;
            }

            if (_isGestureActive && _activePointerCount == 0)
            {
                _isGestureActive = false;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _activePointerCount = 0;
            _isGestureActive = false;
        }
    }

    [TestMethod]
    public void SinglePointer_StartsAndCompletesGesture()
    {
        // Arrange
        PointerCountTracker tracker = new();

        // Act - Simulate single pointer press, move, and release
        bool shouldStart = tracker.ShouldStartGesture();
        bool shouldProcess = tracker.ShouldProcessMovement();
        bool shouldComplete = tracker.ShouldCompleteGesture();

        // Assert
        Assert.IsTrue(shouldStart, "Single pointer press should start gesture");
        Assert.IsTrue(shouldProcess, "Single pointer should process movement");
        Assert.IsTrue(shouldComplete, "Single pointer release should complete gesture");
        Assert.AreEqual(0, tracker.ActivePointerCount, "Pointer count should be 0 after release");
        Assert.IsFalse(tracker.IsGestureActive, "Gesture should not be active after completion");
    }

    [TestMethod]
    public void SecondPointer_PressDoesNotStartNewGesture()
    {
        // Arrange
        PointerCountTracker tracker = new();

        // Act - Simulate first pointer press, then second pointer press
        bool firstShouldStart = tracker.ShouldStartGesture();
        bool secondShouldStart = tracker.ShouldStartGesture();

        // Assert
        Assert.IsTrue(firstShouldStart, "First pointer press should start gesture");
        Assert.IsFalse(secondShouldStart, "Second pointer press should not start new gesture");
        Assert.AreEqual(2, tracker.ActivePointerCount, "Should track 2 active pointers");
        Assert.IsTrue(tracker.IsGestureActive, "Gesture should remain active");
    }

    [TestMethod]
    public void SecondPointer_ReleaseDoesNotCompleteGesture()
    {
        // Arrange
        PointerCountTracker tracker = new();

        // Act - Simulate first pointer press, second pointer press and release
        tracker.ShouldStartGesture(); // First pointer
        tracker.ShouldStartGesture(); // Second pointer
        bool secondShouldComplete = tracker.ShouldCompleteGesture(); // Second pointer release

        // Assert
        Assert.IsFalse(secondShouldComplete, "Second pointer release should not complete gesture");
        Assert.AreEqual(1, tracker.ActivePointerCount, "Should have 1 pointer remaining");
        Assert.IsTrue(tracker.IsGestureActive, "Gesture should still be active");
    }

    [TestMethod]
    public void SecondPointer_MovementIsIgnored()
    {
        // Arrange
        PointerCountTracker tracker = new();

        // Act - Simulate first pointer press, second pointer press, then check movement
        tracker.ShouldStartGesture(); // First pointer
        tracker.ShouldStartGesture(); // Second pointer
        bool shouldProcessWhileTwoPointers = tracker.ShouldProcessMovement();

        // Assert
        Assert.IsFalse(
            shouldProcessWhileTwoPointers,
            "Movement should be ignored when more than one pointer is active"
        );
    }

    [TestMethod]
    public void MultiplePointers_GestureCompletesOnlyWhenAllReleased()
    {
        // Arrange
        PointerCountTracker tracker = new();

        // Act - Simulate three pointers press and release
        tracker.ShouldStartGesture(); // First pointer
        tracker.ShouldStartGesture(); // Second pointer
        tracker.ShouldStartGesture(); // Third pointer

        bool secondRelease = tracker.ShouldCompleteGesture();
        bool thirdRelease = tracker.ShouldCompleteGesture();
        bool firstRelease = tracker.ShouldCompleteGesture();

        // Assert
        Assert.IsFalse(secondRelease, "Gesture should not complete on second pointer release");
        Assert.IsFalse(thirdRelease, "Gesture should not complete on third pointer release");
        Assert.IsTrue(firstRelease, "Gesture should complete when last pointer is released");
        Assert.AreEqual(0, tracker.ActivePointerCount, "All pointers should be released");
        Assert.IsFalse(tracker.IsGestureActive, "Gesture should not be active");
    }

    [TestMethod]
    public void MultiplePointers_FirstPointerMovementContinues()
    {
        // Arrange
        PointerCountTracker tracker = new();

        // Act - Simulate first pointer press, second pointer press & release, then movement
        tracker.ShouldStartGesture(); // First pointer
        tracker.ShouldStartGesture(); // Second pointer
        tracker.ShouldCompleteGesture(); // Second pointer release
        bool shouldProcessAfterSecondRelease = tracker.ShouldProcessMovement();

        // Assert
        Assert.IsTrue(
            shouldProcessAfterSecondRelease,
            "First pointer should continue to process movement after second pointer is released"
        );
        Assert.AreEqual(1, tracker.ActivePointerCount, "Should have 1 pointer remaining");
    }

    [TestMethod]
    public void SequentialGestures_WorkIndependently()
    {
        // Arrange
        PointerCountTracker tracker = new();

        // Act - First gesture
        tracker.ShouldStartGesture();
        tracker.ShouldCompleteGesture();

        // Second gesture
        bool secondStart = tracker.ShouldStartGesture();
        bool secondComplete = tracker.ShouldCompleteGesture();

        // Assert
        Assert.IsTrue(secondStart, "Second gesture should start normally");
        Assert.IsTrue(secondComplete, "Second gesture should complete normally");
        Assert.AreEqual(0, tracker.ActivePointerCount, "Pointer count should reset");
    }
}
