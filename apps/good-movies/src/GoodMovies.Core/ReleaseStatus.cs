namespace GoodMovies.Core;

public enum ReleaseStatus
{
    Future,
    ComingSoon = Future,
    Today,
    InTheatersToday = Today,
    InTheatersNow,
    Past = InTheatersNow,
    Expired,
}

/// <summary>
/// Status data for a release. The date remains a DateOnly so the UI can format
/// it with the user's local culture, including a spelled-out date.
/// </summary>
public readonly record struct ReleaseStatusInfo
{
    public ReleaseStatusInfo(ReleaseStatus status, DateOnly releaseDate, int sleeps)
    {
        Status = status;
        ReleaseDate = releaseDate;
        Sleeps = Math.Max(0, sleeps);
    }

    public ReleaseStatus Status { get; }

    public ReleaseStatus Kind => Status;

    public DateOnly ReleaseDate { get; }

    public DateOnly LocalReleaseDate => ReleaseDate;

    public int Sleeps { get; }

    public int SleepCount => Sleeps;

    public int DaysUntilRelease => Sleeps;

    public bool IsFuture => Status == ReleaseStatus.Future;

    public bool IsToday => Status == ReleaseStatus.Today;

    public bool IsInTheatersNow => Status == ReleaseStatus.InTheatersNow;

    public bool IsExpired => Status == ReleaseStatus.Expired;
}

public static class ReleaseStatusHelpers
{
    public static ReleaseStatus GetStatus(DateOnly releaseDate, DateOnly today) =>
        releaseDate > today ? ReleaseStatus.Future
        : releaseDate == today ? ReleaseStatus.Today
        : releaseDate >= today.AddDays(-ReleaseWindowPolicy.RetainedPastDays)
            ? ReleaseStatus.InTheatersNow
        : ReleaseStatus.Expired;

    public static ReleaseStatusInfo GetStatusInfo(DateOnly releaseDate, DateOnly today) =>
        new(GetStatus(releaseDate, today), releaseDate, GetSleeps(releaseDate, today));

    public static int GetSleeps(DateOnly releaseDate, DateOnly today) =>
        Math.Max(0, releaseDate.DayNumber - today.DayNumber);

    public static ReleaseStatus GetReleaseStatus(this DateOnly releaseDate, DateOnly today) =>
        GetStatus(releaseDate, today);

    public static ReleaseStatusInfo GetReleaseStatusInfo(
        this DateOnly releaseDate,
        DateOnly today
    ) => GetStatusInfo(releaseDate, today);
}
