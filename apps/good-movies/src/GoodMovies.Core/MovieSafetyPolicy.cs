namespace GoodMovies.Core;

/// <summary>
/// Accepts G/PG movies and not-yet-rated family movies with a verified U.S.
/// theatrical release or a verified digital/TV release for a redirected title.
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
        && movie.UsReleases.Count > 0;
}
