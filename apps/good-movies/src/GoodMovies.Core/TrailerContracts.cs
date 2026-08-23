namespace GoodMovies.Core;

public enum TrailerLookupStatus
{
    Found,
    NotFound,
    MissingConfiguration,
    Failed,
}

public sealed class TrailerLookupResult
{
    private TrailerLookupResult(
        TrailerLookupStatus status,
        MovieTrailer? trailer = null,
        Exception? error = null
    )
    {
        Status = status;
        Trailer = trailer;
        Error = error;
    }

    public TrailerLookupStatus Status { get; }

    public MovieTrailer? Trailer { get; }

    public Exception? Error { get; }

    public static TrailerLookupResult Found(MovieTrailer trailer) =>
        new(TrailerLookupStatus.Found, trailer);

    public static TrailerLookupResult NotFound() => new(TrailerLookupStatus.NotFound);

    public static TrailerLookupResult MissingConfiguration(Exception error) =>
        new(TrailerLookupStatus.MissingConfiguration, error: error);

    public static TrailerLookupResult Failure(Exception error) =>
        new(TrailerLookupStatus.Failed, error: error);
}

public interface IMovieTrailerLookup
{
    Task<TrailerLookupResult> GetTrailerAsync(
        int movieId,
        CancellationToken cancellationToken = default
    );
}
