namespace GoodMovies.Core;

/// <summary>
/// An immutable, policy-filtered catalog snapshot.
/// </summary>
public sealed class MovieCatalogSnapshot
{
    public MovieCatalogSnapshot(IEnumerable<Movie> movies, DateOnly today)
    {
        AsOfDate = today;
        Movies = Sort(
            (movies ?? Array.Empty<Movie>())
                .Where(movie => movie is not null)
                .Where(MovieSafetyPolicy.IsSafe)
                .Where(movie => ReleaseWindowPolicy.IsVisible(movie, today)),
            today
        );
    }

    public DateOnly AsOfDate { get; }

    public IReadOnlyList<Movie> Movies { get; }

    private static IReadOnlyList<Movie> Sort(IEnumerable<Movie> movies, DateOnly today)
    {
        IOrderedEnumerable<Movie> sorted = movies.OrderBy(movie =>
            ReleaseWindowPolicy.GetVisibleRelease(movie, today)?.ReleaseDate ?? DateOnly.MaxValue
        );

        return CollectionSnapshot.Create(
            sorted
                .ThenBy(movie => movie.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(movie => movie.Title, StringComparer.Ordinal)
                .ThenBy(movie => movie.Id)
        );
    }
}
