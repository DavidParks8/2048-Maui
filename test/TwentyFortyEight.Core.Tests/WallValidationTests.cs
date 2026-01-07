using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class WallValidationTests
{
    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            Assert.Fail($"Expected {typeof(TException).Name} to be thrown.");
        }
        catch (TException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void WallSegment_InvalidOrientation_Throws()
    {
        AssertThrows<ArgumentOutOfRangeException>(
            () => new WallSegment((WallOrientation)123, divider: 0, start: 0, length: 1)
        );
    }

    [TestMethod]
    public void WallSegment_NegativeDivider_Throws()
    {
        AssertThrows<ArgumentOutOfRangeException>(
            () => new WallSegment(WallOrientation.Vertical, divider: -1, start: 0, length: 1)
        );
    }

    [TestMethod]
    public void WallSegment_NegativeStart_Throws()
    {
        AssertThrows<ArgumentOutOfRangeException>(
            () => new WallSegment(WallOrientation.Vertical, divider: 0, start: -1, length: 1)
        );
    }

    [TestMethod]
    public void WallSegment_NonPositiveLength_Throws()
    {
        AssertThrows<ArgumentOutOfRangeException>(
            () => new WallSegment(WallOrientation.Vertical, divider: 0, start: 0, length: 0)
        );
    }

    [TestMethod]
    public void WallSegment_IsValidForSize_ValidWall_ReturnsTrue()
    {
        var wall = new WallSegment(WallOrientation.Vertical, divider: 1, start: 0, length: 2);

        Assert.IsTrue(wall.IsValidForSize(4));
    }

    [TestMethod]
    public void WallSegment_IsValidForSize_InvalidDivider_ReturnsFalse()
    {
        var wall = new WallSegment(WallOrientation.Vertical, divider: 3, start: 0, length: 1);

        Assert.IsFalse(wall.IsValidForSize(4));
    }

    [TestMethod]
    public void GameState_WithWall_InvalidForSize_Throws()
    {
        GameState state = new(4);
        var invalidWall = new WallSegment(
            WallOrientation.Horizontal,
            divider: 3,
            start: 0,
            length: 1
        );

        AssertThrows<ArgumentOutOfRangeException>(() => state.WithWall(invalidWall));
    }

    [TestMethod]
    public void GameStateDto_ToGameState_DropsInvalidWall()
    {
        var invalidWall = new WallSegment(
            WallOrientation.Vertical,
            divider: 3,
            start: 0,
            length: 1
        );

        GameStateDto dto = new()
        {
            Size = 4,
            Board = new int[16],
            Wall = invalidWall,
        };

        var state = dto.ToGameState();

        Assert.IsNull(state.Wall);
    }
}
