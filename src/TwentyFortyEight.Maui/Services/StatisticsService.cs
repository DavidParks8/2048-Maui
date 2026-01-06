using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Serialization;
using TwentyFortyEight.ViewModels.Messages;
using TwentyFortyEight.ViewModels.Services;

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
    private readonly IPreferencesService _preferencesService;
    private readonly Lock _sync = new();

    private string _rulesetId = new GameConfig { Size = LegacyBoardSize, WinTile = 2048 }.RulesetId;
    private int _boardSize = LegacyBoardSize;
    private bool _migrationChecked;

    public StatisticsService(
        ILogger<StatisticsService> logger,
        IPreferencesService preferencesService
    )
    {
        _logger = logger;
        _preferencesService = preferencesService;

        WeakReferenceMessenger.Default.Register<RulesetChangedMessage>(
            this,
            static (object recipient, RulesetChangedMessage message) =>
            {
                if (recipient is StatisticsService service)
                {
                    service.SetRuleset(message.NewRulesetId, message.NewBoardSize);
                }
            }
        );
    }

    public void SetRuleset(string rulesetId, int boardSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesetId, nameof(rulesetId));
        ArgumentOutOfRangeException.ThrowIfNegative(boardSize, nameof(boardSize));

        EnsureMigrated();

        if (
            _boardSize == boardSize
            && string.Equals(_rulesetId, rulesetId, StringComparison.Ordinal)
        )
        {
            return;
        }

        _rulesetId = rulesetId;
        _boardSize = boardSize;
        Reload();
    }

    private static string GetStatisticsKey(string rulesetId) => $"{StatisticsKeyPrefix}{rulesetId}";

    private static string GetLegacySizeScopedStatisticsKey(int boardSize) =>
        $"{StatisticsKeyPrefix}{boardSize}";

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
                var sizeScopedMigrated =
                    _preferencesService.GetBool(MigrationKey, false)
                    || _preferencesService.GetBool(LegacyMigrationKey, false);

                if (!sizeScopedMigrated)
                {
                    // Migrate legacy stats -> size 4 slot (only if new slot is empty)
                    if (
                        _preferencesService.ContainsKey(LegacyStatisticsKey)
                        && !_preferencesService.ContainsKey(
                            GetLegacySizeScopedStatisticsKey(LegacyBoardSize)
                        )
                    )
                    {
                        var legacyJson = _preferencesService.GetString(
                            LegacyStatisticsKey,
                            string.Empty
                        );
                        if (!string.IsNullOrEmpty(legacyJson))
                        {
                            _preferencesService.SetString(
                                GetLegacySizeScopedStatisticsKey(LegacyBoardSize),
                                legacyJson
                            );
                        }
                    }

                    // Delete legacy key after migration
                    if (_preferencesService.ContainsKey(LegacyStatisticsKey))
                    {
                        _preferencesService.Remove(LegacyStatisticsKey);
                    }

                    _preferencesService.SetBool(MigrationKey, true);
                    _preferencesService.SetBool(LegacyMigrationKey, true);
                }

                RulesetScopedStatisticsMigration.MigrateSizeScopedStatsToRulesetScoped(
                    _preferencesService
                );
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
            _preferencesService.SetString(GetStatisticsKey(_rulesetId), json);
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
            var json = _preferencesService.GetString(GetStatisticsKey(_rulesetId), string.Empty);
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
