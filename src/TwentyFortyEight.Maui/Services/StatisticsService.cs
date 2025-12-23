using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Serialization;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific statistics tracker with Preferences-based persistence.
/// </summary>
public sealed partial class StatisticsService(ILogger<StatisticsService> logger) : StatisticsTracker
{
    private const string StatisticsKey = "GameStatistics";

    /// <inheritdoc />
    protected override void Save(GameStatistics statistics)
    {
        try
        {
            var json = JsonSerializer.Serialize(
                statistics,
                StatisticsSerializationContext.Default.GameStatistics
            );
            Preferences.Set(StatisticsKey, json);
        }
        catch (Exception ex)
        {
            LogSaveError(logger, ex);
        }
    }

    /// <inheritdoc />
    protected override GameStatistics? Load()
    {
        try
        {
            var json = Preferences.Get(StatisticsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize(
                    json,
                    StatisticsSerializationContext.Default.GameStatistics
                );
            }
        }
        catch (Exception ex)
        {
            LogLoadError(logger, ex);
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to save game statistics")]
    private static partial void LogSaveError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load game statistics")]
    private static partial void LogLoadError(ILogger logger, Exception ex);
}
