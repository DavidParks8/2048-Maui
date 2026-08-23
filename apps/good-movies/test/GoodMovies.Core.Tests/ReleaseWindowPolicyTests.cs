using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class ReleaseWindowPolicyTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    public void IsVisible_PastBoundary_IsInclusiveThroughDayThirteen()
    {
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(Today.AddDays(-13), Today));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(Today.AddDays(-14), Today));
    }

    [TestMethod]
    public void IsVisible_FutureBoundary_IsInclusiveForTwelveMonthsOnly()
    {
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(Today.AddMonths(12), Today));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(Today.AddMonths(12).AddDays(1), Today));
    }

    [TestMethod]
    public void IsVisible_LeapDayAndMonthEdges_UseDateOnlyCalendarArithmetic()
    {
        DateOnly leapDay = new(2028, 2, 29);

        Assert.AreEqual(
            new DateOnly(2028, 2, 16),
            ReleaseWindowPolicy.EarliestVisibleDate(leapDay)
        );
        Assert.AreEqual(new DateOnly(2029, 2, 28), ReleaseWindowPolicy.LatestVisibleDate(leapDay));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(new DateOnly(2028, 2, 16), leapDay));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(new DateOnly(2028, 2, 15), leapDay));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(new DateOnly(2029, 2, 28), leapDay));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(new DateOnly(2029, 3, 1), leapDay));

        DateOnly monthEnd = new(2026, 1, 31);
        Assert.AreEqual(new DateOnly(2027, 1, 31), ReleaseWindowPolicy.LatestVisibleDate(monthEnd));
    }

    [TestMethod]
    public void IsVisible_ReleaseRequiresUsAndAllowedType()
    {
        Assert.IsTrue(
            ReleaseWindowPolicy.IsVisible(
                new TheatricalRelease(Today, "US", TheatricalRelease.LimitedTheatricalType),
                Today
            )
        );
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(new TheatricalRelease(Today, "CA", 2), Today));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(new TheatricalRelease(Today, "US", 1), Today));
    }

    [TestMethod]
    public void FavoriteVisibility_UsesTheSameInclusiveWindow()
    {
        Assert.IsTrue(
            ReleaseWindowPolicy.IsVisible(new FavoriteEntry(1, Today.AddDays(-13)), Today)
        );
        Assert.IsFalse(
            ReleaseWindowPolicy.IsVisible(new FavoriteEntry(2, Today.AddDays(-14)), Today)
        );
        Assert.IsTrue(
            ReleaseWindowPolicy.IsVisible(new FavoriteEntry(3, Today.AddMonths(12)), Today)
        );
        Assert.IsFalse(
            ReleaseWindowPolicy.IsVisible(
                new FavoriteEntry(4, Today.AddMonths(12).AddDays(1)),
                Today
            )
        );
    }
}
