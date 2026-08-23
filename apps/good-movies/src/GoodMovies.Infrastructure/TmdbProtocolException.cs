namespace GoodMovies.Infrastructure;

internal sealed class TmdbProtocolException : Exception
{
    public TmdbProtocolException(string message)
        : base(message) { }

    public TmdbProtocolException(string message, Exception innerException)
        : base(message, innerException) { }
}
