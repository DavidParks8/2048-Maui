using Microsoft.VisualStudio.TestTools.UnitTesting;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class RulesetScopedStatisticsMigrationTests
{
    private sealed class InMemoryPreferencesService : IPreferencesService
    {
        private readonly Dictionary<string, object> _store = new();

        public string GetString(string key, string defaultValue = "") =>
            _store.TryGetValue(key, out var value) && value is string s ? s : defaultValue;

        public void SetString(string key, string value) => _store[key] = value;

        public int GetInt(string key, int defaultValue = 0) =>
            _store.TryGetValue(key, out var value) && value is int i ? i : defaultValue;

        public void SetInt(string key, int value) => _store[key] = value;

        public bool GetBool(string key, bool defaultValue = false) =>
            _store.TryGetValue(key, out var value) && value is bool b ? b : defaultValue;

        public void SetBool(string key, bool value) => _store[key] = value;

        public double GetDouble(string key, double defaultValue = 0.0) =>
            _store.TryGetValue(key, out var value) && value is double d ? d : defaultValue;

        public void SetDouble(string key, double value) => _store[key] = value;

        public bool ContainsKey(string key) => _store.ContainsKey(key);

        public void Remove(string key) => _store.Remove(key);
    }

    [TestMethod]
    public void MigrateSizeScopedStatsToRulesetScoped_MovesKeysAndSetsSentinel()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();

        var size = 5;
        var oldKey = $"GameStatistics.{size}";
        var config = new GameConfig { Size = size, WinTile = 2048 };
        var newKey = $"GameStatistics.{config.RulesetId}";

        preferences.SetString(oldKey, "{\"gamesPlayed\":1}");

        // Act
        RulesetScopedStatisticsMigration.MigrateSizeScopedStatsToRulesetScoped(preferences);

        // Assert
        Assert.IsFalse(preferences.ContainsKey(oldKey));
        Assert.IsTrue(preferences.ContainsKey(newKey));
        Assert.IsTrue(preferences.GetBool(RulesetScopedStatisticsMigration.RulesetMigrationKey));
    }
}
