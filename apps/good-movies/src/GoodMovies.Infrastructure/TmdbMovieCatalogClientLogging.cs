using Microsoft.Extensions.Logging;

namespace GoodMovies.Infrastructure;

internal static partial class TmdbMovieCatalogClientLogging
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "TMDB catalog refresh failed."
    )]
    public static partial void CatalogRefreshFailed(this ILogger logger);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "TMDB catalog refresh is missing configuration."
    )]
    public static partial void CatalogRefreshMissingConfiguration(this ILogger logger);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "TMDB trailer lookup failed.")]
    public static partial void TrailerLookupFailed(this ILogger logger);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "TMDB trailer lookup is missing configuration."
    )]
    public static partial void TrailerLookupMissingConfiguration(this ILogger logger);
}
