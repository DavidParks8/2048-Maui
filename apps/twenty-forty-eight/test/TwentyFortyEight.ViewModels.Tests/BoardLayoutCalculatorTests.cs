using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class BoardLayoutCalculatorTests
{
    #region CalculateBoardSize Tests

    [TestMethod]
    public void CalculateBoardSize_SmallScreen_ReturnsMinSize()
    {
        // Arrange - screen smaller than min board size after subtracting reserved space
        double pageWidth = 300;
        double pageHeight = 400;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(pageWidth, pageHeight);

        // Assert - should clamp to minimum
        Assert.AreEqual(BoardLayoutCalculator.MinBoardSize, result);
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
        // Arrange - medium screen where calculated size is between min and max
        double pageWidth = 500;
        double pageHeight = 700;
        double horizontalReserved = 50;
        double verticalReserved = 260;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(pageWidth, pageHeight);

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

        // Assert - width is constraining factor (400 - 50 = 350)
        Assert.AreEqual(350, result);
    }

    [TestMethod]
    public void CalculateBoardSize_HeightConstrained_UsesHeight()
    {
        // Arrange - wide but short screen
        double pageWidth = 1000;
        double pageHeight = 500;

        // Act
        double result = BoardLayoutCalculator.CalculateBoardSize(pageWidth, pageHeight);

        // Assert - height is constraining (500 - 260 = 240, but min is 280)
        Assert.AreEqual(BoardLayoutCalculator.MinBoardSize, result);
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
