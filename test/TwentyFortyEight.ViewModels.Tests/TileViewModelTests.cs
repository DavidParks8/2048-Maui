using TwentyFortyEight.ViewModels.Models;

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for TileViewModel.
/// </summary>
[TestClass]
public class TileViewModelTests
{
    [TestMethod]
    public void DisplayValue_WhenZero_ReturnsEmptyString()
    {
        // Arrange
        var tile = new TileViewModel { Value = 0 };

        // Assert
        Assert.AreEqual("", tile.DisplayValue);
    }

    [TestMethod]
    public void DisplayValue_WhenNonZero_ReturnsValueAsString()
    {
        // Arrange
        var tile = new TileViewModel { Value = 2048 };

        // Assert
        Assert.AreEqual("2048", tile.DisplayValue);
    }

    [TestMethod]
    public void ValueCategory_WhenEmpty_ReturnsEmpty()
    {
        // Arrange
        var tile = new TileViewModel { Value = 0 };

        // Assert
        Assert.AreEqual(TileValueCategory.Empty, tile.ValueCategory);
    }

    [TestMethod]
    public void ValueCategory_When2048_ReturnsValue2048()
    {
        // Arrange
        var tile = new TileViewModel { Value = 2048 };

        // Assert
        Assert.AreEqual(TileValueCategory.Value2048, tile.ValueCategory);
    }

    [TestMethod]
    public void ValueCategory_WhenOver2048_ReturnsHighValue()
    {
        // Arrange
        var tile = new TileViewModel { Value = 4096 };

        // Assert
        Assert.AreEqual(TileValueCategory.HighValue, tile.ValueCategory);
    }

    [TestMethod]
    public void PowerOf2_WhenZero_ReturnsZero()
    {
        // Arrange
        var tile = new TileViewModel { Value = 0 };

        // Assert
        Assert.AreEqual(0, tile.PowerOf2);
    }

    [TestMethod]
    public void PowerOf2_When2_Returns1()
    {
        // Arrange
        var tile = new TileViewModel { Value = 2 };

        // Assert
        Assert.AreEqual(1, tile.PowerOf2);
    }

    [TestMethod]
    public void PowerOf2_When2048_Returns11()
    {
        // Arrange
        var tile = new TileViewModel { Value = 2048 };

        // Assert
        Assert.AreEqual(11, tile.PowerOf2);
    }

    [TestMethod]
    public void UsesDarkText_WhenValue2_ReturnsTrue()
    {
        // Arrange
        var tile = new TileViewModel { Value = 2 };

        // Assert
        Assert.IsTrue(tile.UsesDarkText);
    }

    [TestMethod]
    public void UsesDarkText_WhenValue4_ReturnsTrue()
    {
        // Arrange
        var tile = new TileViewModel { Value = 4 };

        // Assert
        Assert.IsTrue(tile.UsesDarkText);
    }

    [TestMethod]
    public void UsesDarkText_WhenValue8_ReturnsFalse()
    {
        // Arrange
        var tile = new TileViewModel { Value = 8 };

        // Assert
        Assert.IsFalse(tile.UsesDarkText);
    }

    [TestMethod]
    public void FontSizeCategoryValue_WhenSmallNumber_ReturnsLarge()
    {
        // Arrange
        var tile = new TileViewModel { Value = 64 };

        // Assert
        Assert.AreEqual(FontSizeCategory.Large, tile.FontSizeCategoryValue);
    }

    [TestMethod]
    public void FontSizeCategoryValue_When128_ReturnsMedium()
    {
        // Arrange
        var tile = new TileViewModel { Value = 128 };

        // Assert
        Assert.AreEqual(FontSizeCategory.Medium, tile.FontSizeCategoryValue);
    }

    [TestMethod]
    public void FontSizeCategoryValue_When1024_ReturnsSmall()
    {
        // Arrange
        var tile = new TileViewModel { Value = 1024 };

        // Assert
        Assert.AreEqual(FontSizeCategory.Small, tile.FontSizeCategoryValue);
    }

    [TestMethod]
    public void UpdateValue_ChangesValue()
    {
        // Arrange
        var tile = new TileViewModel { Value = 2 };

        // Act
        tile.UpdateValue(4);

        // Assert
        Assert.AreEqual(4, tile.Value);
    }

    [TestMethod]
    public void UpdateValue_NotifiesDisplayValueChanged()
    {
        // Arrange
        var tile = new TileViewModel { Value = 2 };
        var propertyChangedRaised = false;
        tile.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TileViewModel.DisplayValue))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        tile.UpdateValue(4);

        // Assert
        Assert.IsTrue(propertyChangedRaised);
    }

    [TestMethod]
    public void IsNewTile_DefaultsFalse()
    {
        // Arrange & Act
        var tile = new TileViewModel();

        // Assert
        Assert.IsFalse(tile.IsNewTile);
    }

    [TestMethod]
    public void IsMerged_DefaultsFalse()
    {
        // Arrange & Act
        var tile = new TileViewModel();

        // Assert
        Assert.IsFalse(tile.IsMerged);
    }

    [TestMethod]
    public void Row_CanBeSetAndGet()
    {
        // Arrange
        var tile = new TileViewModel { Row = 2 };

        // Assert
        Assert.AreEqual(2, tile.Row);
    }

    [TestMethod]
    public void Column_CanBeSetAndGet()
    {
        // Arrange
        var tile = new TileViewModel { Column = 3 };

        // Assert
        Assert.AreEqual(3, tile.Column);
    }
}
