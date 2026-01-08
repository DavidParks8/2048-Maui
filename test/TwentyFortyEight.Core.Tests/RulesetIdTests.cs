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
        Assert.AreEqual(string.Empty, idA);
    }

    [TestMethod]
    public void RulesetId_DifferentConfigValues_ProducesDifferentId()
    {
        var a = new GameConfig { Size = 4, WinTile = 2048 };
        var b = new GameConfig { Size = 5, WinTile = 2048 };

        Assert.AreNotEqual(a.RulesetId, b.RulesetId);
    }

    [TestMethod]
    public void RulesetId_DefaultValuesAreOmitted()
    {
        var nonDefaultSize = new GameConfig { Size = 5, WinTile = 2048 };
        var nonDefaultWin = new GameConfig { Size = 4, WinTile = 4096 };

        StringAssert.Contains(nonDefaultSize.RulesetId, "size=5");
        Assert.IsFalse(nonDefaultSize.RulesetId.Contains("win=", StringComparison.Ordinal));

        StringAssert.Contains(nonDefaultWin.RulesetId, "win=4096");
        Assert.IsFalse(nonDefaultWin.RulesetId.Contains("size=", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RulesetId_Walltastrophy_IncludesModeToken()
    {
        var config = new GameConfig
        {
            Size = 4,
            WinTile = 2048,
            Mode = GameMode.Walltastrophy,
        };

        StringAssert.Contains(config.RulesetId, "mode=walltastrophy");
    }

    [TestMethod]
    public void RulesetId_Classic_OmitsModeToken()
    {
        var config = new GameConfig
        {
            Size = 5,
            WinTile = 2048,
            Mode = GameMode.Classic,
        };

        StringAssert.Contains(config.RulesetId, "size=5");
        Assert.IsFalse(config.RulesetId.Contains("mode=", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RulesetId_RepeatedAccess_ReturnsSameInstance()
    {
        var config = new GameConfig
        {
            Size = 5,
            WinTile = 4096,
            Mode = GameMode.Walltastrophy,
        };

        var first = config.RulesetId;
        var second = config.RulesetId;

        Assert.AreSame(first, second);
    }
}
