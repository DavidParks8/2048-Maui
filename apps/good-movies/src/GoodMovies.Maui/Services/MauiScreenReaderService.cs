using Microsoft.Extensions.Logging;

namespace GoodMovies.Maui.Services;

public interface IScreenReaderService
{
    void Announce(string message);
}

public sealed partial class MauiScreenReaderService(ILogger<MauiScreenReaderService> logger)
    : IScreenReaderService
{
    public void Announce(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                SemanticScreenReader.Announce(message);
            }
            catch (Exception exception)
            {
                LogAnnouncementFailed(logger, exception);
            }
        });
    }

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Warning,
        Message = "An accessibility announcement failed."
    )]
    private static partial void LogAnnouncementFailed(ILogger logger, Exception exception);
}
