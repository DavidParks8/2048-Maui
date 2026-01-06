using TwentyFortyEight.Core;

namespace TwentyFortyEight.ViewModels.Services;

public static class RulesetScopedStatisticsMigration
{
    private const string StatisticsKeyPrefix = "GameStatistics.";

    public const string RulesetMigrationKey = "Migration.RulesetScopedStatsV1Complete";

    public static void MigrateSizeScopedStatsToRulesetScoped(IPreferencesService preferencesService)
    {
        if (preferencesService.GetBool(RulesetMigrationKey, false))
        {
            return;
        }

        for (int size = 1; size <= GameConfig.MaxReasonableBoardSize; size++)
        {
            var oldKey = $"{StatisticsKeyPrefix}{size}";
            if (!preferencesService.ContainsKey(oldKey))
            {
                continue;
            }

            var defaultRulesetId = new GameConfig { Size = size, WinTile = 2048 }.RulesetId;
            var newKey = $"{StatisticsKeyPrefix}{defaultRulesetId}";

            if (!preferencesService.ContainsKey(newKey))
            {
                var json = preferencesService.GetString(oldKey, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    preferencesService.SetString(newKey, json);
                }
            }

            preferencesService.Remove(oldKey);
        }

        preferencesService.SetBool(RulesetMigrationKey, true);
    }
}
