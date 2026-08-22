using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class ReleaseWindowPolicyTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    public void IsVisible_PastBoundary_IsInclusiveThroughDayThirteen()
    {
        ReleaseWindowPolicy policy = new();

        Assert.IsTrue(policy.IsVisible(Today.AddDays(-13), Today));
        Assert.IsFalse(policy.IsVisible(Today.AddDays(-14), Today));
    }

    [TestMethod]
    public void IsVisible_FutureBoundary_IsInclusiveForTwelveMonthsOnly()
    {
        ReleaseWindowPolicy policy = new();

        Assert.IsTrue(policy.IsVisible(Today.AddMonths(12), Today));
        Assert.IsFalse(policy.IsVisible(Today.AddMonths(12).AddDays(1), Today));
    }

    [TestMethod]
    public void IsVisible_LeapDayAndMonthEdges_UseDateOnlyCalendarArithmetic()
    {
        ReleaseWindowPolicy policy = new();
        DateOnly leapDay = new(2028, 2, 29);

        Assert.AreEqual(new DateOnly(2028, 2, 16), policy.EarliestVisibleDate(leapDay));
        Assert.AreEqual(new DateOnly(2029, 2, 28), policy.LatestVisibleDate(leapDay));
        Assert.IsTrue(policy.IsVisible(new DateOnly(2028, 2, 16), leapDay));
        Assert.IsFalse(policy.IsVisible(new DateOnly(2028, 2, 15), leapDay));
        Assert.IsTrue(policy.IsVisible(new DateOnly(2029, 2, 28), leapDay));
        Assert.IsFalse(policy.IsVisible(new DateOnly(2029, 3, 1), leapDay));

        DateOnly monthEnd = new(2026, 1, 31);
        Assert.AreEqual(new DateOnly(2027, 1, 31), policy.LatestVisibleDate(monthEnd));
    }

    [TestMethod]
    public void IsVisible_ReleaseRequiresUsAndAllowedType()
    {
        ReleaseWindowPolicy policy = new();

        Assert.IsTrue(
            policy.IsVisible(
                new TheatricalRelease(Today, "US", TheatricalRelease.LimitedTheatricalType),
                Today
            )
        );
        Assert.IsFalse(policy.IsVisible(new TheatricalRelease(Today, "CA", 2), Today));
        Assert.IsFalse(policy.IsVisible(new TheatricalRelease(Today, "US", 1), Today));
    }

    [TestMethod]
    public void FavoriteVisibility_UsesTheSameInclusiveWindow()
    {
        ReleaseWindowPolicy policy = new();

        Assert.IsTrue(policy.IsVisible(new FavoriteEntry(1, Today.AddDays(-13)), Today));
        Assert.IsFalse(policy.IsVisible(new FavoriteEntry(2, Today.AddDays(-14)), Today));
        Assert.IsTrue(policy.IsVisible(new FavoriteEntry(3, Today.AddMonths(12)), Today));
        Assert.IsFalse(
            policy.IsVisible(new FavoriteEntry(4, Today.AddMonths(12).AddDays(1)), Today)
        );
    }
}
