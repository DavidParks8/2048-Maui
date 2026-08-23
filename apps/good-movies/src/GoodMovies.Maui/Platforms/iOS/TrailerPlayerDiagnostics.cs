using Microsoft.Extensions.Logging;

namespace GoodMovies.Maui.Platforms.iOS;

internal static partial class TrailerPlayerDiagnostics
{
    private static ILogger? _logger;

    internal static void Configure(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        Interlocked.Exchange(ref _logger, loggerFactory.CreateLogger("GoodMovies.TrailerPlayer"));
    }

    internal static void LogAudioCategoryFailure()
    {
        if (Volatile.Read(ref _logger) is { } logger)
        {
            AudioCategoryFailure(logger);
        }
    }

    internal static void LogAudioActivationFailure()
    {
        if (Volatile.Read(ref _logger) is { } logger)
        {
            AudioActivationFailure(logger);
        }
    }

    internal static void LogAudioDeactivationFailure()
    {
        if (Volatile.Read(ref _logger) is { } logger)
        {
            AudioDeactivationFailure(logger);
        }
    }

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "Trailer audio category configuration failed."
    )]
    private static partial void AudioCategoryFailure(ILogger logger);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Trailer audio session activation failed."
    )]
    private static partial void AudioActivationFailure(ILogger logger);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Warning,
        Message = "Trailer audio session deactivation failed."
    )]
    private static partial void AudioDeactivationFailure(ILogger logger);
}
