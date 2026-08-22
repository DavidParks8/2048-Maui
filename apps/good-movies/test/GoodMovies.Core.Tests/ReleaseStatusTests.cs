using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class ReleaseStatusTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    public void GetStatus_ReleaseDay_IsInTheatersToday()
    {
        ReleaseStatusInfo status = ReleaseStatusHelpers.GetStatusInfo(Today, Today);

        Assert.AreEqual(ReleaseStatus.Today, status.Status);
        Assert.AreEqual(Today, status.ReleaseDate);
    }

    [TestMethod]
    public void GetStatus_PastRetainedRelease_IsInTheatersNow()
    {
        ReleaseStatusInfo status = ReleaseStatusHelpers.GetStatusInfo(Today.AddDays(-13), Today);

        Assert.AreEqual(ReleaseStatus.InTheatersNow, status.Status);
    }

    [TestMethod]
    public void GetStatus_FutureRelease_UsesSingularAndPluralSleeps()
    {
        ReleaseStatusInfo tomorrow = ReleaseStatusHelpers.GetStatusInfo(Today.AddDays(1), Today);
        ReleaseStatusInfo later = ReleaseStatusHelpers.GetStatusInfo(Today.AddDays(4), Today);

        Assert.AreEqual(ReleaseStatus.Future, tomorrow.Status);
        Assert.AreEqual(1, tomorrow.Sleeps);
        Assert.AreEqual(ReleaseStatus.Future, later.Status);
        Assert.AreEqual(4, later.Sleeps);
    }

    [TestMethod]
    public void GetStatus_ExpiredPastRelease_IsNotReportedAsInTheatersNow()
    {
        ReleaseStatusInfo status = ReleaseStatusHelpers.GetStatusInfo(Today.AddDays(-14), Today);

        Assert.AreEqual(ReleaseStatus.Expired, status.Status);
    }
}
