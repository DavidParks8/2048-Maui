using TwentyFortyEight.ViewModels.Models;

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for TileViewModel.
/// </summary>
[TestClass]
public class TileViewModelTests
{
    #region DisplayValue Tests

    [TestMethod]
    public void DisplayValue_WhenZero_ReturnsEmptyString()
    {
        // Arrange
        TileViewModel tile = new() { Value = 0 };

        // Assert
        Assert.AreEqual("", tile.DisplayValue);
    }

    [TestMethod]
    public void DisplayValue_WhenNonZero_ReturnsValueAsString()
    {
        // Arrange
        TileViewModel tile = new() { Value = 2048 };

        // Assert
        Assert.AreEqual("2048", tile.DisplayValue);
    }

    [TestMethod]
    public void Value_WhenChanged_NotifiesDisplayValueChanged()
    {
        // Arrange
        TileViewModel tile = new() { Value = 2 };
        var propertyChangedRaised = false;
        tile.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TileViewModel.DisplayValue))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        tile.Value = 4;

        // Assert
        Assert.IsTrue(propertyChangedRaised);
    }

    [TestMethod]
    public void IsNewTile_DefaultsFalse()
    {
        // Arrange & Act
        TileViewModel tile = new();

        // Assert
        Assert.IsFalse(tile.IsNewTile);
    }

    [TestMethod]
    public void IsMerged_DefaultsFalse()
    {
        // Arrange & Act
        TileViewModel tile = new();

        // Assert
        Assert.IsFalse(tile.IsMerged);
    }

    [TestMethod]
    public void Row_CanBeSetAndGet()
    {
        // Arrange
        TileViewModel tile = new() { Row = 2 };

        // Assert
        Assert.AreEqual(2, tile.Row);
    }

    [TestMethod]
    public void Column_CanBeSetAndGet()
    {
        // Arrange
        TileViewModel tile = new() { Column = 3 };

        // Assert
        Assert.AreEqual(3, tile.Column);
    }

    #endregion

    #region FontSize Caching Tests

    [TestMethod]
    public void GetTileFontSize_ZeroValue_Returns32()
    {
        // Act
        double fontSize = TileViewModel.GetTileFontSize(0);

        // Assert
        Assert.AreEqual(32, fontSize);
    }

    [TestMethod]
    [DataRow(2, 32)]
    [DataRow(4, 32)]
    [DataRow(8, 32)]
    [DataRow(16, 32)]
    [DataRow(32, 32)]
    [DataRow(64, 32)]
    public void GetTileFontSize_TwoDigitValues_Returns32(int value, double expected)
    {
        // Act
        double fontSize = TileViewModel.GetTileFontSize(value);

        // Assert
        Assert.AreEqual(expected, fontSize, $"Font size for {value} should be {expected}");
    }

    [TestMethod]
    [DataRow(128, 28)]
    [DataRow(256, 28)]
    [DataRow(512, 28)]
    public void GetTileFontSize_ThreeDigitValues_Returns28(int value, double expected)
    {
        // Act
        double fontSize = TileViewModel.GetTileFontSize(value);

        // Assert
        Assert.AreEqual(expected, fontSize, $"Font size for {value} should be {expected}");
    }

    [TestMethod]
    [DataRow(1024, 24)]
    [DataRow(2048, 24)]
    [DataRow(4096, 24)]
    [DataRow(8192, 24)]
    public void GetTileFontSize_FourDigitValues_Returns24(int value, double expected)
    {
        // Act
        double fontSize = TileViewModel.GetTileFontSize(value);

        // Assert
        Assert.AreEqual(expected, fontSize, $"Font size for {value} should be {expected}");
    }

    [TestMethod]
    [DataRow(16384, 20)]
    [DataRow(32768, 20)]
    [DataRow(65536, 20)]
    public void GetTileFontSize_FiveDigitValues_Returns20(int value, double expected)
    {
        // Act
        double fontSize = TileViewModel.GetTileFontSize(value);

        // Assert
        Assert.AreEqual(expected, fontSize, $"Font size for {value} should be {expected}");
    }

    [TestMethod]
    [DataRow(131072, 16)]
    [DataRow(262144, 16)]
    [DataRow(524288, 16)]
    public void GetTileFontSize_SixDigitValues_Returns16(int value, double expected)
    {
        // Act
        double fontSize = TileViewModel.GetTileFontSize(value);

        // Assert
        Assert.AreEqual(expected, fontSize, $"Font size for {value} should be {expected}");
    }

    [TestMethod]
    public void GetTileFontSize_SevenDigitValue_ReturnsSmallerThan16()
    {
        // 1048576 = 2^20 = 7 digits
        // Act
        double fontSize = TileViewModel.GetTileFontSize(1048576);

        // Assert - for 7 digits: 96/7 ≈ 13.7
        // IsLessThan(upperBound, value) - value must be less than upperBound
        Assert.IsLessThan(
            16.0,
            fontSize,
            $"Font size for 1048576 should be less than 16, got {fontSize}"
        );
        Assert.IsGreaterThan(
            10.0,
            fontSize,
            $"Font size for 1048576 should be greater than 10, got {fontSize}"
        );
    }

    [TestMethod]
    public void GetTileFontSize_AllPowersOfTwo_ReturnsCachedValues()
    {
        // This test verifies that all common powers of 2 return consistent values
        // from the cache (no exceptions, stable results)
        var expectedValues = new Dictionary<int, double>
        {
            [2] = 32,
            [4] = 32,
            [8] = 32,
            [16] = 32,
            [32] = 32,
            [64] = 32,
            [128] = 28,
            [256] = 28,
            [512] = 28,
            [1024] = 24,
            [2048] = 24,
            [4096] = 24,
            [8192] = 24,
            [16384] = 20,
            [32768] = 20,
            [65536] = 20,
            [131072] = 16,
            [262144] = 16,
            [524288] = 16,
        };

        foreach (var (value, expected) in expectedValues)
        {
            double fontSize = TileViewModel.GetTileFontSize(value);
            Assert.AreEqual(expected, fontSize, $"Font size for {value} should be {expected}");
        }
    }

    [TestMethod]
    public void GetTileFontSize_ConsecutiveCalls_ReturnSameValue()
    {
        // Verify caching returns consistent results
        double first = TileViewModel.GetTileFontSize(2048);
        double second = TileViewModel.GetTileFontSize(2048);
        double third = TileViewModel.GetTileFontSize(2048);

        Assert.AreEqual(first, second);
        Assert.AreEqual(second, third);
    }

    [TestMethod]
    public void GetTileFontSize_NonPowerOfTwo_FallsBackToCalculation()
    {
        // Non-powers of 2 aren't cached but should still work via calculation fallback
        // 100 = 3 digits -> should return 28
        double fontSize = TileViewModel.GetTileFontSize(100);
        Assert.AreEqual(28, fontSize, "3-digit non-power-of-two should return 28");

        // 1000 = 4 digits -> should return 24
        fontSize = TileViewModel.GetTileFontSize(1000);
        Assert.AreEqual(24, fontSize, "4-digit non-power-of-two should return 24");

        // 10000 = 5 digits -> should return 20
        fontSize = TileViewModel.GetTileFontSize(10000);
        Assert.AreEqual(20, fontSize, "5-digit non-power-of-two should return 20");
    }

    #endregion
}
