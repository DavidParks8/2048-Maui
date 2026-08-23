namespace GoodMovies.Infrastructure;

internal sealed class CatalogRefreshException : InvalidOperationException
{
    public CatalogRefreshException(string message, Exception innerException)
        : base(message, innerException) { }
}
