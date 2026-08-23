namespace GoodMovies.Core;

public enum ReleaseStatus
{
    Future,
    Today,
    InTheatersNow,
    Expired,
}

public readonly record struct ReleaseStatusInfo(ReleaseStatus Status, int Sleeps);
