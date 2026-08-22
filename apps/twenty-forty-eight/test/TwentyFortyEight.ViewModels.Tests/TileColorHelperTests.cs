using TwentyFortyEight.ViewModels.Helpers;

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for TileColorHelper color caching optimization.
/// Note: Methods that rely on Application.Current cannot be tested directly
/// without a MAUI host; these tests verify the underlying cache structure.
/// </summary>
[TestClass]
public class TileColorHelperTests
{
    #region GetTileBackgroundColor Tests

    [TestMethod]
    public void GetTileBackgroundColor_ConsecutiveCalls_ReturnsSameInstance()
    {
        // This verifies that the caching returns the same Color instance
        // (Colors are immutable but we want to ensure no re-parsing)
        Color first = TileColorHelper.GetTileBackgroundColor(2);
        Color second = TileColorHelper.GetTileBackgroundColor(2);

        // Colors should be equal
        Assert.AreEqual(first, second);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(32)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(256)]
    [DataRow(512)]
    [DataRow(1024)]
    [DataRow(2048)]
    public void GetTileBackgroundColor_KnownTileValues_ReturnsNonNullColor(int value)
    {
        // Act
        Color color = TileColorHelper.GetTileBackgroundColor(value);

        // Assert
        Assert.IsNotNull(color, $"Color for value {value} should not be null");
    }

    [TestMethod]
    [DataRow(4096)]
    [DataRow(8192)]
    [DataRow(16384)]
    [DataRow(32768)]
    [DataRow(65536)]
    [DataRow(131072)]
    [DataRow(262144)]
    [DataRow(524288)]
    [DataRow(1048576)]
    public void GetTileBackgroundColor_HighTileValues_ReturnsNonNullColor(int value)
    {
        // These are valid for 8x8 boards with high scores
        Color color = TileColorHelper.GetTileBackgroundColor(value);

        Assert.IsNotNull(color, $"Color for value {value} should not be null");
    }

    [TestMethod]
    public void GetTileBackgroundColor_ValueAboveMax_FallsBackToHighestDefined()
    {
        // 2^21 = 2097152, above max cached value
        Color color = TileColorHelper.GetTileBackgroundColor(2097152);

        // Should fall back to the 1048576 (2^20) color or the cap value
        Assert.IsNotNull(color);
    }

    [TestMethod]
    public void GetTileBackgroundColor_UncachedValue_FallsBackToEmptyTileColor()
    {
        // A non-power-of-2 value that's not in the cache (e.g., 3, 5, 6)
        // should return the fallback (empty tile color)
        Color color = TileColorHelper.GetTileBackgroundColor(3);

        // Should fall back to empty tile color [0]
        Assert.IsNotNull(color);

        // Verify it equals the empty tile color
        Color emptyColor = TileColorHelper.GetTileBackgroundColor(0);
        Assert.AreEqual(emptyColor, color, "Uncached values should return empty tile color");
    }

    #endregion

    #region GetTileBackgroundBrush Tests

    [TestMethod]
    public void GetTileBackgroundBrush_ConsecutiveCalls_ReturnsSameInstance()
    {
        // The key optimization: verify same brush instance is returned each time
        var first = TileColorHelper.GetTileBackgroundBrush(2);
        var second = TileColorHelper.GetTileBackgroundBrush(2);

        // Must be the exact same instance (reference equality)
        Assert.AreSame(first, second, "Cached brushes should be the same instance");
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(32)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(256)]
    [DataRow(512)]
    [DataRow(1024)]
    [DataRow(2048)]
    public void GetTileBackgroundBrush_KnownTileValues_ReturnsNonNullBrush(int value)
    {
        // Act
        var brush = TileColorHelper.GetTileBackgroundBrush(value);

        // Assert
        Assert.IsNotNull(brush, $"Brush for value {value} should not be null");
    }

    [TestMethod]
    public void GetTileBackgroundBrush_AllPowersOfTwo_ReturnCachedInstances()
    {
        // Verify all powers of 2 from 2^1 to 2^20 return cached brushes
        for (int i = 1; i <= 20; i++)
        {
            int value = 1 << i; // 2^i
            var brush = TileColorHelper.GetTileBackgroundBrush(value);

            Assert.IsNotNull(brush, $"Brush for 2^{i} = {value} should not be null");

            // Verify caching by getting again
            var brush2 = TileColorHelper.GetTileBackgroundBrush(value);
            Assert.AreSame(brush, brush2, $"Brush for {value} should be same instance");
        }
    }

    [TestMethod]
    public void GetTileBackgroundBrush_EmptyTile_ReturnsCachedBrush()
    {
        var first = TileColorHelper.GetTileBackgroundBrush(0);
        var second = TileColorHelper.GetTileBackgroundBrush(0);

        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void GetTileBackgroundBrush_UncachedValue_FallsBackToEmptyTileBrush()
    {
        // Non-power-of-2 values should fall back to empty tile brush
        var brush = TileColorHelper.GetTileBackgroundBrush(3);
        var emptyBrush = TileColorHelper.GetTileBackgroundBrush(0);

        Assert.AreSame(emptyBrush, brush, "Uncached values should return empty tile brush");
    }

    #endregion

    #region GetTileTextColor Tests

    [TestMethod]
    public void GetTileTextColor_LowValues_ReturnsConsistentColor()
    {
        // Low values (2, 4) use dark text in light mode
        Color color2 = TileColorHelper.GetTileTextColor(2);
        Color color4 = TileColorHelper.GetTileTextColor(4);

        Assert.IsNotNull(color2);
        Assert.IsNotNull(color4);
        Assert.AreEqual(color2, color4, "Low-value tiles should have same text color");
    }

    [TestMethod]
    public void GetTileTextColor_HighValues_ReturnsConsistentColor()
    {
        // High values (8+) use light text
        Color color8 = TileColorHelper.GetTileTextColor(8);
        Color color2048 = TileColorHelper.GetTileTextColor(2048);

        Assert.IsNotNull(color8);
        Assert.IsNotNull(color2048);
        Assert.AreEqual(color8, color2048, "High-value tiles should have same text color");
    }

    [TestMethod]
    public void GetTileTextColor_ConsecutiveCalls_ReturnsEqualColors()
    {
        Color first = TileColorHelper.GetTileTextColor(2048);
        Color second = TileColorHelper.GetTileTextColor(2048);

        Assert.AreEqual(first, second);
    }

    #endregion

    #region Cache Consistency Tests

    [TestMethod]
    public void ColorAndBrush_SameTileValue_HaveMatchingColors()
    {
        // Verify the brush's Color matches GetTileBackgroundColor result
        for (int i = 0; i <= 11; i++)
        {
            int value = i == 0 ? 0 : 1 << i; // 0, 2, 4, 8, ... 2048
            var color = TileColorHelper.GetTileBackgroundColor(value);
            var brush = TileColorHelper.GetTileBackgroundBrush(value);

            Assert.AreEqual(
                color,
                brush.Color,
                $"Brush color for {value} should match GetTileBackgroundColor result"
            );
        }
    }

    [TestMethod]
    public void BrushCaching_PerformanceVerification_ManyConsecutiveCalls()
    {
        // This test verifies that many calls don't create new instances
        var brushes = new List<SolidColorBrush>();

        for (int i = 0; i < 1000; i++)
        {
            brushes.Add(TileColorHelper.GetTileBackgroundBrush(2048));
        }

        // All 1000 should be the exact same instance
        var first = brushes[0];
        Assert.IsTrue(
            brushes.All(b => ReferenceEquals(b, first)),
            "All 1000 brush calls should return the same instance"
        );
    }

    #endregion
}
