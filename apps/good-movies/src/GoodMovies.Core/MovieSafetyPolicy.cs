namespace GoodMovies.Core;

/// <summary>
/// Accepts only child-safe certifications and US limited/theatrical releases.
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

    public bool IsSafe(Movie? movie) =>
        movie is not null
        && IsAllowedCertification(movie.Certification)
        && movie.UsTheatricalReleases.Any(IsUsTheatricalRelease);

    public bool Accepts(Movie? movie) => IsSafe(movie);

    public bool IsSafeCertification(string? certification) => IsAllowedCertification(certification);

    public bool IsSafeRelease(TheatricalRelease? release) => IsUsTheatricalRelease(release);
}
