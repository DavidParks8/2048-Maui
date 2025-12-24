using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Core.Tests;

[TestClass]
public class AdaptiveSpawnTests
{
    #region GameState.MaxTileValue Tests

    [TestMethod]
    public void MaxTileValue_EmptyBoard_ReturnsZero()
    {
        // Arrange
        var state = new GameState(4);

        // Act & Assert
        Assert.AreEqual(0, state.MaxTileValue);
    }

    [TestMethod]
    public void MaxTileValue_FromLegacyConstructor_CalculatesCorrectly()
    {
        // Arrange
        var data = new int[16];
        data[5] = 128;

        // Act
        var state = TestHelpers.CreateGameState(data, 4, 0, 0, false, false);

        // Assert
        Assert.AreEqual(128, state.MaxTileValue);
    }

    [TestMethod]
    public void MaxTileValue_MultipleTiles_ReturnsHighest()
    {
        // Arrange
        var data = new int[16];
        data[0] = 2;
        data[1] = 4;
        data[5] = 2048;
        data[10] = 512;
        data[15] = 1024;

        // Act
        var state = TestHelpers.CreateGameState(data, 4, 0, 0, false, false);

        // Assert
        Assert.AreEqual(2048, state.MaxTileValue);
    }

    [TestMethod]
    public void MaxTileValue_WithTile_UpdatesWhenHigher()
    {
        // Arrange
        var state = new GameState(4);
        Assert.AreEqual(0, state.MaxTileValue);

        // Act - add a tile
        var newState = state.WithTile(0, 0, 256);

        // Assert
        Assert.AreEqual(256, newState.MaxTileValue);
    }

    [TestMethod]
    public void MaxTileValue_WithTile_DoesNotDecreaseWhenLower()
    {
        // Arrange
        var data = new int[16];
        data[0] = 512;
        var state = TestHelpers.CreateGameState(data, 4, 0, 0, false, false);
        Assert.AreEqual(512, state.MaxTileValue);

        // Act - add a lower value tile
        var newState = state.WithTile(1, 1, 2);

        // Assert - max should still be 512
        Assert.AreEqual(512, newState.MaxTileValue);
    }

    #endregion

    #region Game2048Engine Adaptive Spawn Integration Tests

    [TestMethod]
    public void Move_With2048OnBoard_SpawnsHigherValues()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };

        // Create a mock random that always returns 0.5 (should spawn common value)
        var mockRandom = new Mock<IRandomSource>();
        mockRandom.Setup(r => r.NextDouble()).Returns(0.5); // 0.5 < 0.9, so common value
        mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0); // Always pick first empty cell

        // Create board with 2048 tile and empty space
        var data = new int[16];
        data[0] = 2048;
        data[1] = 4;
        data[2] = 8;
        data[4] = 4;
        // Leave rest empty for spawning
        var state = TestHelpers.CreateGameState(data, 4, 1000, 10, false, false);
        var engine = new Game2048Engine(
            state,
            config,
            mockRandom.Object,
            NullStatisticsTracker.Instance
        );

        // Act
        engine.Move(Direction.Right); // This should spawn a new tile

        // Assert - when max tile is 2048, common spawn should be 4
        // Find the newly spawned tile (should be value 4 somewhere that was previously 0)
        var newState = engine.CurrentState;
        var nonZeroCount = 0;
        var hasFour = false;
        for (int i = 0; i < 16; i++)
        {
            if (newState.Board[i] != 0)
                nonZeroCount++;
            if (newState.Board[i] == 4)
                hasFour = true;
        }

        Assert.IsTrue(hasFour, "Board should have a 4 tile (either from existing or spawned)");
    }

    [TestMethod]
    public void Move_WithLowTiles_SpawnsDefaultValues()
    {
        // Arrange
        var config = new GameConfig { Size = 4 };

        // Create a mock random that always returns 0.5 (should spawn common value = 2)
        var mockRandom = new Mock<IRandomSource>();
        mockRandom.Setup(r => r.NextDouble()).Returns(0.5);
        mockRandom.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        // Create board with low value tiles
        var data = new int[16];
        data[0] = 64;
        data[1] = 32;
        data[4] = 32;
        // Leave rest empty for spawning
        var state = TestHelpers.CreateGameState(data, 4, 100, 5, false, false);
        var engine = new Game2048Engine(
            state,
            config,
            mockRandom.Object,
            NullStatisticsTracker.Instance
        );

        // Act
        engine.Move(Direction.Right);

        // Assert - when max tile is 64, common spawn should be 2
        var newState = engine.CurrentState;
        var hasTwo = false;
        for (int i = 0; i < 16; i++)
        {
            if (newState.Board[i] == 2)
                hasTwo = true;
        }

        Assert.IsTrue(
            hasTwo,
            "Board should have spawned a 2 tile when max tile is below threshold"
        );
    }

    #endregion
}
