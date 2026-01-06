using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class RulesetIdTests
{
    [TestMethod]
    public void RulesetId_SameConfigValues_ProducesSameId()
    {
        // Arrange
        var a = new GameConfig { Size = 4, WinTile = 2048 };
        var b = new GameConfig { Size = 4, WinTile = 2048 };

        // Act
        var idA = a.RulesetId;
        var idB = b.RulesetId;

        // Assert
        Assert.AreEqual(idA, idB);
        StringAssert.StartsWith(idA, "v1:");
        StringAssert.Contains(idA, "size=4");
        StringAssert.Contains(idA, "win=2048");
    }

    [TestMethod]
    public void RulesetId_DifferentConfigValues_ProducesDifferentId()
    {
        var a = new GameConfig { Size = 4, WinTile = 2048 };
        var b = new GameConfig { Size = 5, WinTile = 2048 };

        Assert.AreNotEqual(a.RulesetId, b.RulesetId);
    }
}
