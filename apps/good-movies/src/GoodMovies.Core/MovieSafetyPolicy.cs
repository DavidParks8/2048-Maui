namespace GoodMovies.Core;

/// <summary>
/// Accepts G/PG movies and not-yet-rated family movies with a verified U.S.
/// limited or wide theatrical release.
/// </summary>
public static class MovieSafetyPolicy
{
    public static bool IsSafe(Movie? movie) =>
        movie is not null
        && (
            movie.Certification is not null
            || (
                movie.IsNotYetRated
                && movie.IsFamilyAudience
                && string.Equals(movie.OriginalLanguage, "en", StringComparison.OrdinalIgnoreCase)
            )
        )
        && movie.UsTheatricalReleases.Count > 0;
}
