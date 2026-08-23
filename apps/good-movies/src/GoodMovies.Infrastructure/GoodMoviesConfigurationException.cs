namespace GoodMovies.Infrastructure;

internal sealed class GoodMoviesConfigurationException : InvalidOperationException
{
    public GoodMoviesConfigurationException(string message)
        : base(message) { }
}
