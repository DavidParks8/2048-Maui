using System.Net;

namespace GoodMovies.Infrastructure;

internal sealed class TmdbRequestException : HttpRequestException
{
    public TmdbRequestException(string message, HttpStatusCode statusCode)
        : base(message, null, statusCode) { }
}
