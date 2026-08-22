using System.Collections.ObjectModel;

namespace GoodMovies.Core;

/// <summary>
/// An immutable, policy-filtered catalog snapshot.
/// </summary>
public sealed record MovieCatalogSnapshot
{
    public static MovieCatalogSnapshot Empty { get; } = new(Array.Empty<Movie>(), asOfDate: null);

    public MovieCatalogSnapshot(
        IEnumerable<Movie> movies,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
    {
        AsOfDate = today;
        ReleaseWindowPolicy = releaseWindowPolicy ?? GoodMovies.Core.ReleaseWindowPolicy.Default;
        MovieSafetyPolicy = movieSafetyPolicy ?? new MovieSafetyPolicy();
        Movies = Sort(
            (movies ?? Array.Empty<Movie>())
                .Where(movie => movie is not null)
                .Where(movie => MovieSafetyPolicy.IsSafe(movie))
                .Where(movie => ReleaseWindowPolicy.IsVisible(movie, today)),
            today
        );
    }

    public MovieCatalogSnapshot(
        IEnumerable<Movie> movies,
        ReleaseWindowPolicy releaseWindowPolicy,
        MovieSafetyPolicy movieSafetyPolicy,
        DateOnly today
    )
        : this(movies, today, releaseWindowPolicy, movieSafetyPolicy) { }

    public DateOnly? AsOfDate { get; }

    public DateOnly? Today => AsOfDate;

    public ReleaseWindowPolicy? ReleaseWindowPolicy { get; }

    public MovieSafetyPolicy? MovieSafetyPolicy { get; }

    public IReadOnlyList<Movie> Movies { get; }

    public IReadOnlyList<Movie> Items => Movies;

    public IReadOnlyList<Movie> VisibleMovies => Movies;

    public static MovieCatalogSnapshot Create(
        IEnumerable<Movie> movies,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    ) => new(movies, today, releaseWindowPolicy, movieSafetyPolicy);

    public static IReadOnlyList<Movie> FilterVisible(
        IEnumerable<Movie> movies,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    ) => Create(movies, today, releaseWindowPolicy, movieSafetyPolicy).Movies;

    public static IReadOnlyList<Movie> FilterVisibleMovies(
        IEnumerable<Movie> movies,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    ) => FilterVisible(movies, today, releaseWindowPolicy, movieSafetyPolicy);

    public static IReadOnlyList<Movie> FilterForSearch(
        IEnumerable<Movie> movies,
        string? query,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    ) => Create(movies, today, releaseWindowPolicy, movieSafetyPolicy).Search(query).Movies;

    public static IReadOnlyList<Movie> FilterCachedMovies(
        IEnumerable<Movie> movies,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    ) => FilterVisible(movies, today, releaseWindowPolicy, movieSafetyPolicy);

    public static IReadOnlyList<Movie> FilterForCache(
        IEnumerable<Movie> movies,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    ) => FilterVisible(movies, today, releaseWindowPolicy, movieSafetyPolicy);

    public static IReadOnlyList<FavoriteEntry> FilterVisibleFavorites(
        IEnumerable<FavoriteEntry> entries,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null
    ) =>
        Array.AsReadOnly(
            (entries ?? Array.Empty<FavoriteEntry>())
                .Where(entry =>
                    (releaseWindowPolicy ?? ReleaseWindowPolicy.Default).IsVisible(entry, today)
                )
                .ToArray()
        );

    public MovieCatalogSnapshot Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return this;
        }

        string normalizedQuery = query.Trim();
        return new MovieCatalogSnapshot(
            Movies.Where(movie =>
                movie.Title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
            ),
            AsOfDate
        );
    }

    public MovieCatalogSnapshot Search(
        string? query,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null
    ) => Create(Movies, today, releaseWindowPolicy, movieSafetyPolicy).Search(query);

    public bool ContainsMovie(int movieId) => Movies.Any(movie => movie.Id == movieId);

    private MovieCatalogSnapshot(IEnumerable<Movie> movies, DateOnly? asOfDate)
    {
        AsOfDate = asOfDate;
        Movies = Sort(movies, asOfDate);
    }

    private static IReadOnlyList<Movie> Sort(IEnumerable<Movie> movies, DateOnly? today)
    {
        IOrderedEnumerable<Movie> sorted = today is DateOnly asOfDate
            ? movies.OrderBy(movie => GetVisibleReleaseDate(movie, asOfDate) ?? DateOnly.MaxValue)
            : movies.OrderBy(movie => movie.UsTheatricalReleaseDate ?? DateOnly.MaxValue);

        Movie[] items = sorted
            .ThenBy(movie => movie.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(movie => movie.Title, StringComparer.Ordinal)
            .ThenBy(movie => movie.Id)
            .ToArray();

        return new ReadOnlyCollection<Movie>(items);
    }

    private static DateOnly? GetVisibleReleaseDate(Movie movie, DateOnly today)
    {
        DateOnly[] releaseDates = movie
            .UsTheatricalReleases.Where(release => release is not null)
            .Select(release => release.ReleaseDate)
            .Where(releaseDate => ReleaseWindowPolicy.Default.IsVisible(releaseDate, today))
            .OrderBy(releaseDate => releaseDate)
            .ToArray();

        return releaseDates.Length == 0 ? null : releaseDates[0];
    }
}
