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
}
