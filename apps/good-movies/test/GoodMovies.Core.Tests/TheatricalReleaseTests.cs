using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class TheatricalReleaseTests
{
    [TestMethod]
    public void IsUsTheatrical_UsLimitedAndTheatrical_AreAccepted()
    {
        DateOnly date = new(2026, 8, 21);

        Assert.IsTrue(new TheatricalRelease(date, "US", 2).IsUsTheatrical);
        Assert.IsTrue(new TheatricalRelease(date, " us ", 3).IsUsTheatrical);
    }

    [TestMethod]
    public void IsUsTheatrical_NonUsAndUnsupportedTypes_AreRejected()
    {
        DateOnly date = new(2026, 8, 21);
        int[] unsupportedTypes = { 1, 4, 0, -1 };

        Assert.IsFalse(new TheatricalRelease(date, "CA", 2).IsUsTheatrical);
        Assert.IsFalse(new TheatricalRelease(date, "US", 1).IsUsTheatrical);

        foreach (int releaseType in unsupportedTypes)
        {
            Assert.IsFalse(new TheatricalRelease(date, "US", releaseType).IsUsTheatrical);
        }
    }

    [TestMethod]
    public void MovieSafetyPolicy_RequiresAtLeastOneUsTheatricalRelease()
    {
        MovieSafetyPolicy policy = new();
        DateOnly date = new(2026, 8, 21);

        Movie foreignOnly = new(
            1,
            "Foreign",
            "G",
            new[] { new TheatricalRelease(date, "GB", TheatricalRelease.TheatricalType) }
        );
        Movie streamingOnly = new(
            2,
            "Streaming",
            "PG",
            new[] { new TheatricalRelease(date, "US", 1) }
        );
        Movie oneAllowedRelease = new(
            3,
            "Allowed",
            "PG",
            new[] { new TheatricalRelease(date, "GB", 3), new TheatricalRelease(date, "US", 2) }
        );

        Assert.IsFalse(policy.IsSafe(foreignOnly));
        Assert.IsFalse(policy.IsSafe(streamingOnly));
        Assert.IsTrue(policy.IsSafe(oneAllowedRelease));
    }
}
