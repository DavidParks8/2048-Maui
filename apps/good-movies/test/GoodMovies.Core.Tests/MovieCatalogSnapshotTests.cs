using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class MovieCatalogSnapshotTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    public void Create_RemovesUnsafeExpiredAndOutOfWindowMovies_AndSortsDeterministically()
    {
        Movie laterTitle = Movie(1, "Zebra", Today.AddDays(2), "G");
        Movie earlierTitle = Movie(2, "Alpha", Today.AddDays(2), "PG");
        Movie sameTitleDifferentId = Movie(3, "Alpha", Today.AddDays(2), "PG");
        Movie oldestVisible = Movie(4, "Old", Today.AddDays(-13), "G");
        Movie expired = Movie(5, "Expired", Today.AddDays(-14), "G");
        Movie tooFarAhead = Movie(6, "Too Far", Today.AddMonths(12).AddDays(1), "PG");
        Movie unsafeMovie = Movie(7, "Unsafe", Today, "R");
        Movie foreign = new(
            8,
            "Foreign",
            "G",
            new TheatricalRelease(Today, "CA", TheatricalRelease.TheatricalType)
        );

        MovieCatalogSnapshot snapshot = MovieCatalogSnapshot.Create(
            new[]
            {
                laterTitle,
                unsafeMovie,
                tooFarAhead,
                sameTitleDifferentId,
                foreign,
                expired,
                earlierTitle,
                oldestVisible,
            },
            Today
        );

        Assert.AreEqual(4, snapshot.Movies.Count);
        Assert.AreEqual("Old", snapshot.Movies[0].Title);
        Assert.AreEqual(2, snapshot.Movies[1].Id);
        Assert.AreEqual(3, snapshot.Movies[2].Id);
        Assert.AreEqual("Zebra", snapshot.Movies[3].Title);
    }

    [TestMethod]
    public void Search_RunsOnTheAlreadyPolicyFilteredSnapshot()
    {
        Movie visible = Movie(1, "Visible Adventure", Today, "G");
        Movie expiredMatchingTitle = Movie(2, "Visible Adventure Old", Today.AddDays(-14), "G");
        Movie unsafeMatchingTitle = Movie(3, "Visible Adventure Unsafe", Today, "PG-13");

        MovieCatalogSnapshot snapshot = MovieCatalogSnapshot.Create(
            new[] { visible, expiredMatchingTitle, unsafeMatchingTitle },
            Today
        );

        IReadOnlyList<Movie> results = snapshot.Search("adventure").Movies;

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(visible.Id, results[0].Id);
    }

    [TestMethod]
    public void FilterCachedMovies_UsesTheSamePolicyAsCatalog()
    {
        Movie visible = Movie(1, "Visible", Today, "PG");
        Movie expired = Movie(2, "Expired", Today.AddDays(-14), "PG");

        IReadOnlyList<Movie> cached = MovieCatalogSnapshot.FilterCachedMovies(
            new[] { visible, expired },
            Today
        );

        Assert.AreEqual(1, cached.Count);
        Assert.AreEqual(visible.Id, cached[0].Id);
    }

    private static Movie Movie(int id, string title, DateOnly releaseDate, string certification) =>
        new(
            id,
            title,
            certification,
            new TheatricalRelease(releaseDate, "US", TheatricalRelease.TheatricalType)
        );
}
