namespace GoodMovies.Core;

public enum ReleaseStatus
{
    Future,
    Today,
    InTheatersNow,
    Released,
}

public readonly record struct ReleaseStatusInfo(ReleaseStatus Status, int Sleeps);
