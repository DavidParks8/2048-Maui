namespace GoodMovies.Core;

/// <summary>
/// The shared catalog window: thirteen retained days in the past and twelve
/// calendar months in the future, both inclusive.
/// </summary>
public static class ReleaseWindowPolicy
{
    public const int RetainedPastDays = 13;
    public const int FutureMonths = 12;

    public static DateOnly EarliestVisibleDate(DateOnly today) => today.AddDays(-RetainedPastDays);

    public static DateOnly LatestVisibleDate(DateOnly today) => today.AddMonths(FutureMonths);

    public static bool IsVisible(DateOnly releaseDate, DateOnly today) =>
        releaseDate >= EarliestVisibleDate(today) && releaseDate <= LatestVisibleDate(today);

    public static bool IsVisible(TheatricalRelease? release, DateOnly today) =>
        release is not null && release.IsUsTheatrical && IsVisible(release.ReleaseDate, today);

    public static bool IsVisible(Movie? movie, DateOnly today) =>
        GetVisibleRelease(movie, today) is not null;

    public static TheatricalRelease? GetVisibleRelease(Movie? movie, DateOnly today)
    {
        if (movie is not null)
        {
            foreach (TheatricalRelease release in movie.UsTheatricalReleases)
            {
                if (IsVisible(release.ReleaseDate, today))
                {
                    return release;
                }
            }
        }

        return null;
    }

    public static bool IsVisible(FavoriteEntry favorite, DateOnly today) =>
        IsVisible(favorite.UsTheatricalReleaseDate, today);

    public static ReleaseStatusInfo GetStatusInfo(DateOnly releaseDate, DateOnly today)
    {
        ReleaseStatus status =
            releaseDate > today ? ReleaseStatus.Future
            : releaseDate == today ? ReleaseStatus.Today
            : releaseDate < EarliestVisibleDate(today) ? ReleaseStatus.Expired
            : ReleaseStatus.InTheatersNow;
        return new ReleaseStatusInfo(status, Math.Max(0, releaseDate.DayNumber - today.DayNumber));
    }
}
