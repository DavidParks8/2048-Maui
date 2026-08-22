namespace GoodMovies.Core;

/// <summary>
/// Accepts only child-safe certifications and US limited/theatrical releases.
/// A release that the MPAA has not rated yet is accepted only when the provider
/// classifies it as animation or family, so that far-future kids' movies still
/// appear without ever letting an unrated grown-up movie through.
/// </summary>
public sealed class MovieSafetyPolicy
{
    public bool IsAllowedCertification(string? certification) =>
        MovieCertification.IsAllowed(certification);

    public bool IsAllowedCertification(MovieCertification? certification) =>
        MovieCertification.IsAllowed(certification);

    public bool IsUsTheatricalRelease(TheatricalRelease? release) =>
        release is not null
        && release.CountryCode == "US"
        && TheatricalRelease.IsAllowedTheatricalType(release.ReleaseType);

    /// <summary>
    /// True when a movie without a published certification is still safe to show
    /// because the provider classifies it as animation or family.
    /// </summary>
    public bool IsAllowedUnratedFamilyMovie(Movie? movie) =>
        movie is not null && movie.IsNotYetRated && movie.IsFamilyAudience;

    public bool IsAllowedRating(Movie? movie) =>
        movie is not null
        && (IsAllowedCertification(movie.Certification) || IsAllowedUnratedFamilyMovie(movie));

    public bool IsSafe(Movie? movie) =>
        movie is not null
        && IsAllowedRating(movie)
        && movie.UsTheatricalReleases.Any(IsUsTheatricalRelease);

    public bool Accepts(Movie? movie) => IsSafe(movie);

    public bool IsSafeCertification(string? certification) => IsAllowedCertification(certification);

    public bool IsSafeRelease(TheatricalRelease? release) => IsUsTheatricalRelease(release);
}
