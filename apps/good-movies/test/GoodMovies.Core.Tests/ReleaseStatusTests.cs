using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class ReleaseStatusTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    public void GetStatus_ReleaseDay_IsInTheatersToday()
    {
        ReleaseStatusInfo status = ReleaseWindowPolicy.GetStatusInfo(
            Today,
            Today,
            isInTheaters: true
        );

        Assert.AreEqual(ReleaseStatus.Today, status.Status);
    }

    [TestMethod]
    public void GetStatus_OldMovieStillPlaying_IsInTheatersNow()
    {
        ReleaseStatusInfo status = ReleaseWindowPolicy.GetStatusInfo(
            Today.AddDays(-90),
            Today,
            isInTheaters: true
        );

        Assert.AreEqual(ReleaseStatus.InTheatersNow, status.Status);
    }

    [TestMethod]
    public void GetStatus_FutureRelease_UsesSingularAndPluralSleeps()
    {
        ReleaseStatusInfo tomorrow = ReleaseWindowPolicy.GetStatusInfo(Today.AddDays(1), Today);
        ReleaseStatusInfo later = ReleaseWindowPolicy.GetStatusInfo(Today.AddDays(4), Today);

        Assert.AreEqual(ReleaseStatus.Future, tomorrow.Status);
        Assert.AreEqual(1, tomorrow.Sleeps);
        Assert.AreEqual(ReleaseStatus.Future, later.Status);
        Assert.AreEqual(4, later.Sleeps);
    }

    [TestMethod]
    public void GetStatus_ReleaseDateAlone_DoesNotClaimMovieIsInTheaters()
    {
        ReleaseStatusInfo status = ReleaseWindowPolicy.GetStatusInfo(Today.AddDays(-14), Today);

        Assert.AreEqual(ReleaseStatus.Released, status.Status);
        Assert.AreEqual(
            ReleaseStatus.Released,
            ReleaseWindowPolicy.GetStatusInfo(Today, Today).Status
        );
    }

    [TestMethod]
    public void GetStatus_PlayingBeforePublishedReleaseDate_DoesNotShowCountdown()
    {
        ReleaseStatusInfo status = ReleaseWindowPolicy.GetStatusInfo(
            Today.AddDays(2),
            Today,
            isInTheaters: true
        );

        Assert.AreEqual(ReleaseStatus.InTheatersNow, status.Status);
        Assert.AreEqual(0, status.Sleeps);
    }
}
