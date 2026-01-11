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
    private const string StatisticsKeyPrefix = "GameStatistics";

    private readonly ILogger<StatisticsService> _logger;
    private readonly IPreferencesService _preferencesService;
    private readonly ISettingsService _settingsService;
    private readonly IMessenger _messenger;
    private readonly Lock _sync = new();

    private string _rulesetId;
    private int _boardSize;

    public StatisticsService(
        ILogger<StatisticsService> logger,
        IPreferencesService preferencesService,
        ISettingsService settingsService,
        IMessenger messenger
    )
    {
        _logger = logger;
        _preferencesService = preferencesService;
        _settingsService = settingsService;
        _messenger = messenger;

        var config = _settingsService.LastActiveGameConfig;

        _rulesetId = config.RulesetId;
        _boardSize = config.Size;

        _messenger.Register<RulesetChangedMessage>(
            this,
            static (recipient, message) =>
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
        ArgumentNullException.ThrowIfNull(rulesetId, nameof(rulesetId));
        ArgumentOutOfRangeException.ThrowIfNegative(boardSize, nameof(boardSize));

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

    private static string GetStatisticsKey(string rulesetId) =>
        string.IsNullOrEmpty(rulesetId)
            ? StatisticsKeyPrefix
            : $"{StatisticsKeyPrefix}.{rulesetId}";

    /// <inheritdoc />
    protected override void Save(GameStatistics statistics)
    {
        try
        {
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
}
