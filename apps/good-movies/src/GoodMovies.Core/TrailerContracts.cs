namespace GoodMovies.Core;

public enum TrailerLookupStatus
{
    Found,
    NotFound,
    MissingConfiguration,
    Failed,
    None = NotFound,
}

public sealed class TrailerLookupResult
{
    public TrailerLookupResult(
        TrailerLookupStatus status,
        int movieId,
        MovieTrailer? trailer = null,
        Exception? error = null
    )
    {
        Status = status;
        MovieId = movieId;
        Trailer = trailer;
        Error = error;
    }

    public TrailerLookupStatus Status { get; }

    public TrailerLookupStatus State => Status;

    public int MovieId { get; }

    public MovieTrailer? Trailer { get; }

    public MovieTrailer? Value => Trailer;

    public Exception? Error { get; }

    public bool Succeeded => Status is TrailerLookupStatus.Found or TrailerLookupStatus.NotFound;

    public static TrailerLookupResult Found(int movieId, MovieTrailer trailer) =>
        new(TrailerLookupStatus.Found, movieId, trailer);

    public static TrailerLookupResult NotFound(int movieId) =>
        new(TrailerLookupStatus.NotFound, movieId);

    public static TrailerLookupResult MissingConfiguration(int movieId, Exception error) =>
        new(TrailerLookupStatus.MissingConfiguration, movieId, error: error);

    public static TrailerLookupResult Failure(int movieId, Exception error) =>
        new(TrailerLookupStatus.Failed, movieId, error: error);
}

public interface IMovieTrailerLookup
{
    Task<TrailerLookupResult> GetTrailerAsync(
        int movieId,
        CancellationToken cancellationToken = default
    );
}

public interface ITrailerLookup : IMovieTrailerLookup { }

/// <summary>
/// Alias used by ViewModels when treating trailer lookup as a service.
/// </summary>
public interface IMovieTrailerService : IMovieTrailerLookup { }
