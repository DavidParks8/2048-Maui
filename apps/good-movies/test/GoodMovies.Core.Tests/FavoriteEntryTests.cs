using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class FavoriteEntryTests
{
    [TestMethod]
    public void PruneExpired_ExpiresExactlyAtLocalDayFourteen_UsingOnlyStoredEntry()
    {
        DateOnly today = new(2026, 8, 21);
        FavoriteEntry retained = new(1, today.AddDays(-13));
        FavoriteEntry expired = new(2, today.AddDays(-14));
        FavoriteEntry future = new(3, today.AddMonths(12));

        IReadOnlyList<FavoriteEntry> retainedEntries = FavoriteEntry.PruneExpired(
            new[] { retained, expired, future },
            today
        );

        Assert.AreEqual(2, retainedEntries.Count);
        Assert.IsTrue(retainedEntries.Contains(retained));
        Assert.IsFalse(retainedEntries.Contains(expired));
        Assert.IsTrue(expired.IsExpired(today));
        Assert.IsFalse(retained.IsExpired(today));
    }

    [TestMethod]
    public void PruneExpired_UsesStoredReleaseDateWithoutMovieData()
    {
        DateOnly today = new(2026, 2, 28);
        FavoriteEntry leapRelease = new(42, today.AddDays(-13));

        Assert.IsFalse(leapRelease.IsExpired(today));
        Assert.IsTrue(
            FavoriteEntry.PruneExpired(new[] { leapRelease }, today).Contains(leapRelease)
        );
    }
}
