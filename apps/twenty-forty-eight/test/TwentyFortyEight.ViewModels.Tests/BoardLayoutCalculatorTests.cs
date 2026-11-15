using Microsoft.Maui.Graphics;
using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class BoardLayoutCalculatorTests
{
    #region CalculateBoardSize Tests

    [TestMethod]
    public void CalculateBoardSize_SmallViewport_FitsAvailableSpace()
    {
        // A fold or landscape window may leave less space than a typical phone.
        double pageWidth = 300;
        double pageHeight = 400;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(pageWidth, pageHeight);

        Assert.AreEqual(300, result);
    }

    [TestMethod]
    public void CalculateBoardSize_LargeScreen_ReturnsMaxSize()
    {
        // Arrange - very large screen
        double pageWidth = 2000;
        double pageHeight = 2000;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(pageWidth, pageHeight);

        // Assert - should clamp to maximum
        Assert.AreEqual(BoardLayoutCalculator.MaxBoardSize, result);
    }

    [TestMethod]
    public void CalculateBoardSize_MediumScreen_ReturnsCalculatedSize()
    {
        // Arrange
        double pageWidth = 500;
        double pageHeight = 700;
        double horizontalReserved = 50;
        double verticalReserved = 260;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(
            pageWidth,
            pageHeight,
            horizontalReserved,
            verticalReserved
        );

        // Assert - should be the smaller of available dimensions
        double expectedWidth = pageWidth - horizontalReserved; // 450
        double expectedHeight = pageHeight - verticalReserved; // 440
        double expected = Math.Min(expectedWidth, expectedHeight); // 440
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void CalculateBoardSize_WidthConstrained_UsesWidth()
    {
        // Arrange - narrow but tall screen
        double pageWidth = 400;
        double pageHeight = 1000;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(pageWidth, pageHeight);

        Assert.AreEqual(400, result);
    }

    [TestMethod]
    public void CalculateBoardSize_HeightConstrained_UsesHeight()
    {
        // Arrange - wide but short screen
        double pageWidth = 1000;
        double pageHeight = 500;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(pageWidth, pageHeight);

        Assert.AreEqual(500, result);
    }

    [TestMethod]
    public void CalculateBoardSize_CustomReservedSpace_UsesProvidedValues()
    {
        // Arrange
        double pageWidth = 600;
        double pageHeight = 800;
        double customHorizontal = 100;
        double customVertical = 300;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(
            pageWidth,
            pageHeight,
            customHorizontal,
            customVertical
        );

        // Assert - should use custom reserved values
        double expectedWidth = pageWidth - customHorizontal; // 500
        double expectedHeight = pageHeight - customVertical; // 500
        Assert.AreEqual(500, result);
    }

    #endregion

    [TestMethod]
    [DataRow(240, 180, 160)]
    [DataRow(800, 240, 220)]
    [DataRow(320, 600, 300)]
    [DataRow(800, 700, 680)]
    [DataRow(1200, 1100, 800)]
    [DataRow(10, 10, 0)]
    public void CalculateBoardSize_WithBorderPadding_NeverExceedsViewport(
        int width,
        int height,
        int expected
    )
    {
        double size = BoardLayoutCalculator.CalculateBoardSize(width, height, 20, 20);

        Assert.AreEqual(expected, size);
        if (size > 0)
        {
            Assert.IsLessThanOrEqualTo(width, size + 20);
            Assert.IsLessThanOrEqualTo(height, size + 20);
        }
    }

    private static readonly Size Mode = new(130, 56);
    private static readonly Size Card = new(96, 56);
    private static readonly Size Undo = new(48, 48);

    [TestMethod]
    public void Arrange_ClosedPhone_StacksStatusBoardAndActions()
    {
        Rect bounds = new(16, 16, 350, 600);

        var layout = BoardLayoutCalculator.Arrange(bounds, null, Mode, Card, Undo);

        Assert.AreEqual(GameLayoutKind.Stacked, layout.Kind);
        Assert.AreEqual(350, layout.Board.Width);
        Assert.IsLessThanOrEqualTo(layout.Board.Top, layout.Scores.Bottom);
        Assert.IsGreaterThanOrEqualTo(layout.Board.Bottom, layout.Actions.Top);
        Assert.AreEqual(layout.Board.Left, layout.Mode.Left);
        Assert.AreEqual(layout.Board.Right, layout.Scores.Right);
        AssertComposed(bounds, layout);
    }

    [TestMethod]
    public void Arrange_OpenDuo_FlanksCenteredBoardWithSymmetricWings()
    {
        Rect bounds = new(16, 98, 835, 521);

        var layout = BoardLayoutCalculator.Arrange(bounds, null, Mode, Card, Undo);

        Assert.AreEqual(GameLayoutKind.Wings, layout.Kind);
        Assert.AreEqual(521, layout.Board.Width);
        Assert.AreEqual(bounds.Center, layout.Board.Center);
        Assert.AreEqual(
            layout.Board.Left - layout.Scores.Left,
            layout.Mode.Right - layout.Board.Right
        );
        Assert.IsTrue(layout.ScoresVertical);
        Assert.AreEqual(layout.Board.Bottom, layout.Actions.Bottom);
        AssertComposed(bounds, layout);
    }

    [TestMethod]
    public void Arrange_OpenDuo_UsesMoreBoardThanStackedLayout()
    {
        Rect bounds = new(16, 98, 835, 521);
        var wings = BoardLayoutCalculator.Arrange(bounds, null, Mode, Card, Undo);
        var stackedOnly = BoardLayoutCalculator.Arrange(bounds, null, new(400, 56), Card, Undo);

        Assert.AreEqual(GameLayoutKind.Stacked, stackedOnly.Kind);
        Assert.IsGreaterThan(stackedOnly.Board.Width * 1.25, wings.Board.Width);
    }

    [TestMethod]
    [DataRow(350, 0, 24, 600, GameLayoutKind.SplitVertical)]
    [DataRow(0, 280, 800, 24, GameLayoutKind.SplitHorizontal)]
    public void Arrange_ActiveFold_PutsBoardAndConsoleOnOppositeSides(
        int x,
        int y,
        int width,
        int height,
        GameLayoutKind expected
    )
    {
        Rect bounds = new(0, 0, 800, 600);
        Rect fold = new(x, y, width, height);

        var layout = BoardLayoutCalculator.Arrange(bounds, fold, Mode, Card, Undo);

        Assert.AreEqual(expected, layout.Kind);
        Assert.IsTrue(layout.BoardPane!.Value.Contains(layout.Board));
        foreach (var group in new[] { layout.Mode, layout.Scores, layout.Actions })
        {
            Assert.IsTrue(layout.ConsolePane!.Value.Contains(group));
            Assert.IsTrue(group.Intersect(fold).IsEmpty);
        }
        Assert.IsTrue(layout.Board.Intersect(fold).IsEmpty);
        AssertComposed(bounds, layout);
    }

    [TestMethod]
    [DataRow(700, 0, 24, 600)]
    [DataRow(0, 560, 800, 24)]
    public void Arrange_FoldWithoutConsoleRoom_FallsBackToSinglePane(
        int x,
        int y,
        int width,
        int height
    )
    {
        var layout = BoardLayoutCalculator.Arrange(
            new Rect(0, 0, 800, 600),
            new Rect(x, y, width, height),
            Mode,
            Card,
            Undo
        );

        Assert.IsNull(layout.ConsolePane);
    }

    [TestMethod]
    public void Arrange_NearCrossover_KeepsPreviousLayout()
    {
        // Wings and stacked yield nearly the same board here.
        Rect bounds = new(0, 0, 640, 480);
        var fresh = BoardLayoutCalculator.Arrange(bounds, null, Mode, Card, Undo);
        var kept = BoardLayoutCalculator.Arrange(
            bounds,
            null,
            Mode,
            Card,
            Undo,
            GameLayoutKind.Wings
        );

        Assert.AreEqual(GameLayoutKind.Stacked, fresh.Kind);
        Assert.AreEqual(GameLayoutKind.Wings, kept.Kind);
    }

    [TestMethod]
    [DataRow(350, 600, 220, 120, 84)]
    [DataRow(835, 521, 260, 150, 84)]
    [DataRow(320, 480, 300, 140, 0)]
    [DataRow(1000, 700, 140, 96, 216)]
    public void Arrange_LargeTextAndVoiceControls_NeverOverlap(
        int width,
        int height,
        int modeWidth,
        int cardWidth,
        int actionsWidth
    )
    {
        Rect bounds = new(0, 0, width, height);
        Size actions = actionsWidth > 0 ? new(actionsWidth, 54) : Size.Zero;

        var layout = BoardLayoutCalculator.Arrange(
            bounds,
            null,
            new Size(modeWidth, 72),
            new Size(cardWidth, 72),
            actions
        );

        AssertComposed(bounds, layout);
    }

    private static void AssertComposed(Rect bounds, GameLayout layout)
    {
        var groups = new List<Rect> { layout.Board, layout.Mode, layout.Scores };
        if (layout.Actions.Height > 0)
        {
            groups.Add(layout.Actions);
        }

        foreach (var group in groups)
        {
            Assert.IsTrue(bounds.Inflate(0.5, 0.5).Contains(group), $"{group} is outside {bounds}");
        }

        Assert.AreEqual(layout.Board.Width, layout.Board.Height);
        for (int i = 0; i < groups.Count; i++)
        {
            for (int j = i + 1; j < groups.Count; j++)
            {
                var overlap = groups[i].Intersect(groups[j]);
                Assert.IsTrue(
                    overlap.Width <= 0.01 || overlap.Height <= 0.01,
                    $"{groups[i]} overlaps {groups[j]}"
                );
            }
        }
    }

    [TestMethod]
    [DataRow(0, 300, 800, 24, 0, 0, 800, 300)]
    [DataRow(0, 280, 800, 24, 0, 304, 800, 296)]
    [DataRow(300, 0, 24, 600, 324, 0, 476, 600)]
    [DataRow(0, 0, 40, 40, 40, 0, 760, 600)]
    [DataRow(900, 0, 40, 600, 0, 0, 800, 600)]
    public void ExcludeReservedRegion_ReturnsLargestClearArea(
        int x,
        int y,
        int width,
        int height,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight
    )
    {
        Rect bounds = new(0, 0, 800, 600);
        Rect region = new(x, y, width, height);

        var available = BoardLayoutCalculator.ExcludeReservedRegion(bounds, region);

        Assert.AreEqual(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), available);
        Assert.IsTrue(bounds.Contains(available));
        Assert.IsTrue(available.Intersect(region).IsEmpty);
    }

    [TestMethod]
    public void ExcludeReservedRegion_UnfoldingRestoresFullArea()
    {
        Rect bounds = new(0, 0, 800, 600);
        Rect fold = new(300, 0, 24, 600);
        var folded = BoardLayoutCalculator.ExcludeReservedRegion(bounds, fold);
        var unfolded = BoardLayoutCalculator.ExcludeReservedRegion(bounds, Rect.Zero);

        Assert.IsLessThan(bounds.Width, folded.Width);
        Assert.AreEqual(bounds, unfolded);
    }

    #region CalculateScaleFactor Tests

    [TestMethod]
    public void CalculateScaleFactor_DefaultBoard4x4At400px_ReturnsOne()
    {
        // Arrange - default reference values
        double boardSize = 400;
        int gridSize = 4;

        // Act
        double result = BoardLayoutCalculator.CalculateScaleFactor(boardSize, gridSize);

        // Assert
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void CalculateScaleFactor_SmallerBoard_ReturnsLessThanOne()
    {
        // Arrange - half the default size
        double boardSize = 200;
        int gridSize = 4;

        // Act
        double result = BoardLayoutCalculator.CalculateScaleFactor(boardSize, gridSize);

        // Assert
        Assert.AreEqual(0.5, result);
    }

    [TestMethod]
    public void CalculateScaleFactor_LargerBoard_ReturnsGreaterThanOne()
    {
        // Arrange - double the default size
        double boardSize = 800;
        int gridSize = 4;

        // Act
        double result = BoardLayoutCalculator.CalculateScaleFactor(boardSize, gridSize);

        // Assert
        Assert.AreEqual(2.0, result);
    }

    [TestMethod]
    public void CalculateScaleFactor_LargerGrid_ScalesDown()
    {
        // Arrange - 8x8 grid at default size (more tiles = smaller tiles)
        double boardSize = 400;
        int gridSize = 8;

        // Act
        double result = BoardLayoutCalculator.CalculateScaleFactor(boardSize, gridSize);

        // Assert - should be half (4/8)
        Assert.AreEqual(0.5, result);
    }

    [TestMethod]
    public void CalculateScaleFactor_SmallerGrid_ScalesUp()
    {
        // Arrange - 3x3 grid at default size (fewer tiles = larger tiles)
        double boardSize = 400;
        int gridSize = 3;

        // Act
        double result = BoardLayoutCalculator.CalculateScaleFactor(boardSize, gridSize);

        // Assert - should be 4/3 ≈ 1.333
        Assert.AreEqual(4.0 / 3.0, result, 0.001);
    }

    [TestMethod]
    public void CalculateScaleFactor_ZeroGridSize_ReturnsOne()
    {
        // Arrange - edge case
        double boardSize = 400;
        int gridSize = 0;

        // Act
        double result = BoardLayoutCalculator.CalculateScaleFactor(boardSize, gridSize);

        // Assert - should handle gracefully
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void CalculateScaleFactor_NegativeGridSize_ReturnsOne()
    {
        // Arrange - edge case
        double boardSize = 400;
        int gridSize = -1;

        // Act
        double result = BoardLayoutCalculator.CalculateScaleFactor(boardSize, gridSize);

        // Assert - should handle gracefully
        Assert.AreEqual(1.0, result);
    }

    #endregion

    #region CalculateTileSpacing Tests

    [TestMethod]
    public void CalculateTileSpacing_DefaultBoard_ReturnsExpectedSpacing()
    {
        // Arrange
        double boardSize = 400;

        // Act
        double result = BoardLayoutCalculator.CalculateTileSpacing(boardSize);

        // Assert - 400 / 40 = 10
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public void CalculateTileSpacing_SmallBoard_ReturnsMinSpacing()
    {
        // Arrange - very small board
        double boardSize = 100;

        // Act
        double result = BoardLayoutCalculator.CalculateTileSpacing(boardSize);

        // Assert - 100 / 40 = 2.5, but min is 5
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void CalculateTileSpacing_LargeBoard_ReturnsProportionalSpacing()
    {
        // Arrange
        double boardSize = 800;

        // Act
        double result = BoardLayoutCalculator.CalculateTileSpacing(boardSize);

        // Assert - 800 / 40 = 20
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void CalculateTileSpacing_CustomMinSpacing_UsesProvidedValue()
    {
        // Arrange
        double boardSize = 100;
        double customMin = 3;

        // Act
        double result = BoardLayoutCalculator.CalculateTileSpacing(boardSize, customMin);

        // Assert - 100 / 40 = 2.5, custom min is 3
        Assert.AreEqual(3, result);
    }

    #endregion
}
