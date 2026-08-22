namespace GoodMovies.Core;

/// <summary>
/// The shared catalog window: thirteen retained days in the past and twelve
/// calendar months in the future, both inclusive.
/// </summary>
public sealed class ReleaseWindowPolicy
{
    public const int RetainedPastDays = 13;
    public const int FutureMonths = 12;

    public static ReleaseWindowPolicy Default { get; } = new();

    public DateOnly EarliestVisibleDate(DateOnly today) => today.AddDays(-RetainedPastDays);

    public DateOnly LatestVisibleDate(DateOnly today) => today.AddMonths(FutureMonths);

    public bool IsVisible(DateOnly releaseDate, DateOnly today) =>
        releaseDate >= EarliestVisibleDate(today) && releaseDate <= LatestVisibleDate(today);

    public bool IsVisible(DateOnly releaseDate, IClock clock) =>
        IsVisible(releaseDate, clock.Today);

    public bool IsVisible(TheatricalRelease? release, DateOnly today) =>
        release is not null && release.IsUsTheatrical && IsVisible(release.ReleaseDate, today);

    public bool IsVisible(TheatricalRelease? release, IClock clock) =>
        IsVisible(release, clock.Today);

    public bool IsVisible(Movie? movie, DateOnly today) =>
        movie is not null && movie.UsTheatricalReleases.Any(release => IsVisible(release, today));

    public bool IsVisible(Movie? movie, IClock clock) => IsVisible(movie, clock.Today);

    public bool IsVisible(FavoriteEntry favorite, DateOnly today) =>
        IsVisible(favorite.UsTheatricalReleaseDate, today);

    public bool IsVisible(FavoriteEntry favorite, IClock clock) => IsVisible(favorite, clock.Today);

    public bool IsFavoriteVisible(FavoriteEntry favorite, DateOnly today) =>
        IsVisible(favorite, today);

    public bool IsExpired(DateOnly releaseDate, DateOnly today) =>
        releaseDate < EarliestVisibleDate(today);

    public bool IsExpired(TheatricalRelease? release, DateOnly today) =>
        release is null || !release.IsUsTheatrical || IsExpired(release.ReleaseDate, today);

    public bool IsExpired(FavoriteEntry favorite, DateOnly today) =>
        IsExpired(favorite.UsTheatricalReleaseDate, today);

    public bool IsFuture(DateOnly releaseDate, DateOnly today) => releaseDate > today;

    public int SleepsUntil(DateOnly releaseDate, DateOnly today) =>
        ReleaseStatusHelpers.GetSleeps(releaseDate, today);

    public ReleaseStatus GetStatus(DateOnly releaseDate, DateOnly today) =>
        ReleaseStatusHelpers.GetStatus(releaseDate, today);

    public ReleaseStatusInfo GetStatusInfo(DateOnly releaseDate, DateOnly today) =>
        ReleaseStatusHelpers.GetStatusInfo(releaseDate, today);

    public ReleaseStatusInfo GetStatusInfo(DateOnly releaseDate, IClock clock) =>
        GetStatusInfo(releaseDate, clock.Today);

    public IReadOnlyList<Movie> FilterVisibleMovies(
        IEnumerable<Movie> movies,
        DateOnly today,
        MovieSafetyPolicy? movieSafetyPolicy = null
    )
    {
        MovieSafetyPolicy safetyPolicy = movieSafetyPolicy ?? new MovieSafetyPolicy();
        Movie[] visibleMovies = (movies ?? Array.Empty<Movie>())
            .Where(movie =>
                movie is not null && safetyPolicy.IsSafe(movie) && IsVisible(movie, today)
            )
            .ToArray();

        return Array.AsReadOnly(visibleMovies);
    }

    public IReadOnlyList<Movie> FilterVisible(
        IEnumerable<Movie> movies,
        DateOnly today,
        MovieSafetyPolicy? movieSafetyPolicy = null
    ) => FilterVisibleMovies(movies, today, movieSafetyPolicy);

    public IReadOnlyList<FavoriteEntry> FilterVisibleFavorites(
        IEnumerable<FavoriteEntry> entries,
        DateOnly today
    ) =>
        Array.AsReadOnly(
            (entries ?? Array.Empty<FavoriteEntry>())
                .Where(entry => IsVisible(entry, today))
                .ToArray()
        );
}
