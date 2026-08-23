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
            new[] { new TheatricalRelease(Today, "CA", TheatricalRelease.TheatricalType) }
        );

        MovieCatalogSnapshot snapshot = new MovieCatalogSnapshot(
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
    public void Create_CopiesTheSourceCollection()
    {
        Movie original = Movie(1, "Original", Today, "G");
        List<Movie> source = new() { original };
        MovieCatalogSnapshot snapshot = new(source, Today);

        source.Clear();
        source.Add(Movie(2, "Replacement", Today, "G"));

        Assert.AreEqual(1, snapshot.Movies.Count);
        Assert.AreSame(original, snapshot.Movies[0]);
    }

    [TestMethod]
    public void Create_SortsByTheFirstCurrentlyVisibleRelease()
    {
        Movie rerelease = new(
            1,
            "Rerelease",
            "G",
            new[]
            {
                new TheatricalRelease(
                    Today.AddDays(-14),
                    "US",
                    TheatricalRelease.LimitedTheatricalType
                ),
                new TheatricalRelease(Today.AddDays(2), "US", TheatricalRelease.TheatricalType),
            }
        );
        Movie tomorrow = Movie(2, "Tomorrow", Today.AddDays(1), "G");

        MovieCatalogSnapshot snapshot = new(new[] { rerelease, tomorrow }, Today);

        CollectionAssert.AreEqual(
            new[] { tomorrow.Id, rerelease.Id },
            snapshot.Movies.Select(movie => movie.Id).ToArray()
        );
    }

    private static Movie Movie(int id, string title, DateOnly releaseDate, string certification) =>
        new(
            id,
            title,
            certification,
            new[] { new TheatricalRelease(releaseDate, "US", TheatricalRelease.TheatricalType) }
        );
}
