using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Serialization;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class GameStateRepositoryTests
{
    private sealed class InMemoryPreferencesService : IPreferencesService
    {
        private readonly Dictionary<string, object> _store = new();

        public string GetString(string key, string defaultValue = "")
        {
            return _store.TryGetValue(key, out var value) && value is string s ? s : defaultValue;
        }

        public void SetString(string key, string value)
        {
            _store[key] = value;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return _store.TryGetValue(key, out var value) && value is int i ? i : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            _store[key] = value;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return _store.TryGetValue(key, out var value) && value is bool b ? b : defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            _store[key] = value;
        }

        public double GetDouble(string key, double defaultValue = 0.0)
        {
            return _store.TryGetValue(key, out var value) && value is double d ? d : defaultValue;
        }

        public void SetDouble(string key, double value)
        {
            _store[key] = value;
        }

        public bool ContainsKey(string key) => _store.ContainsKey(key);

        public void Remove(string key) => _store.Remove(key);
    }

    [TestMethod]
    public void LoadGameState_WhenLegacyKeysExist_MigratesToDefaultRulesetIdAndDeletesLegacyAndSizeKeys()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();

        GameState legacyState = new(4);
        var dto = GameStateDto.FromGameState(legacyState);
        var json = JsonSerializer.Serialize(dto, GameSerializationContext.Default.GameStateDto);

        preferences.SetString("SavedGame", json);
        preferences.SetInt("BestScore", 1234);

        var repository = new GameStateRepository(preferences, logger.Object);

        var config4 = new GameConfig { Size = 4 };
        var defaultRulesetId4 = new GameConfig { Size = 4, WinTile = 2048 }.RulesetId;

        // Act
        var loaded = repository.LoadGameState(config4);
        var best = repository.GetBestScore(config4);

        // Assert
        Assert.IsNotNull(loaded);
        Assert.AreEqual(4, loaded!.Size);
        Assert.AreEqual(1234, best);

        Assert.IsFalse(preferences.ContainsKey("SavedGame"));
        Assert.IsFalse(preferences.ContainsKey("BestScore"));
        Assert.IsFalse(preferences.ContainsKey("SavedGame.4"));
        Assert.IsFalse(preferences.ContainsKey("BestScore.4"));
        Assert.IsTrue(preferences.ContainsKey($"SavedGame.{defaultRulesetId4}"));
        Assert.IsTrue(preferences.ContainsKey($"BestScore.{defaultRulesetId4}"));
        Assert.IsTrue(preferences.GetBool("Migration.SizeScopedSaveStateV1Complete"));
        Assert.IsTrue(preferences.GetBool("Migration.RulesetScopedPersistenceV1Complete"));
    }

    [TestMethod]
    public void LoadGameState_WhenSlotContainsMismatchedSize_ReturnsNull()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();

        GameState state4 = new(4);
        var dto = GameStateDto.FromGameState(state4);
        var json = JsonSerializer.Serialize(dto, GameSerializationContext.Default.GameStateDto);

        // Corrupt slot: store a 4x4 state under the 5x5 key
        var config5 = new GameConfig { Size = 5 };
        preferences.SetString($"SavedGame.{config5.RulesetId}", json);
        preferences.SetBool("Migration.RulesetScopedPersistenceV1Complete", true);

        var repository = new GameStateRepository(preferences, logger.Object);

        // Act
        var loaded = repository.LoadGameState(config5);

        // Assert
        Assert.IsNull(loaded);
    }

    [TestMethod]
    public void LoadGameState_WhenSizeScopedKeysExist_MigratesToDefaultRulesetId()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();

        var config5 = new GameConfig { Size = 5, WinTile = 2048 };

        GameState state5 = new(5);
        var dto = GameStateDto.FromGameState(state5);
        var json = JsonSerializer.Serialize(dto, GameSerializationContext.Default.GameStateDto);

        preferences.SetString("SavedGame.5", json);
        preferences.SetInt("BestScore.5", 555);
        preferences.SetBool("Migration.SizeScopedSaveStateV1Complete", true);

        var repository = new GameStateRepository(preferences, logger.Object);

        // Act
        var loaded = repository.LoadGameState(config5);
        var best = repository.GetBestScore(config5);

        // Assert
        Assert.IsNotNull(loaded);
        Assert.AreEqual(5, loaded!.Size);
        Assert.AreEqual(555, best);

        Assert.IsFalse(preferences.ContainsKey("SavedGame.5"));
        Assert.IsFalse(preferences.ContainsKey("BestScore.5"));
        Assert.IsTrue(preferences.ContainsKey($"SavedGame.{config5.RulesetId}"));
        Assert.IsTrue(preferences.ContainsKey($"BestScore.{config5.RulesetId}"));
        Assert.IsTrue(preferences.GetBool("Migration.RulesetScopedPersistenceV1Complete"));
    }
}
