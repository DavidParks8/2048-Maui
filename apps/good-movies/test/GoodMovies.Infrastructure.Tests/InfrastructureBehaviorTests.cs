using System.Collections.Concurrent;
using System.Net;
using System.Text;
using GoodMovies.Core;
using GoodMovies.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoodMovies.Infrastructure.Tests;

[TestClass]
public sealed class InfrastructureBehaviorTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    [DataRow(TheatricalRelease.TheatricalType, true)]
    [DataRow(TheatricalRelease.DigitalType, false)]
    public async Task TmdbClient_PreviouslyTheatricalTitle_OnlyTheatricalRereleaseCanKeepIt(
        int futureReleaseType,
        bool shouldRemain
    )
    {
        Movie tracked = new(1, "Returning movie", "PG", Today.AddDays(-1), isInTheaters: true);
        using FakeHandler handler = new(request =>
            request.RequestUri!.AbsolutePath switch
            {
                "/3/discover/movie" => Json(Discover(1, Candidate(1, "Returning movie"))),
                "/3/genre/movie/list" => Json("""{"genres":[]}"""),
                "/3/movie/1/release_dates" => Json(
                    $$"""{"results":[{"iso_3166_1":"US","release_dates":[{"certification":"PG","release_date":"{{Today.AddDays(-1):yyyy-MM-dd}}T00:00:00Z","type":3},{"certification":"PG","release_date":"{{Today.AddDays(5):yyyy-MM-dd}}T00:00:00Z","type":{{futureReleaseType}}}]}]}"""
                ),
                _ => NotFound(),
            }
        );

        CatalogFetchResult result = await CreateClient(handler)
            .FetchAsync(Today, trackedMovies: new[] { tracked });

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(shouldRemain ? 1 : 0, result.Movies.Count);
        if (shouldRemain)
        {
            Assert.AreEqual(Today.AddDays(5), result.Movies.Single().UsTheatricalReleaseDate);
            Assert.IsTrue(result.Movies.Single().HasBeenInTheaters);
            Assert.IsFalse(result.Movies.Single().IsInTheaters);
        }
    }

    [TestMethod]
    [DataRow(TheatricalRelease.DigitalType)]
    [DataRow(TheatricalRelease.TvType)]
    public async Task TmdbClient_RechecksTrackedRedirectedTitle_AndUsesItsActualStreamingReleaseDate(
        int releaseType
    )
    {
        Movie tracked = new(99, "Scheduled for theaters", "PG", Today.AddDays(-2));
        using FakeHandler handler = new(request =>
            request.RequestUri!.AbsolutePath switch
            {
                "/3/discover/movie" => Json(Discover(0)),
                "/3/genre/movie/list" => Json("""{"genres":[{"id":16,"name":"Animation"}]}"""),
                "/3/movie/99" => Json(
                    """{"id":99,"title":"Now on TV","original_language":"en","popularity":10,"genres":[{"id":16,"name":"Animation"}]}"""
                ),
                "/3/movie/99/release_dates" => Json(
                    UsRelease(Today.AddDays(-13), "PG", releaseType)
                ),
                _ => NotFound(),
            }
        );
        TmdbMovieCatalogClient client = CreateClient(handler);

        CatalogFetchResult retained = await client.FetchAsync(
            Today,
            trackedMovies: new[] { tracked }
        );
        Assert.IsTrue(retained.Succeeded, retained.Error?.ToString());
        Movie movie = retained.Movies.Single();
        Assert.IsFalse(movie.IsInTheaters);
        Assert.IsFalse(movie.HasBeenInTheaters);
        Assert.AreEqual(0, movie.UsTheatricalReleases.Count);
        Assert.AreEqual(releaseType, movie.UsReleases.Single().ReleaseType);
        Assert.AreEqual(Today.AddDays(-13), movie.UsReleaseDate);
        Assert.AreEqual("Animation", movie.Genres.Single().Name);

        CatalogFetchResult expired = await client.FetchAsync(
            Today.AddDays(1),
            trackedMovies: new[] { movie }
        );
        Assert.IsTrue(expired.Succeeded, expired.Error?.ToString());
        Assert.AreEqual(0, expired.Movies.Count);
    }

    [TestMethod]
    public async Task TmdbClient_TrackedRedirectedTitleWithUnsafeCertification_IsStillRejected()
    {
        Movie tracked = new(99, "Scheduled", "PG", Today);
        using FakeHandler handler = new(request =>
            request.RequestUri!.AbsolutePath switch
            {
                "/3/discover/movie" => Json(Discover(0)),
                "/3/genre/movie/list" => Json("""{"genres":[]}"""),
                "/3/movie/99" => Json("""{"id":99,"title":"Changed rating","popularity":10}"""),
                "/3/movie/99/release_dates" => Json(
                    UsRelease(Today, "PG-13", TheatricalRelease.TvType)
                ),
                _ => NotFound(),
            }
        );

        CatalogFetchResult result = await CreateClient(handler)
            .FetchAsync(Today, trackedMovies: new[] { tracked });

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(0, result.Movies.Count);
    }

    [TestMethod]
    public async Task CatalogService_PreservesPriorTheatricalHistory_AndDoesNotGiveEndedRunFourteenMoreDays()
    {
        using TestDirectory directory = new();
        JsonMovieCatalogCache cache = CreateCache(Path.Combine(directory.Path, "catalog.json"));
        JsonFavoritesStore favorites = CreateFavoritesStore(
            Path.Combine(directory.Path, "favorites.json")
        );
        Movie playing = new(1, "Short run", "G", Today.AddDays(-1), isInTheaters: true);
        await cache.WriteAsync(
            new MovieCatalogSnapshot(new[] { playing }, Today),
            DateTimeOffset.UtcNow
        );
        await favorites.ToggleAsync(new FavoriteEntry(1, Today.AddDays(-1), true, true), Today);
        Movie ended = new(1, "Short run", "G", Today.AddDays(-1));
        MovieCatalogService service = new(
            new StubCatalogProvider(
                CatalogFetchResult.Success(new[] { ended }, DateTimeOffset.UtcNow)
            ),
            cache,
            new FixedClock(Today),
            favoritesStore: favorites
        );

        CatalogResult result = await service.RefreshAsync();

        Assert.AreEqual(CatalogResultStatus.Refreshed, result.Status, result.Error?.ToString());
        Assert.AreEqual(0, result.Movies.Count);
        Assert.AreEqual(0, (await cache.ReadAsync(Today)).Movies.Count);
        Assert.AreEqual(0, (await favorites.GetAsync(Today)).Entries.Count);
    }

    [TestMethod]
    public async Task Persistence_ExpiresNeverTheatricalMovieAndFavoriteOnDayFourteen_WhileKeepingPlayingTitle()
    {
        using TestDirectory directory = new();
        string catalogPath = Path.Combine(directory.Path, "catalog.json");
        string favoritesPath = Path.Combine(directory.Path, "favorites.json");
        JsonMovieCatalogCache cache = CreateCache(catalogPath);
        JsonFavoritesStore favorites = CreateFavoritesStore(favoritesPath);
        Movie streaming = new(
            1,
            "On TV",
            "PG",
            new[] { new TheatricalRelease(Today.AddDays(-13), "US", TheatricalRelease.TvType) }
        );
        Movie playing = new(2, "Long run", "G", Today.AddDays(-120), isInTheaters: true);
        await cache.WriteAsync(
            new MovieCatalogSnapshot(new[] { streaming, playing }, Today),
            DateTimeOffset.UtcNow
        );
        await favorites.ToggleAsync(new FavoriteEntry(1, Today.AddDays(-13), false), Today);
        await favorites.ToggleAsync(new FavoriteEntry(2, Today.AddDays(-120), true, true), Today);
        Assert.AreEqual(2, (await cache.ReadAsync(Today)).Movies.Count);
        Assert.AreEqual(2, (await favorites.GetAsync(Today)).Entries.Count);

        Assert.AreEqual(2, (await cache.ReadAsync(Today.AddDays(1))).Movies.Single().Id);
        Assert.AreEqual(2, (await favorites.GetAsync(Today.AddDays(1))).Entries.Single().MovieId);
        Assert.AreEqual(
            2,
            (await CreateFavoritesStore(favoritesPath).GetAsync(Today.AddDays(1)))
                .Entries.Single()
                .MovieId
        );
        Assert.IsTrue((await cache.ReadAsync(Today.AddDays(1))).Movies.Single().HasBeenInTheaters);
    }

    [TestMethod]
    public async Task TmdbClient_NowPlayingTraversesAllUsPages_AndKeepsOnlyVerifiedSafeTheatricalMovies()
    {
        using FakeHandler handler = new(
            request =>
                request.RequestUri!.AbsolutePath switch
                {
                    "/3/genre/movie/list" => Json("""{"genres":[]}"""),
                    "/3/discover/movie" => Json(
                        Discover(
                            1,
                            Candidate(1, "Duplicate"),
                            Candidate(6, "Ended"),
                            Candidate(7, "Upcoming")
                        )
                    ),
                    "/3/movie/1/release_dates" => Json(UsRelease(Today.AddDays(-90), "G", 3)),
                    "/3/movie/2/release_dates" => Json(
                        $$"""{"results":[{"iso_3166_1":"US","release_dates":[{"certification":"PG","release_date":"2020-01-01T00:00:00Z","type":3},{"certification":"PG","release_date":"{{Today.AddDays(-20):yyyy-MM-dd}}T00:00:00Z","type":2}]}]}"""
                    ),
                    "/3/movie/3/release_dates" => Json(UsRelease(Today, "PG-13", 3)),
                    "/3/movie/4/release_dates" => Json(UsRelease(Today, "G", 3, "CA")),
                    "/3/movie/5/release_dates" => Json(UsRelease(Today, "G", 4)),
                    "/3/movie/6/release_dates" => Json(UsRelease(Today.AddDays(-2), "G", 3)),
                    "/3/movie/7/release_dates" => Json(UsRelease(Today.AddDays(20), "G", 3)),
                    _ => NotFound(),
                },
            nowPlayingResponder: request =>
                Json(
                    request.RequestUri!.Query.Contains("page=1", StringComparison.Ordinal)
                        ? NowPlaying(1, 2, Candidate(1, "Long run"), Candidate(3, "Unsafe"))
                        : NowPlaying(
                            2,
                            2,
                            Candidate(2, "Rerelease"),
                            Candidate(4, "Foreign"),
                            Candidate(5, "Digital")
                        )
                )
        );

        CatalogFetchResult result = await CreateClient(handler).FetchAsync(Today);

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        CollectionAssert.AreEquivalent(
            new[] { 1, 2, 7 },
            result.Movies.Select(movie => movie.Id).ToArray()
        );
        Assert.IsTrue(result.Movies.Single(movie => movie.Id == 1).IsInTheaters);
        Assert.AreEqual("Long run", result.Movies.Single(movie => movie.Id == 1).Title);
        Movie rerelease = result.Movies.Single(movie => movie.Id == 2);
        Assert.IsTrue(rerelease.IsInTheaters);
        Assert.AreEqual(Today.AddDays(-20), rerelease.UsTheatricalReleaseDate);
        Assert.IsFalse(result.Movies.Single(movie => movie.Id == 7).IsInTheaters);
        HttpRequestMessage[] theaterRequests = handler
            .Requests.Where(request => request.RequestUri!.AbsolutePath == "/3/movie/now_playing")
            .ToArray();
        Assert.AreEqual(2, theaterRequests.Length);
        Assert.IsTrue(
            theaterRequests.All(request =>
                request.RequestUri!.Query.Contains("region=US", StringComparison.Ordinal)
            )
        );
        Assert.IsTrue(
            theaterRequests.All(request =>
                !request.RequestUri!.Query.Contains("release_date", StringComparison.Ordinal)
            )
        );
        Assert.AreEqual(
            1,
            handler.Requests.Count(request =>
                request.RequestUri!.AbsolutePath == "/3/movie/1/release_dates"
            )
        );
    }

    [TestMethod]
    [DataRow("""{}""", 20)]
    [DataRow("""{"page":1,"total_pages":21,"results":[]}""", 20)]
    [DataRow("""{"page":1,"total_pages":2,"results":[]}""", 1)]
    [DataRow("""{"page":0,"total_pages":0,"results":[]}""", 20)]
    [DataRow("""{"page":1,"total_pages":0,"results":[{"id":1}]}""", 20)]
    public async Task TmdbClient_IncompleteNowPlayingResponse_FailsWithoutPartialCatalog(
        string response,
        int maxPages
    )
    {
        using FakeHandler handler = new(
            _ =>
                throw new AssertFailedException(
                    "Discovery must not run with incomplete theater status."
                ),
            nowPlayingResponder: _ => Json(response)
        );
        GoodMoviesInfrastructureOptions options = Options();
        options.MaxPages = maxPages;
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        CatalogFetchResult result = await client.FetchAsync(Today);

        Assert.AreEqual(CatalogFetchStatus.Failed, result.Status);
        Assert.AreEqual(0, result.Movies.Count);
        Assert.IsNotNull(result.Error);
    }

    [TestMethod]
    [DataRow(2, 3)]
    [DataRow(1, 2)]
    public async Task TmdbClient_InconsistentNowPlayingPagination_FailsWithoutPartialCatalog(
        int returnedPage,
        int totalPages
    )
    {
        using FakeHandler handler = new(
            _ =>
                throw new AssertFailedException(
                    "Discovery must not run with incomplete theater status."
                ),
            nowPlayingResponder: request =>
                Json(
                    request.RequestUri!.Query.Contains("page=1", StringComparison.Ordinal)
                        ? NowPlaying(1, 2, Candidate(1, "Partial"))
                        : NowPlaying(returnedPage, totalPages)
                )
        );

        CatalogFetchResult result = await CreateClient(handler).FetchAsync(Today);

        Assert.AreEqual(CatalogFetchStatus.Failed, result.Status);
        Assert.AreEqual(0, result.Movies.Count);
    }

    [TestMethod]
    public async Task CatalogService_RemovesReleasedMovieAndFavoriteOnlyAfterCompleteSuccessfulTheaterRefresh()
    {
        using TestDirectory directory = new();
        int refreshMode = 0;
        using FakeHandler handler = new(
            request =>
                request.RequestUri!.AbsolutePath switch
                {
                    "/3/discover/movie" => Json(Discover(0)),
                    "/3/genre/movie/list" => Json("""{"genres":[]}"""),
                    "/3/movie/1/release_dates" => Json(UsRelease(Today.AddDays(-90), "PG", 3)),
                    _ => NotFound(),
                },
            nowPlayingResponder: request =>
            {
                if (refreshMode == 2)
                {
                    return Json(NowPlaying(1, 0));
                }

                if (request.RequestUri!.Query.Contains("page=1", StringComparison.Ordinal))
                {
                    return Json(NowPlaying(1, 2, Candidate(1, "Long run")));
                }

                return refreshMode == 1 ? ServerError() : Json(NowPlaying(2, 2));
            }
        );
        JsonMovieCatalogCache cache = CreateCache(Path.Combine(directory.Path, "catalog.json"));
        JsonFavoritesStore favorites = CreateFavoritesStore(
            Path.Combine(directory.Path, "favorites.json")
        );
        MovieCatalogService service = new(
            CreateClient(handler),
            cache,
            new FixedClock(Today),
            favoritesStore: favorites
        );
        CatalogResult first = await service.RefreshAsync();
        Assert.AreEqual(CatalogResultStatus.Refreshed, first.Status, first.Error?.ToString());
        Movie playing = first.Movies.Single();
        Assert.IsTrue(playing.IsInTheaters);
        await favorites.ToggleAsync(
            new FavoriteEntry(playing.Id, playing.UsTheatricalReleaseDate!.Value, true, true),
            Today
        );

        refreshMode = 1;
        CatalogResult failed = await service.RefreshAsync();
        Assert.AreEqual(CatalogResultStatus.RefreshFailed, failed.Status);
        Assert.IsTrue(failed.Movies.Single().IsInTheaters);
        Assert.AreEqual(1, (await cache.ReadAsync(Today.AddDays(30))).Movies.Count);
        Assert.AreEqual(1, (await favorites.GetAsync(Today.AddDays(30))).Entries.Count);

        refreshMode = 2;
        CatalogResult ended = await service.RefreshAsync();
        Assert.AreEqual(CatalogResultStatus.Refreshed, ended.Status, ended.Error?.ToString());
        Assert.AreEqual(0, ended.Movies.Count);
        Assert.AreEqual(0, (await cache.ReadAsync(Today)).Movies.Count);
        Assert.AreEqual(0, (await favorites.GetAsync(Today)).Entries.Count);
    }

    [TestMethod]
    public async Task TmdbClient_UsesExactDiscoverFilters_AndTraversesEveryPage()
    {
        using FakeHandler handler = new(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            string query = request.RequestUri.Query;
            bool isFamilyPass = query.Contains("with_genres=", StringComparison.Ordinal);
            return path switch
            {
                "/3/genre/movie/list" => Json("""{"genres":[{"id":16,"name":"Animation"}]}"""),
                "/3/discover/movie" when isFamilyPass => Json(Discover(1)),
                "/3/discover/movie" => Json(
                    query.Contains("page=1", StringComparison.Ordinal)
                        ? Discover(2, Candidate(2, "Second", new[] { 16 }))
                        : Discover(2, Candidate(1, "First", new[] { 16 }))
                ),
                "/3/movie/1/release_dates" => Json(UsRelease(Today, "G", 3)),
                "/3/movie/2/release_dates" => Json(UsRelease(Today.AddDays(2), "PG", 2)),
                _ => NotFound(),
            };
        });
        GoodMoviesInfrastructureOptions options = Options();
        options.Token = "do-not-log";
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        CatalogFetchResult result = await client.FetchAsync(Today);

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(2, result.Movies.Count);
        HttpRequestMessage ratedRequest = handler.Requests.Single(request =>
            request.RequestUri!.AbsolutePath == "/3/discover/movie"
            && request.RequestUri.Query.Contains("page=1", StringComparison.Ordinal)
            && request.RequestUri.Query.Contains("certification.lte=PG", StringComparison.Ordinal)
        );
        string ratedQuery = ratedRequest.RequestUri!.Query;
        Assert.AreEqual("Bearer", ratedRequest.Headers.Authorization!.Scheme);
        Assert.AreEqual("do-not-log", ratedRequest.Headers.Authorization.Parameter);
        Assert.IsTrue(ratedQuery.Contains("region=US", StringComparison.Ordinal));
        Assert.IsTrue(ratedQuery.Contains("include_adult=false", StringComparison.Ordinal));
        Assert.IsTrue(ratedQuery.Contains("language=en-US", StringComparison.Ordinal));
        Assert.IsTrue(
            ratedQuery.Contains("sort_by=primary_release_date.asc", StringComparison.Ordinal)
        );
        Assert.IsTrue(
            ratedQuery.Contains($"release_date.gte={Today:yyyy-MM-dd}", StringComparison.Ordinal)
        );
        Assert.IsTrue(
            ratedQuery.Contains(
                $"release_date.lte={Today.AddMonths(12):yyyy-MM-dd}",
                StringComparison.Ordinal
            )
        );
        Assert.IsTrue(
            ratedQuery.Contains("with_release_type=2%7C3", StringComparison.OrdinalIgnoreCase)
        );
        Assert.IsTrue(ratedQuery.Contains("certification_country=US", StringComparison.Ordinal));
        Assert.IsFalse(ratedQuery.Contains("with_genres=", StringComparison.Ordinal));
        Assert.AreEqual(
            2,
            handler.Requests.Count(request =>
                request.RequestUri!.AbsolutePath == "/3/discover/movie"
                && request.RequestUri.Query.Contains(
                    "certification.lte=PG",
                    StringComparison.Ordinal
                )
            )
        );

        // The family pass drops the certification filter so that titles the MPAA
        // has not rated yet are still candidates.
        HttpRequestMessage familyRequest = handler.Requests.Single(request =>
            request.RequestUri!.AbsolutePath == "/3/discover/movie"
            && request.RequestUri.Query.Contains("with_genres=", StringComparison.Ordinal)
        );
        string familyQuery = familyRequest.RequestUri!.Query;
        Assert.IsTrue(
            familyQuery.Contains("with_genres=16%7C10751", StringComparison.OrdinalIgnoreCase)
        );
        Assert.IsTrue(familyQuery.Contains("with_original_language=en", StringComparison.Ordinal));
        Assert.IsFalse(familyQuery.Contains("certification.lte", StringComparison.Ordinal));
        Assert.IsTrue(
            familyQuery.Contains(
                $"release_date.lte={Today.AddMonths(12):yyyy-MM-dd}",
                StringComparison.Ordinal
            )
        );
    }

    [TestMethod]
    public async Task TmdbClient_VerifiesCertificationCountryAndType_AndChoosesEarliestAllowedRelease()
    {
        using FakeHandler handler = new(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            return path switch
            {
                "/3/genre/movie/list" => Json("""{"genres":[]}"""),
                "/3/discover/movie" => Json(
                    Discover(
                        1,
                        Candidate(1, "Good", Array.Empty<int>()),
                        Candidate(2, "Pg13", Array.Empty<int>()),
                        Candidate(3, "Unrated", Array.Empty<int>()),
                        Candidate(4, "Foreign", Array.Empty<int>()),
                        Candidate(5, "Streaming", Array.Empty<int>())
                    )
                ),
                "/3/movie/1/release_dates" => Json(
                    """{"results":[{"iso_3166_1":"US","release_dates":[{"certification":"PG","release_date":"2026-08-25T00:00:00Z","type":3},{"certification":"G","release_date":"2026-08-22T00:00:00Z","type":2}]}]}"""
                ),
                "/3/movie/2/release_dates" => Json(UsRelease(Today, "PG-13", 3)),
                "/3/movie/3/release_dates" => Json(UsRelease(Today, "", 3)),
                "/3/movie/4/release_dates" => Json(UsRelease(Today, "G", 3, "CA")),
                "/3/movie/5/release_dates" => Json(UsRelease(Today, "G", 1)),
                _ => NotFound(),
            };
        });
        TmdbMovieCatalogClient client = CreateClient(handler);

        CatalogFetchResult result = await client.FetchAsync(Today);

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(1, result.Movies.Count);
        Movie movie = result.Movies[0];
        Assert.AreEqual(1, movie.Id);
        Assert.AreEqual("G", movie.Certification?.Code);
        Assert.AreEqual(Today.AddDays(1), movie.UsTheatricalReleaseDate);
        Assert.AreEqual(
            TheatricalRelease.LimitedTheatricalType,
            movie.UsTheatricalReleases[0].ReleaseType
        );
    }

    [TestMethod]
    public async Task TmdbClient_DeduplicatesAndMapsGenresAndProductionFieldsDeterministically()
    {
        using FakeHandler handler = new(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            return path switch
            {
                "/3/genre/movie/list" => Json(
                    """{"genres":[{"id":1,"name":"Action"},{"id":2,"name":"Animation"}]}"""
                ),
                "/3/discover/movie" => Json(
                    request.RequestUri.Query.Contains("page=1", StringComparison.Ordinal)
                        ? Discover(
                            2,
                            Candidate(2, "Zulu", new[] { 2, 1 }),
                            Candidate(1, "Alpha", new[] { 1 }, "Overview", "/poster.jpg")
                        )
                        : Discover(2, Candidate(1, "Duplicate", new[] { 1 }))
                ),
                "/3/movie/1/release_dates" => Json(UsRelease(Today.AddDays(1), "G", 3)),
                "/3/movie/2/release_dates" => Json(UsRelease(Today.AddDays(2), "PG", 3)),
                _ => NotFound(),
            };
        });
        TmdbMovieCatalogClient client = CreateClient(handler);

        CatalogFetchResult result = await client.FetchAsync(Today);

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(2, result.Movies.Count);
        Assert.AreEqual(1, result.Movies[0].Id);
        Assert.AreEqual("Overview", result.Movies[0].Overview);
        Assert.AreEqual("/poster.jpg", result.Movies[0].PosterPath);
        Assert.AreEqual(
            "https://image.tmdb.org/t/p/w500/poster.jpg",
            result.Movies[0].PosterUri?.AbsoluteUri
        );
        CollectionAssert.AreEqual(new[] { 1 }, result.Movies[0].GenreIds.ToArray());
        Assert.AreEqual("Action", result.Movies[0].Genres[0].Name);
        Assert.AreEqual(
            1,
            handler.Requests.Count(request =>
                request.RequestUri!.AbsolutePath == "/3/movie/1/release_dates"
            )
        );
    }

    [TestMethod]
    public async Task TmdbClient_ReleaseVerificationHonorsConfiguredConcurrencyCap()
    {
        const int concurrencyCap = 2;
        using BoundedVerificationHandler handler = new(concurrencyCap);
        GoodMoviesInfrastructureOptions options = Options();
        options.MaxConcurrentRequests = concurrencyCap;
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        Task<CatalogFetchResult> operation = client.FetchAsync(Today);
        await handler.ReachedConcurrencyCap.Task.WaitAsync(TimeSpan.FromSeconds(5));
        handler.ReleaseRequests.TrySetResult();
        CatalogFetchResult result = await operation;

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(4, handler.ReleaseRequestCount);
        Assert.AreEqual(concurrencyCap, handler.MaximumActiveRequests);
        Assert.IsTrue(handler.MaximumActiveRequests > 1);
    }

    [TestMethod]
    public async Task TmdbClient_FailedPageOrVerification_ReturnsFailureWithoutPartialMovies()
    {
        using FakeHandler pageFailureHandler = new(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/3/genre/movie/list")
            {
                return Json("""{"genres":[]}""");
            }

            if (request.RequestUri.AbsolutePath == "/3/discover/movie")
            {
                return request.RequestUri.Query.Contains("page=1", StringComparison.Ordinal)
                    ? Json(Discover(2, Candidate(1, "Good")))
                    : ServerError();
            }

            return NotFound();
        });
        CatalogFetchResult pageFailure = await CreateClient(pageFailureHandler).FetchAsync(Today);
        Assert.AreEqual(CatalogFetchStatus.Failed, pageFailure.Status);
        Assert.AreEqual(0, pageFailure.Movies.Count);

        using FakeHandler verificationFailureHandler = new(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/3/genre/movie/list")
            {
                return Json("""{"genres":[]}""");
            }

            if (request.RequestUri.AbsolutePath == "/3/discover/movie")
            {
                return Json(Discover(1, Candidate(1, "Good")));
            }

            return ServerError();
        });
        CatalogFetchResult verificationFailure = await CreateClient(verificationFailureHandler)
            .FetchAsync(Today);
        Assert.AreEqual(CatalogFetchStatus.Failed, verificationFailure.Status);
        Assert.AreEqual(0, verificationFailure.Movies.Count);
    }

    [TestMethod]
    public async Task TmdbClient_PageCapClampsAnUnboundedResponseInsteadOfFailing()
    {
        using FakeHandler handler = new(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/3/genre/movie/list")
            {
                return Json("""{"genres":[]}""");
            }

            if (request.RequestUri.AbsolutePath == "/3/movie/1/release_dates")
            {
                return Json(UsRelease(Today, "G", 3));
            }

            return Json(Discover(21, Candidate(1, "Good")));
        });
        GoodMoviesInfrastructureOptions options = Options();
        options.MaxPages = 20;
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        CatalogFetchResult result = await client.FetchAsync(Today);

        // Twenty pages for each of the two discover passes, and no failure.
        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(1, result.Movies.Count);
        Assert.AreEqual(
            40,
            handler.Requests.Count(request =>
                request.RequestUri!.AbsolutePath == "/3/discover/movie"
            )
        );
    }

    [TestMethod]
    public async Task TmdbClient_KeepsNotYetRatedFamilyMovies_AndDropsNotYetRatedGrownUpMovies()
    {
        // 1 is animation and has no US certification yet, which is normal for a
        // release that is still months out. 2 has no certification either but is
        // not a family title, so it must not reach the catalog.
        using FakeHandler handler = new(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            return path switch
            {
                "/3/genre/movie/list" => Json("""{"genres":[{"id":16,"name":"Animation"}]}"""),
                "/3/discover/movie" => Json(
                    Discover(
                        1,
                        Candidate(1, "Not Rated Yet Family", new[] { 16 }),
                        Candidate(2, "Not Rated Yet Thriller", new[] { 53 }),
                        Candidate(
                            3,
                            "Not Rated Yet Foreign Cartoon",
                            new[] { 16 },
                            originalLanguage: "fr"
                        )
                    )
                ),
                "/3/movie/1/release_dates" => Json(UsRelease(Today.AddDays(200), "", 3)),
                "/3/movie/2/release_dates" => Json(UsRelease(Today.AddDays(200), "", 3)),
                "/3/movie/3/release_dates" => Json(UsRelease(Today.AddDays(200), "", 3)),
                _ => NotFound(),
            };
        });
        GoodMoviesInfrastructureOptions options = Options();
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        CatalogFetchResult result = await client.FetchAsync(Today);

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(1, result.Movies.Count);
        Movie kept = result.Movies.Single();
        Assert.AreEqual(1, kept.Id);
        Assert.IsTrue(kept.IsNotYetRated);
        Assert.IsTrue(kept.IsFamilyAudience);
        Assert.IsNull(kept.Certification);
    }

    [TestMethod]
    public async Task TmdbClient_DropsFamilyMovieThatCarriesADisallowedCertification()
    {
        // An unrated entry must never be used to sneak past a PG-13 rating that
        // the same movie already carries.
        using FakeHandler handler = new(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            return path switch
            {
                "/3/genre/movie/list" => Json("""{"genres":[{"id":16,"name":"Animation"}]}"""),
                "/3/discover/movie" => Json(
                    Discover(1, Candidate(1, "Teen Cartoon", new[] { 16 }))
                ),
                "/3/movie/1/release_dates" => Json(
                    $$"""{"results":[{"iso_3166_1":"US","release_dates":[{"certification":"","release_date":"{{Today.AddDays(200):yyyy-MM-dd}}T00:00:00Z","type":3},{"certification":"PG-13","release_date":"{{Today.AddDays(201):yyyy-MM-dd}}T00:00:00Z","type":3}]}]}"""
                ),
                _ => NotFound(),
            };
        });
        GoodMoviesInfrastructureOptions options = Options();
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        CatalogFetchResult result = await client.FetchAsync(Today);

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(0, result.Movies.Count);
    }

    [TestMethod]
    public async Task TmdbClient_DropsNotYetRatedFamilyMoviesBelowThePopularityFloor()
    {
        // Festival shorts share the animation genre with real releases, so an
        // unrated title also has to be prominent enough to be a real release.
        using FakeHandler handler = new(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            return path switch
            {
                "/3/genre/movie/list" => Json("""{"genres":[{"id":16,"name":"Animation"}]}"""),
                "/3/discover/movie" => Json(
                    Discover(
                        1,
                        Candidate(1, "Tiny Festival Short", new[] { 16 }, popularity: 0.4),
                        Candidate(2, "Real Release", new[] { 16 }, popularity: 7.9)
                    )
                ),
                "/3/movie/1/release_dates" => Json(UsRelease(Today.AddDays(200), "", 3)),
                "/3/movie/2/release_dates" => Json(UsRelease(Today.AddDays(200), "", 3)),
                _ => NotFound(),
            };
        });
        GoodMoviesInfrastructureOptions options = Options();
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        CatalogFetchResult result = await client.FetchAsync(Today);

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(1, result.Movies.Count);
        Assert.AreEqual(2, result.Movies.Single().Id);
    }

    [TestMethod]
    public async Task TmdbClient_KeepsUnpopularMoviesThatCarryAnAllowedCertification()
    {
        // The popularity floor only exists to vouch for unrated movies. A small
        // title the MPAA already rated G stays in.
        using FakeHandler handler = new(request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            return path switch
            {
                "/3/genre/movie/list" => Json("""{"genres":[]}"""),
                "/3/discover/movie" => Json(
                    Discover(1, Candidate(1, "Small But Rated", popularity: 0.01))
                ),
                "/3/movie/1/release_dates" => Json(UsRelease(Today, "G", 3)),
                _ => NotFound(),
            };
        });
        GoodMoviesInfrastructureOptions options = Options();
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        CatalogFetchResult result = await client.FetchAsync(Today);

        Assert.IsTrue(result.Succeeded, result.Error?.ToString());
        Assert.AreEqual(1, result.Movies.Count);
        Assert.IsFalse(result.Movies.Single().IsNotYetRated);
    }

    [TestMethod]
    public async Task TmdbClient_MalformedJsonAndCancellationAreNotSuccessful()
    {
        using FakeHandler malformedHandler = new(request =>
            request.RequestUri!.AbsolutePath == "/3/genre/movie/list" ? Json("{") : NotFound()
        );
        CatalogFetchResult malformed = await CreateClient(malformedHandler).FetchAsync(Today);
        Assert.AreEqual(CatalogFetchStatus.Failed, malformed.Status);
        Assert.IsInstanceOfType(malformed.Error, typeof(CatalogRefreshException));

        using CancellationHandler cancellationHandler = new();
        TmdbMovieCatalogClient client = CreateClient(cancellationHandler);
        using CancellationTokenSource cancellation = new();
        Task<CatalogFetchResult> operation = client.FetchAsync(Today, cancellation.Token);
        await cancellationHandler.Started.Task;
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await operation);
    }

    [TestMethod]
    public async Task TmdbClient_TrailersAreLazy_AndUseCorePrecedence()
    {
        using FakeHandler handler = new(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/3/genre/movie/list")
            {
                return Json("""{"genres":[]}""");
            }

            if (request.RequestUri.AbsolutePath == "/3/discover/movie")
            {
                return Json(Discover(1, Candidate(1, "Good")));
            }

            if (request.RequestUri.AbsolutePath == "/3/movie/1/release_dates")
            {
                return Json(UsRelease(Today, "G", 3));
            }

            if (request.RequestUri.AbsolutePath == "/3/movie/1/videos")
            {
                return Json(
                    """{"results":[{"key":"Teas_123456","name":"Teaser","site":"YouTube","type":"Teaser","official":true,"iso_639_1":"en"},{"key":"Trai_123456","name":"Trailer","site":"YouTube","type":"Trailer","official":true,"iso_639_1":"en"}]}"""
                );
            }

            return NotFound();
        });
        TmdbMovieCatalogClient client = CreateClient(handler);
        CatalogFetchResult catalog = await client.FetchAsync(Today);
        Assert.IsTrue(catalog.Succeeded, catalog.Error?.ToString());
        Assert.AreEqual(
            0,
            handler.Requests.Count(request => request.RequestUri!.AbsolutePath.EndsWith("/videos"))
        );

        TrailerLookupResult trailer = await client.GetTrailerAsync(1);

        Assert.AreEqual(TrailerLookupStatus.Found, trailer.Status);
        Assert.AreEqual("Trai_123456", trailer.Trailer!.Key);
        Assert.AreEqual(
            1,
            handler.Requests.Count(request => request.RequestUri!.AbsolutePath.EndsWith("/videos"))
        );
    }

    [TestMethod]
    public void PosterUrlBuilder_ReturnsNullForUnsafePaths_AndUsesConfiguredSize()
    {
        PosterUrlBuilder builder = new(Options());

        Assert.IsNull(builder.Build(null));
        Assert.IsNull(builder.Build(" "));
        Assert.AreEqual(
            "https://image.tmdb.org/t/p/w500/abc.jpg",
            builder.Build("/abc.jpg")?.AbsoluteUri
        );
        Assert.IsNull(builder.Build("http://image.tmdb.org/t/p/w500/abc.jpg"));
        Assert.IsNull(builder.Build("https://example.com/abc.jpg"));
        Assert.IsNull(builder.Build("../abc.jpg"));
    }

    [TestMethod]
    public async Task CatalogService_FailedRefreshKeepsGoodCacheAndDoesNotReconcileFavorites()
    {
        using TestDirectory directory = new();
        DateOnly today = Today;
        Movie cachedMovie = Movie(1, "Cached", today);
        JsonMovieCatalogCache cache = CreateCache(
            Path.Combine(directory.Path, "catalog.json"),
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero))
        );
        CatalogCacheWriteResult write = await cache.WriteAsync(
            new MovieCatalogSnapshot(new[] { cachedMovie }, today),
            new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero)
        );
        Assert.IsTrue(write.Succeeded);
        StubCatalogProvider provider = new(
            CatalogFetchResult.Failure(new InvalidOperationException("offline"))
        );
        MovieCatalogService service = new(provider, cache, new FixedClock(today));

        CatalogResult result = await service.GetCatalogAsync(forceRefresh: true);

        Assert.AreEqual(CatalogResultStatus.RefreshFailed, result.Status);
        Assert.IsTrue(result.UsedCache);
        Assert.AreEqual(1, result.Movies.Count);
        CatalogCacheReadResult after = await cache.ReadAsync(today);
        Assert.AreEqual(1, after.Movies.Count);
    }

    [TestMethod]
    public async Task CatalogService_SerializesRefreshesAndNewestWriteWins()
    {
        using TestDirectory directory = new();
        JsonMovieCatalogCache cache = CreateCache(Path.Combine(directory.Path, "catalog.json"));
        TaskCompletionSource<CatalogFetchResult> first = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        SequencedCatalogProvider provider = new(
            first.Task,
            Task.FromResult(
                CatalogFetchResult.Success(
                    new[] { Movie(2, "New", Today.AddDays(2)) },
                    DateTimeOffset.UtcNow.AddMinutes(1)
                )
            )
        );
        MovieCatalogService service = new(provider, cache, new FixedClock(Today));

        Task<CatalogResult> firstRefresh = service.RefreshAsync();
        await provider.FirstRequestStarted.Task;
        Task<CatalogResult> secondRefresh = service.RefreshAsync();
        await Task.Delay(50);
        Assert.AreEqual(1, provider.CallCount);

        first.SetResult(
            CatalogFetchResult.Success(
                new[] { Movie(1, "Old", Today.AddDays(1)) },
                DateTimeOffset.UtcNow
            )
        );
        await firstRefresh;
        await secondRefresh;

        Assert.AreEqual(2, provider.CallCount);
        CatalogCacheReadResult cached = await cache.ReadAsync(Today);
        Assert.AreEqual("New", cached.Movies.Single().Title);
    }

    [TestMethod]
    public async Task CatalogCache_RoundTripsProductionFields_AndReportsStalenessAndCorruption()
    {
        using TestDirectory directory = new();
        DateOnly today = Today;
        MutableTimeProvider time = new(new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero));
        JsonMovieCatalogCache cache = CreateCache(
            Path.Combine(directory.Path, "catalog.json"),
            time
        );
        Movie original = new(
            8,
            "Animation",
            "PG",
            new[] { new TheatricalRelease(today, "US", 3) },
            new[] { new MovieGenre(16, "Animation") },
            "A synopsis",
            "/poster.jpg",
            new Uri("https://image.tmdb.org/t/p/w500/poster.jpg"),
            "en",
            new[] { 16 },
            isInTheaters: true
        );

        await cache.WriteAsync(new MovieCatalogSnapshot(new[] { original }, today), time.UtcNow);
        string json = await File.ReadAllTextAsync(Path.Combine(directory.Path, "catalog.json"));
        Assert.IsNotNull(GoodMoviesJsonContext.Default.CatalogCacheDocument);
        Assert.IsTrue(json.Contains("refreshedAt", StringComparison.Ordinal));

        time.UtcNow = time.UtcNow.AddHours(6);
        CatalogCacheReadResult stale = await cache.ReadAsync(today);
        Assert.AreEqual(CatalogCacheStatus.Available, stale.Status);
        Assert.IsTrue(stale.IsStale);
        Assert.AreEqual(1, stale.Movies.Count);
        Assert.AreEqual("A synopsis", stale.Movies[0].Overview);
        Assert.AreEqual("Animation", stale.Movies[0].Genres[0].Name);
        Assert.IsTrue(stale.Movies[0].IsInTheaters);
        Assert.IsTrue(stale.Movies[0].HasBeenInTheaters);
        CatalogCacheReadResult muchLater = await cache.ReadAsync(today.AddMonths(3));
        Assert.AreEqual(1, muchLater.Movies.Count);
        Assert.IsTrue(muchLater.Movies[0].IsInTheaters);

        await File.WriteAllTextAsync(Path.Combine(directory.Path, "catalog.json"), "{");
        CatalogCacheReadResult corrupt = await cache.ReadAsync(today);
        Assert.AreEqual(CatalogCacheStatus.Corrupted, corrupt.Status);
        Assert.IsFalse(corrupt.HasUsableCache);
    }

    [TestMethod]
    public async Task CatalogCache_FiltersUnsafeAndExpiredNeverTheatricalMovies_ButKeepsPlayingMovies()
    {
        using TestDirectory directory = new();
        string path = Path.Combine(directory.Path, "catalog.json");
        await File.WriteAllTextAsync(
            path,
            $$"""
            {
              "refreshedAt":"2026-08-21T12:00:00+00:00",
              "movies":[
                {"id":1,"title":"Safe","certification":"G","releases":[{"releaseDate":"2026-08-21","countryCode":"US","releaseType":3}]},
                {"id":2,"title":"Expired","certification":"PG","releases":[{"releaseDate":"2026-08-07","countryCode":"US","releaseType":3}]},
                {"id":3,"title":"Unsafe","certification":"PG-13","releases":[{"releaseDate":"2026-08-21","countryCode":"US","releaseType":3}]},
                {"id":4,"title":"Foreign","certification":"G","releases":[{"releaseDate":"2026-08-21","countryCode":"CA","releaseType":3}]},
                {"id":5,"title":"Playing","certification":"G","isInTheaters":true,"releases":[{"releaseDate":"2026-05-01","countryCode":"US","releaseType":3}]}
              ]
            }
            """
        );
        JsonMovieCatalogCache cache = CreateCache(path);

        CatalogCacheReadResult result = await cache.ReadAsync(Today);

        Assert.AreEqual(CatalogCacheStatus.Available, result.Status);
        CollectionAssert.AreEquivalent(
            new[] { 1, 5 },
            result.Movies.Select(movie => movie.Id).ToArray()
        );
        Assert.IsTrue(result.Movies.Single(movie => movie.Id == 5).HasBeenInTheaters);
    }

    [TestMethod]
    public async Task CatalogCache_AtomicWriteFailurePreservesPriorGoodDocument()
    {
        using TestDirectory directory = new();
        string path = Path.Combine(directory.Path, "catalog.json");
        JsonMovieCatalogCache goodCache = CreateCache(path);
        await goodCache.WriteAsync(
            new MovieCatalogSnapshot(new[] { Movie(1, "Good", Today) }, Today),
            DateTimeOffset.UtcNow
        );
        string prior = await File.ReadAllTextAsync(path);
        JsonMovieCatalogCache failingCache = CreateCache(
            path,
            atomicFileWriter: new ThrowingAtomicFileWriter()
        );

        CatalogCacheWriteResult failed = await failingCache.WriteAsync(
            new MovieCatalogSnapshot(new[] { Movie(2, "New", Today) }, Today),
            DateTimeOffset.UtcNow
        );

        Assert.AreEqual(CatalogCacheWriteStatus.Failed, failed.Status);
        Assert.AreEqual(prior, await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task FavoritesStore_TogglesPersistsAndReconciles_WithoutAgeBasedPruning()
    {
        using TestDirectory directory = new();
        string path = Path.Combine(directory.Path, "favorites.json");
        JsonFavoritesStore store = CreateFavoritesStore(path);
        FavoriteEntry retained = new(1, Today.AddDays(-13));
        FavoriteEntry older = new(2, Today.AddDays(-90));

        FavoriteToggleResult added = await store.ToggleAsync(retained, Today);
        Assert.AreEqual(FavoriteToggleStatus.Added, added.Status, added.Error?.ToString());
        Assert.AreEqual(FavoriteToggleStatus.Added, (await store.ToggleAsync(older, Today)).Status);
        FavoritesResult listed = await store.GetAsync(Today);
        Assert.AreEqual(2, listed.Entries.Count);
        Assert.AreEqual(1, listed.Entries[0].MovieId);
        Assert.IsTrue(
            (await File.ReadAllTextAsync(path)).Contains(
                "usTheatricalReleaseDate",
                StringComparison.Ordinal
            )
        );

        FavoriteToggleResult removed = await store.ToggleAsync(retained, Today);
        Assert.AreEqual(FavoriteToggleStatus.Removed, removed.Status);
        await store.ToggleAsync(retained, Today);
        Movie matchingWithNewDate = Movie(1, "Matching", Today.AddDays(-12));
        Movie absent = Movie(3, "Absent", Today);
        FavoritesResult reconciled = await store.ReconcileAsync(
            new[] { matchingWithNewDate, absent },
            Today
        );
        Assert.AreEqual(1, reconciled.Entries.Count);
        Assert.AreEqual(
            matchingWithNewDate.UsTheatricalReleaseDate,
            reconciled.Entries[0].UsTheatricalReleaseDate
        );
    }

    [TestMethod]
    public async Task FavoritesStore_GuardsConcurrentToggles()
    {
        using TestDirectory directory = new();
        JsonFavoritesStore store = CreateFavoritesStore(
            Path.Combine(directory.Path, "favorites.json")
        );
        Task<FavoriteToggleResult>[] operations = Enumerable
            .Range(1, 12)
            .Select(id => store.ToggleAsync(new FavoriteEntry(id, Today), Today))
            .ToArray();

        await Task.WhenAll(operations);
        FavoritesResult result = await store.GetAsync(Today);

        Assert.AreEqual(12, result.Entries.Count);
    }

    [TestMethod]
    public async Task MissingToken_ReturnsConfigurationResultWithoutHttpCalls()
    {
        using FakeHandler handler = new(_ => throw new AssertFailedException("HTTP was called"));
        GoodMoviesInfrastructureOptions options = Options();
        options.Token = " ";
        using HttpClient httpClient = CreateHttpClient(handler, options);
        TmdbMovieCatalogClient client = new(httpClient, options);

        CatalogFetchResult catalog = await client.FetchAsync(Today);
        TrailerLookupResult trailer = await client.GetTrailerAsync(1);

        Assert.AreEqual(CatalogFetchStatus.MissingConfiguration, catalog.Status);
        Assert.AreEqual(TrailerLookupStatus.MissingConfiguration, trailer.Status);
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    [DataRow("secret-token with-space")]
    [DataRow("secret,token")]
    public async Task InvalidToken_ReturnsConfigurationResultWithoutHttpCallsOrTokenLogs(
        string token
    )
    {
        using FakeHandler handler = new(_ => throw new AssertFailedException("HTTP was called"));
        GoodMoviesInfrastructureOptions options = Options();
        options.Token = token;
        TmdbBearerTokenHandler bearerHandler = new(options) { InnerHandler = handler };
        using HttpClient httpClient = new(bearerHandler) { BaseAddress = options.ApiBaseAddress };
        ListLogger<TmdbMovieCatalogClient> logger = new();
        TmdbMovieCatalogClient client = new(httpClient, options, logger: logger);

        CatalogFetchResult catalog = await client.FetchAsync(Today);
        TrailerLookupResult trailer = await client.GetTrailerAsync(1);

        Assert.AreEqual(CatalogFetchStatus.MissingConfiguration, catalog.Status);
        Assert.AreEqual(TrailerLookupStatus.MissingConfiguration, trailer.Status);
        Assert.AreEqual(0, handler.Requests.Count);
        Assert.IsFalse(catalog.Error!.ToString()!.Contains(token, StringComparison.Ordinal));
        Assert.IsFalse(trailer.Error!.ToString()!.Contains(token, StringComparison.Ordinal));
        Assert.IsTrue(
            logger.Messages.All(message => !message.Contains(token, StringComparison.Ordinal))
        );
    }

    [TestMethod]
    public void Options_RejectsInvalidUnratedPopularity()
    {
        foreach (double value in new[] { -1, double.NaN, double.PositiveInfinity })
        {
            GoodMoviesInfrastructureOptions options = Options();
            options.MinimumUnratedPopularity = value;

            Assert.Throws<GoodMoviesConfigurationException>(() => options.Validate());
        }
    }

    [TestMethod]
    public void Options_RejectsHttpApiAddress()
    {
        GoodMoviesInfrastructureOptions options = Options();
        options.ApiBaseAddress = new Uri("http://tmdb.test/");
        using FakeHandler handler = new(_ => throw new AssertFailedException("HTTP was called"));
        using HttpClient httpClient = CreateHttpClient(handler, options);

        Assert.Throws<GoodMoviesConfigurationException>(() => options.Validate());
        Assert.Throws<GoodMoviesConfigurationException>(() =>
            new TmdbMovieCatalogClient(httpClient, options)
        );
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public void Options_RejectsHttpImageAddress()
    {
        GoodMoviesInfrastructureOptions options = Options();
        options.ImageBaseAddress = new Uri("http://images.test/t/p/w500");
        using FakeHandler handler = new(_ => throw new AssertFailedException("HTTP was called"));
        using HttpClient httpClient = CreateHttpClient(handler, options);

        Assert.Throws<GoodMoviesConfigurationException>(() => options.Validate());
        Assert.Throws<GoodMoviesConfigurationException>(() =>
            new TmdbMovieCatalogClient(httpClient, options)
        );
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public void TmdbClient_RejectsHttpClientBaseAddress()
    {
        GoodMoviesInfrastructureOptions options = Options();
        using FakeHandler handler = new(_ => throw new AssertFailedException("HTTP was called"));
        TmdbBearerTokenHandler bearerHandler = new(options) { InnerHandler = handler };
        using HttpClient httpClient = new(bearerHandler)
        {
            BaseAddress = new Uri("http://override.test/"),
        };

        Assert.Throws<GoodMoviesConfigurationException>(() =>
            new TmdbMovieCatalogClient(httpClient, options)
        );
        Assert.AreEqual(0, handler.Requests.Count);
    }

    [TestMethod]
    public void ServiceRegistration_ResolvesCoreFacingInfrastructureContracts()
    {
        using TestDirectory directory = new();
        ServiceCollection services = new();
        services.AddGoodMoviesInfrastructure(
            new GoodMoviesInfrastructureOptions { StorageDirectory = directory.Path }
        );

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<IMovieCatalogService>());
        Assert.IsNotNull(provider.GetRequiredService<IMovieCatalogCache>());
        Assert.IsNotNull(provider.GetRequiredService<IFavoritesStore>());
        Assert.IsNotNull(provider.GetRequiredService<IMovieTrailerLookup>());
    }

    [TestMethod]
    public async Task ServiceRegistration_RetriesTransientTrailerFailures()
    {
        using TestDirectory directory = new();
        int attempts = 0;
        using FakeHandler handler = new(_ =>
            Interlocked.Increment(ref attempts) switch
            {
                1 => throw new HttpRequestException("offline"),
                2 => ServerError(),
                _ => Json(
                    """{"results":[{"key":"Rtry_123456","name":"Trailer","site":"YouTube","type":"Trailer","official":true,"iso_639_1":"en"}]}"""
                ),
            }
        );
        GoodMoviesInfrastructureOptions options = Options();
        options.StorageDirectory = directory.Path;
        ServiceCollection services = new();
        services.AddGoodMoviesInfrastructure(options);
        services
            .AddHttpClient<TmdbMovieCatalogClient>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using ServiceProvider provider = services.BuildServiceProvider();

        TrailerLookupResult result = await provider
            .GetRequiredService<IMovieTrailerLookup>()
            .GetTrailerAsync(1);

        Assert.AreEqual(TrailerLookupStatus.Found, result.Status);
        Assert.AreEqual("Rtry_123456", result.Trailer!.Key);
        Assert.AreEqual(3, attempts);
    }

    private static JsonMovieCatalogCache CreateCache(
        string filePath,
        TimeProvider? timeProvider = null,
        IAtomicFileWriter? atomicFileWriter = null
    )
    {
        GoodMoviesInfrastructureOptions options = new()
        {
            CatalogCacheFileName = Path.GetFileName(filePath),
        };
        return new JsonMovieCatalogCache(
            new FileSystemPathProvider(Path.GetDirectoryName(filePath)!),
            options,
            timeProvider,
            atomicFileWriter
        );
    }

    private static JsonFavoritesStore CreateFavoritesStore(string filePath) =>
        new(
            new FileSystemPathProvider(Path.GetDirectoryName(filePath)!),
            new GoodMoviesInfrastructureOptions { FavoritesFileName = Path.GetFileName(filePath) }
        );

    private static TmdbMovieCatalogClient CreateClient(HttpMessageHandler handler)
    {
        GoodMoviesInfrastructureOptions options = Options();
        return new TmdbMovieCatalogClient(CreateHttpClient(handler, options), options);
    }

    private static HttpClient CreateHttpClient(
        HttpMessageHandler handler,
        GoodMoviesInfrastructureOptions options
    )
    {
        TmdbBearerTokenHandler authentication = new(options) { InnerHandler = handler };
        return new HttpClient(authentication) { BaseAddress = options.ApiBaseAddress };
    }

    private static GoodMoviesInfrastructureOptions Options() =>
        new() { Token = "secret", ApiBaseAddress = new Uri("https://tmdb.test/") };

    private static TmdbDiscoverMovie Candidate(
        int id,
        string title,
        int[]? genreIds = null,
        string? overview = null,
        string? posterPath = null,
        double popularity = 10,
        string originalLanguage = "en"
    ) =>
        new()
        {
            Id = id,
            Title = title,
            GenreIds = genreIds?.ToList() ?? new List<int>(),
            Overview = overview,
            PosterPath = posterPath,
            OriginalLanguage = originalLanguage,
            Popularity = popularity,
        };

    private static string Discover(int totalPages, params TmdbDiscoverMovie[] movies) =>
        System.Text.Json.JsonSerializer.Serialize(
            new TmdbDiscoverResponse { TotalPages = totalPages, Results = movies.ToList() },
            GoodMoviesJsonContext.Default.TmdbDiscoverResponse
        );

    private static string NowPlaying(int page, int totalPages, params TmdbDiscoverMovie[] movies) =>
        System.Text.Json.JsonSerializer.Serialize(
            new TmdbDiscoverResponse
            {
                Page = page,
                TotalPages = totalPages,
                Results = movies.ToList(),
            },
            GoodMoviesJsonContext.Default.TmdbDiscoverResponse
        );

    private static string UsRelease(
        DateOnly date,
        string certification,
        int type,
        string country = "US"
    ) =>
        $$"""{"results":[{"iso_3166_1":"{{country}}","release_dates":[{"certification":"{{certification}}","release_date":"{{date:yyyy-MM-dd}}T00:00:00Z","type":{{type}}}]}]}""";

    private static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage NotFound() => new(HttpStatusCode.NotFound);

    private static HttpResponseMessage ServerError() => new(HttpStatusCode.InternalServerError);

    private static Movie Movie(int id, string title, DateOnly releaseDate) =>
        new(
            id,
            title,
            "G",
            new[] { new TheatricalRelease(releaseDate, "US", TheatricalRelease.TheatricalType) }
        );

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _nowPlayingResponder;

        public FakeHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responder,
            Func<HttpRequestMessage, HttpResponseMessage>? nowPlayingResponder = null
        )
        {
            _responder = responder;
            _nowPlayingResponder = nowPlayingResponder ?? (_ => Json(NowPlaying(1, 0)));
        }

        public ConcurrentBag<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return Task.FromResult(
                request.RequestUri!.AbsolutePath == "/3/movie/now_playing"
                    ? _nowPlayingResponder(request)
                    : _responder(request)
            );
        }
    }

    private sealed class CancellationHandler : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class BoundedVerificationHandler : HttpMessageHandler
    {
        private readonly int _concurrencyCap;
        private int _activeRequests;
        private int _maximumActiveRequests;
        private int _releaseRequestCount;

        public BoundedVerificationHandler(int concurrencyCap)
        {
            _concurrencyCap = concurrencyCap;
        }

        public TaskCompletionSource ReachedConcurrencyCap { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseRequests { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ReleaseRequestCount => Volatile.Read(ref _releaseRequestCount);

        public int MaximumActiveRequests => Volatile.Read(ref _maximumActiveRequests);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            string path = request.RequestUri!.AbsolutePath;
            if (path == "/3/genre/movie/list")
            {
                return Json("""{"genres":[]}""");
            }

            if (path == "/3/movie/now_playing")
            {
                return Json(NowPlaying(1, 0));
            }

            if (path == "/3/discover/movie")
            {
                return Json(
                    Discover(
                        1,
                        Candidate(1, "One"),
                        Candidate(2, "Two"),
                        Candidate(3, "Three"),
                        Candidate(4, "Four")
                    )
                );
            }

            if (!path.EndsWith("/release_dates", StringComparison.Ordinal))
            {
                return NotFound();
            }

            int activeRequests = Interlocked.Increment(ref _activeRequests);
            UpdateMaximumActiveRequests(activeRequests);
            Interlocked.Increment(ref _releaseRequestCount);
            if (activeRequests >= _concurrencyCap)
            {
                ReachedConcurrencyCap.TrySetResult();
            }

            try
            {
                await ReleaseRequests.Task.WaitAsync(cancellationToken);
                string id = path.Split('/')[3];
                return Json(UsRelease(Today.AddDays(int.Parse(id)), "G", 3));
            }
            finally
            {
                Interlocked.Decrement(ref _activeRequests);
            }
        }

        private void UpdateMaximumActiveRequests(int activeRequests)
        {
            while (true)
            {
                int maximum = Volatile.Read(ref _maximumActiveRequests);
                if (activeRequests <= maximum)
                {
                    return;
                }

                if (
                    Interlocked.CompareExchange(ref _maximumActiveRequests, activeRequests, maximum)
                    == maximum
                )
                {
                    return;
                }
            }
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateOnly today)
        {
            Today = today;
        }

        public DateOnly Today { get; }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        public MutableTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class ThrowingAtomicFileWriter : IAtomicFileWriter
    {
        public Task WriteAsync(
            string targetPath,
            Func<Stream, Task> writeContentAsync,
            CancellationToken cancellationToken = default
        ) => throw new IOException("simulated write failure");
    }

    private sealed class StubCatalogProvider : IMovieCatalogProvider
    {
        private readonly CatalogFetchResult _result;

        public StubCatalogProvider(CatalogFetchResult result)
        {
            _result = result;
        }

        public Task<CatalogFetchResult> FetchAsync(
            DateOnly today,
            CancellationToken cancellationToken = default,
            IReadOnlyList<Movie>? trackedMovies = null
        ) => Task.FromResult(_result);
    }

    private sealed class SequencedCatalogProvider : IMovieCatalogProvider
    {
        private readonly Queue<Task<CatalogFetchResult>> _results;
        private readonly Lock _sync = new();
        private int _callCount;

        public SequencedCatalogProvider(params Task<CatalogFetchResult>[] results)
        {
            _results = new Queue<Task<CatalogFetchResult>>(results);
        }

        public TaskCompletionSource FirstRequestStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount => Volatile.Read(ref _callCount);

        public Task<CatalogFetchResult> FetchAsync(
            DateOnly today,
            CancellationToken cancellationToken = default,
            IReadOnlyList<Movie>? trackedMovies = null
        )
        {
            Task<CatalogFetchResult> result;
            lock (_sync)
            {
                result = _results.Dequeue();
                if (Interlocked.Increment(ref _callCount) == 1)
                {
                    FirstRequestStarted.TrySetResult();
                }
            }

            return result.WaitAsync(cancellationToken);
        }
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "GoodMoviesTestData",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
