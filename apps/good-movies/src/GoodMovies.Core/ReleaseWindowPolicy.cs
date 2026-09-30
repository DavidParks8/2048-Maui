namespace GoodMovies.Core;

/// <summary>
/// Upcoming releases extend twelve calendar months ahead. Movies that played in
/// theaters expire when their run ends; never-theatrical releases expire after 14 days.
/// </summary>
public static class ReleaseWindowPolicy
{
    public const int FutureMonths = 12;
    public const int NonTheatricalRetentionDays = 14;

    public static DateOnly LatestVisibleDate(DateOnly today) => today.AddMonths(FutureMonths);

    public static bool IsVisible(DateOnly releaseDate, DateOnly today) =>
        releaseDate != default && releaseDate <= LatestVisibleDate(today);

    public static bool IsVisible(TheatricalRelease? release, DateOnly today) =>
        release is not null && release.IsUsCatalogRelease && IsVisible(release.ReleaseDate, today);

    public static bool IsVisible(Movie? movie, DateOnly today) =>
        movie is not null
        && GetVisibleRelease(movie, today) is { } release
        && IsRetained(release.ReleaseDate, today, movie.IsInTheaters, movie.HasBeenInTheaters);

    public static TheatricalRelease? GetVisibleRelease(Movie? movie, DateOnly today)
    {
        TheatricalRelease? pastRelease = null;
        if (movie is not null)
        {
            foreach (TheatricalRelease release in movie.UsReleases)
            {
                if (IsVisible(release.ReleaseDate, today))
                {
                    if (release.ReleaseDate < today)
                    {
                        pastRelease = release;
                    }
                    else
                    {
                        return movie.IsInTheaters && pastRelease is not null
                            ? pastRelease
                            : release;
                    }
                }
            }
        }

        return pastRelease;
    }

    public static bool IsVisible(FavoriteEntry favorite, DateOnly today) =>
        favorite.MovieId > 0
        && IsRetained(
            favorite.UsTheatricalReleaseDate,
            today,
            favorite.IsInTheaters,
            favorite.HasBeenInTheaters
        );

    private static bool IsRetained(
        DateOnly releaseDate,
        DateOnly today,
        bool isInTheaters,
        bool? hasBeenInTheaters
    ) =>
        IsVisible(releaseDate, today)
        && (
            isInTheaters
            || releaseDate > today
            || hasBeenInTheaters is null
            || (
                !hasBeenInTheaters.Value
                && today.DayNumber - releaseDate.DayNumber < NonTheatricalRetentionDays
            )
        );

    public static ReleaseStatusInfo GetStatusInfo(
        DateOnly releaseDate,
        DateOnly today,
        bool isInTheaters = false
    )
    {
        ReleaseStatus status = isInTheaters
            ? releaseDate == today
                ? ReleaseStatus.Today
                : ReleaseStatus.InTheatersNow
            : releaseDate > today
                ? ReleaseStatus.Future
                : ReleaseStatus.Released;
        return new ReleaseStatusInfo(
            status,
            status == ReleaseStatus.Future
                ? Math.Max(0, releaseDate.DayNumber - today.DayNumber)
                : 0
        );
    }
}
