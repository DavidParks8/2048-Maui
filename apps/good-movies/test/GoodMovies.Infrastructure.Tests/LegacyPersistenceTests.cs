using System.Text;
using GoodMovies.Core;
using GoodMovies.Infrastructure;

namespace GoodMovies.Infrastructure.Tests;

[TestClass]
public sealed class LegacyPersistenceTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    private const string LegacyFavoritesJson =
        "[{\"movieId\":1,\"releaseDate\":\"2026-08-08\"},"
        + "{\"movieId\":2,\"releaseDate\":\"2026-08-07\"},"
        + "{\"movieId\":3,\"usTheatricalReleaseDate\":\"2026-08-21\"}]";

    private const string PrunedCurrentFavoritesJson =
        "[{\"movieId\":1,\"usTheatricalReleaseDate\":\"2026-08-08\"},"
        + "{\"movieId\":3,\"usTheatricalReleaseDate\":\"2026-08-21\"}]";

    private const string LegacyCatalogJson =
        "{\"refreshedAt\":\"2026-08-21T12:00:00+00:00\",\"movies\":["
        + "{\"id\":1,\"title\":\"Legacy Safe\",\"overview\":\"Legacy overview\","
        + "\"posterPath\":\"/legacy.jpg\",\"posterUri\":\"https://image.tmdb.org/t/p/w500/legacy.jpg\","
        + "\"originalLanguage\":\"en\",\"certification\":\"PG\","
        + "\"usTheatricalReleaseDate\":\"2026-08-21\","
        + "\"releases\":[{\"releaseDate\":\"2026-08-21\",\"countryCode\":\"US\",\"releaseType\":3}],"
        + "\"genres\":[{\"id\":16,\"name\":\"Animation\"}],\"genreIds\":[16],"
        + "\"trailers\":[{\"key\":\"Legacy_123\",\"name\":\"Trailer\",\"site\":\"YouTube\",\"type\":\"Trailer\",\"isOfficial\":true,\"languageCode\":\"en\"}]},"
        + "{\"id\":2,\"title\":\"Unsafe\",\"certification\":\"PG-13\",\"releases\":[{\"releaseDate\":\"2026-08-21\",\"countryCode\":\"US\",\"releaseType\":3}]},"
        + "{\"id\":3,\"title\":\"Expired\",\"certification\":\"G\",\"releases\":[{\"releaseDate\":\"2026-08-07\",\"countryCode\":\"US\",\"releaseType\":3}]},"
        + "{\"id\":4,\"title\":\"Foreign\",\"certification\":\"G\",\"releases\":[{\"releaseDate\":\"2026-08-21\",\"countryCode\":\"CA\",\"releaseType\":3}]},"
        + "{\"id\":5,\"title\":\"Legacy Date\",\"certification\":\"G\",\"usTheatricalReleaseDate\":\"2026-08-22\"}]}";

    [TestMethod]
    public async Task Favorites_LoadsLegacyAndCurrentDates_AndPrunesExpiredEntries()
    {
        using TestDirectory directory = new();
        string path = Path.Combine(directory.Path, "favorites.json");
        await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes(LegacyFavoritesJson));
        JsonFavoritesStore store = new(
            new FileSystemPathProvider(directory.Path),
            new GoodMoviesInfrastructureOptions { FavoritesFileName = "favorites.json" }
        );

        FavoritesResult result = await store.GetAsync(Today);

        Assert.AreEqual(FavoritesResultStatus.Succeeded, result.Status, result.Error?.ToString());
        CollectionAssert.AreEqual(
            new[] { 1, 3 },
            result.Entries.Select(entry => entry.MovieId).ToArray()
        );
        Assert.AreEqual(Today.AddDays(-13), result.Entries[0].UsTheatricalReleaseDate);
        Assert.AreEqual(Today, result.Entries[1].UsTheatricalReleaseDate);
        Assert.AreEqual(PrunedCurrentFavoritesJson, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task Catalog_LoadsShippedJsonShape_AndReappliesCurrentPolicies()
    {
        using TestDirectory directory = new();
        string path = Path.Combine(directory.Path, "catalog.json");
        byte[] fixture = Encoding.UTF8.GetBytes(LegacyCatalogJson);
        await File.WriteAllBytesAsync(path, fixture);
        CollectionAssert.AreEqual(fixture, await File.ReadAllBytesAsync(path));
        GoodMoviesInfrastructureOptions options = new() { CatalogCacheFileName = "catalog.json" };
        JsonMovieCatalogCache cache = new(
            new FileSystemPathProvider(directory.Path),
            options,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 21, 13, 0, 0, TimeSpan.Zero))
        );

        CatalogCacheReadResult result = await cache.ReadAsync(Today);

        Assert.AreEqual(CatalogCacheStatus.Available, result.Status, result.Error?.ToString());
        Assert.IsFalse(result.IsStale);
        CollectionAssert.AreEquivalent(
            new[] { 1, 5 },
            result.Movies.Select(movie => movie.Id).ToArray()
        );

        Movie legacy = result.Movies.Single(movie => movie.Id == 1);
        Assert.AreEqual("Legacy overview", legacy.Overview);
        Assert.AreEqual("PG", legacy.Certification?.Code);
        Assert.AreEqual("/legacy.jpg", legacy.PosterPath);
        Assert.AreEqual(
            "https://image.tmdb.org/t/p/w500/legacy.jpg",
            legacy.PosterUri?.AbsoluteUri
        );
        CollectionAssert.AreEqual(new[] { 16 }, legacy.GenreIds.ToArray());
        Assert.AreEqual("Animation", legacy.Genres.Single().Name);
        Assert.AreEqual(Today, legacy.UsTheatricalReleaseDate);

        Movie dateOnlyLegacy = result.Movies.Single(movie => movie.Id == 5);
        Assert.AreEqual(Today.AddDays(1), dateOnlyLegacy.UsTheatricalReleaseDate);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "GoodMoviesLegacyFixtures",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
