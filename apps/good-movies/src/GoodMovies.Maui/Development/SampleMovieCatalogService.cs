#if DEBUG && GOOD_MOVIES_SAMPLE_DATA
using GoodMovies.Core;

namespace GoodMovies.Maui.Development;

/// <summary>
/// Synthetic catalog for local simulator and visual QA only. It is compiled
/// into Debug only when GoodMoviesUseSampleData=true is explicitly supplied.
/// </summary>
public sealed class SampleMovieCatalogService : IMovieCatalogService
{
    private readonly IClock _clock;
    private readonly IReadOnlyList<Movie> _movies;

    public SampleMovieCatalogService(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _movies = CreateMovies(clock.Today);
    }

    public Task<CatalogResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateResult(CatalogResultStatus.FreshCache, usedCache: true));
    }

    public Task<CatalogResult> GetCatalogAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            CreateResult(
                forceRefresh ? CatalogResultStatus.Refreshed : CatalogResultStatus.FreshCache,
                usedCache: !forceRefresh
            )
        );
    }

    public Task<CatalogResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateResult(CatalogResultStatus.Refreshed, usedCache: false));
    }

    private CatalogResult CreateResult(CatalogResultStatus status, bool usedCache)
    {
        MovieCatalogSnapshot snapshot = MovieCatalogSnapshot.Create(_movies, _clock.Today);
        return new CatalogResult(
            status,
            snapshot.Movies,
            lastSuccessfulRefresh: DateTimeOffset.UtcNow,
            cacheAge: TimeSpan.Zero,
            isStale: false,
            usedCache: usedCache,
            snapshot: snapshot,
            cacheStatus: CatalogCacheStatus.Available
        );
    }

    private static IReadOnlyList<Movie> CreateMovies(DateOnly today) =>
        new[]
        {
            CreateMovie(
                9001,
                "Moonlight Marsh",
                today.AddDays(-2),
                "G",
                "Adventure",
                "A friendly moon moth helps two campers find their way home.",
                "sample_poster_01.png"
            ),
            CreateMovie(
                9002,
                "Rocket Picnic",
                today,
                "PG",
                "Comedy",
                "A careful crew packs sandwiches for a very small trip to space.",
                "sample_poster_02.png"
            ),
            CreateMovie(
                9003,
                "The Friendly Dragon",
                today.AddDays(1),
                "G",
                "Fantasy",
                "A young dragon learns that sharing a warm cave makes a big difference.",
                "sample_poster_03.png"
            ),
            CreateMovie(
                9004,
                "Cloud Library",
                today.AddDays(3),
                "G",
                "Family",
                "A floating library opens its doors to every curious reader in town.",
                "sample_poster_04.png"
            ),
            CreateMovie(
                9005,
                "Bicycle Planet",
                today.AddDays(3),
                "PG",
                "Adventure",
                "Two friends pedal across a tiny planet to return a lost star.",
                "sample_poster_05.png"
            ),
            CreateMovie(
                9006,
                "Puddle Pirates",
                today.AddDays(7),
                "G",
                "Comedy",
                "A rainy-day crew sails a puddle and discovers a kind new neighbor.",
                "sample_poster_06.png"
            ),
            CreateMovie(
                9007,
                "Starlight Soup",
                today.AddDays(12),
                "G",
                "Family",
                "A grandparent and child cook a glowing recipe for their whole street.",
                "sample_poster_07.png"
            ),
            CreateMovie(
                9008,
                "The Map of Maybe",
                today.AddMonths(1),
                "PG",
                "Fantasy",
                "A paper map draws a new path whenever a brave question is asked.",
                "sample_poster_08.png"
            ),
            CreateMovie(
                9009,
                "Tiny Giants",
                today.AddMonths(2),
                "G",
                "Nature",
                "Small garden helpers show a child how much teamwork can do.",
                "sample_poster_09.png"
            ),
            CreateMovie(
                9010,
                "Lantern Day",
                today.AddMonths(3),
                "PG",
                "Family",
                "A neighborhood makes a gentle light festival after the sun goes down.",
                "sample_poster_10.png"
            ),
        };

    private static Movie CreateMovie(
        int id,
        string title,
        DateOnly releaseDate,
        string rating,
        string genre,
        string overview,
        string posterPath
    ) =>
        new(
            id,
            title,
            rating,
            new TheatricalRelease(releaseDate, "US", TheatricalRelease.TheatricalType),
            new[] { new MovieGenre(0, genre) },
            overview: overview,
            posterPath: posterPath
        );
}

/// <summary>
/// Sample visual QA intentionally exercises the no-trailer state and never
/// supplies a real or unrelated video URL.
/// </summary>
public sealed class SampleTrailerLookup : IMovieTrailerLookup, IMovieTrailerService, ITrailerLookup
{
    public Task<TrailerLookupResult> GetTrailerAsync(
        int movieId,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(TrailerLookupResult.NotFound(movieId));
    }
}
#endif
