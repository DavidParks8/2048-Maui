using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class ReleaseWindowPolicyTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    public void IsVisible_PastReleases_DoNotExpireByAge()
    {
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(Today.AddDays(-13), Today));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(Today.AddDays(-14), Today));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(Today.AddMonths(-6), Today));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(default(DateOnly), Today));
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

        Assert.AreEqual(new DateOnly(2029, 2, 28), ReleaseWindowPolicy.LatestVisibleDate(leapDay));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(new DateOnly(2028, 2, 16), leapDay));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(new DateOnly(2028, 2, 15), leapDay));
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
    public void FavoriteVisibility_RetainsPastFavoritesUntilProviderReconciliation()
    {
        Assert.IsTrue(
            ReleaseWindowPolicy.IsVisible(new FavoriteEntry(1, Today.AddDays(-13)), Today)
        );
        Assert.IsTrue(
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
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(new FavoriteEntry(0, Today), Today));
    }

    [TestMethod]
    public void Visibility_NeverTheatricalMovieAndFavorite_ExpireExactlyOnDayFourteen()
    {
        DateOnly releaseDate = Today.AddDays(-13);
        Movie movie = new(
            1,
            "TV release",
            "G",
            new[] { new TheatricalRelease(releaseDate, "US", TheatricalRelease.TvType) }
        );
        FavoriteEntry favorite = new(1, releaseDate, HasBeenInTheaters: false);

        Assert.IsTrue(MovieSafetyPolicy.IsSafe(movie));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(movie, Today));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(favorite, Today));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(movie, Today.AddDays(1)));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(favorite, Today.AddDays(1)));
    }

    [TestMethod]
    public void Visibility_TheatricalHistory_ExpiresImmediatelyAfterConfirmedExit()
    {
        DateOnly releaseDate = Today.AddDays(-1);
        Movie ended = new(1, "Ended", "PG", releaseDate, hasBeenInTheaters: true);
        FavoriteEntry endedFavorite = new(1, releaseDate, HasBeenInTheaters: true);
        Movie playing = new(2, "Long run", "G", Today.AddDays(-120), isInTheaters: true);
        FavoriteEntry playingFavorite = new(2, Today.AddDays(-120), true, true);

        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(ended, Today));
        Assert.IsFalse(ReleaseWindowPolicy.IsVisible(endedFavorite, Today));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(playing, Today.AddMonths(3)));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(playingFavorite, Today.AddMonths(3)));
        Assert.IsTrue(playing.HasBeenInTheaters);
    }

    [TestMethod]
    public void Visibility_UpcomingMovieWithTheatricalHistory_RemainsVisibleForRerelease()
    {
        Movie rerelease = new(1, "Return", "G", Today.AddDays(10), hasBeenInTheaters: true);
        FavoriteEntry favorite = new(1, Today.AddDays(10), HasBeenInTheaters: true);

        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(rerelease, Today));
        Assert.IsTrue(ReleaseWindowPolicy.IsVisible(favorite, Today));
    }

    [TestMethod]
    public void GetVisibleRelease_PrefersUpcomingRunUnlessMovieIsCurrentlyPlaying()
    {
        TheatricalRelease past = new(Today.AddDays(-60), "US", 3);
        TheatricalRelease upcoming = new(Today.AddDays(10), "US", 3);
        Movie playing = new(1, "Playing", "G", new[] { past, upcoming }, isInTheaters: true);
        Movie rerelease = new(2, "Rerelease", "G", new[] { past, upcoming });

        Assert.AreSame(past, ReleaseWindowPolicy.GetVisibleRelease(playing, Today));
        Assert.AreSame(upcoming, ReleaseWindowPolicy.GetVisibleRelease(rerelease, Today));
    }
}
