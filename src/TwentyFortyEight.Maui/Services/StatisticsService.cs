using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Serialization;
using TwentyFortyEight.ViewModels.Messages;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific statistics tracker with Preferences-based persistence.
/// </summary>
public sealed partial class StatisticsService : StatisticsTracker
{
    private const int LegacyBoardSize = 4;
    private const string LegacyStatisticsKey = "GameStatistics";
    private const string StatisticsKeyPrefix = "GameStatistics.";
    private const string MigrationKey = "Migration.SizeScopedStatsV1Complete";
    private const string LegacyMigrationKey = "Migration.SizeScopedPersistenceV1Complete";

    private readonly ILogger<StatisticsService> _logger;
    private readonly Lock _sync = new();

    private int _boardSize = LegacyBoardSize;
    private bool _migrationChecked;

    public int BoardSize => _boardSize;

    public StatisticsService(ILogger<StatisticsService> logger)
    {
        _logger = logger;

        WeakReferenceMessenger.Default.Register<BoardSizeChangedMessage>(
            this,
            static (object recipient, BoardSizeChangedMessage message) =>
            {
                if (recipient is StatisticsService service)
                {
                    service.SetBoardSize(message.NewSize);
                }
            }
        );
    }

    public void SetBoardSize(int boardSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(boardSize, nameof(boardSize));

        EnsureMigrated();

        if (_boardSize == boardSize)
        {
            return;
        }

        _boardSize = boardSize;
        Reload();
    }

    private static string GetStatisticsKey(int boardSize) => $"{StatisticsKeyPrefix}{boardSize}";

    private void EnsureMigrated()
    {
        if (_migrationChecked)
        {
            return;
        }

        lock (_sync)
        {
            if (_migrationChecked)
            {
                return;
            }

            try
            {
                if (
                    Preferences.Get(MigrationKey, false)
                    || Preferences.Get(LegacyMigrationKey, false)
                )
                {
                    _migrationChecked = true;
                    return;
                }

                // Migrate legacy stats -> size 4 slot (only if new slot is empty)
                if (
                    Preferences.ContainsKey(LegacyStatisticsKey)
                    && !Preferences.ContainsKey(GetStatisticsKey(LegacyBoardSize))
                )
                {
                    var legacyJson = Preferences.Get(LegacyStatisticsKey, string.Empty);
                    if (!string.IsNullOrEmpty(legacyJson))
                    {
                        Preferences.Set(GetStatisticsKey(LegacyBoardSize), legacyJson);
                    }
                }

                // Delete legacy key after migration
                if (Preferences.ContainsKey(LegacyStatisticsKey))
                {
                    Preferences.Remove(LegacyStatisticsKey);
                }

                Preferences.Set(MigrationKey, true);
                Preferences.Set(LegacyMigrationKey, true);
            }
            catch (Exception ex)
            {
                LogMigrationError(_logger, ex);
            }
            finally
            {
                _migrationChecked = true;
            }
        }
    }

    /// <inheritdoc />
    protected override void Save(GameStatistics statistics)
    {
        try
        {
            EnsureMigrated();
            var json = JsonSerializer.Serialize(
                statistics,
                StatisticsSerializationContext.Default.GameStatistics
            );
            Preferences.Set(GetStatisticsKey(_boardSize), json);
        }
        catch (Exception ex)
        {
            LogSaveError(_logger, ex);
        }
    }

    /// <inheritdoc />
    protected override GameStatistics? Load()
    {
        try
        {
            EnsureMigrated();
            var json = Preferences.Get(GetStatisticsKey(_boardSize), string.Empty);
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
            LogLoadError(_logger, ex);
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to save game statistics")]
    private static partial void LogSaveError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load game statistics")]
    private static partial void LogLoadError(ILogger logger, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to migrate legacy game statistics key"
    )]
    private static partial void LogMigrationError(ILogger logger, Exception ex);
}
