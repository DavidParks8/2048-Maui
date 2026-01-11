using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    public void LoadGame_WhenDefaultRuleset_UsesLegacyKeys()
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
        Assert.AreEqual(string.Empty, config4.RulesetId);

        // Act
        var loaded = repository.LoadGame(config4);
        var best = repository.GetBestScore(config4);

        // Assert
        Assert.IsNotNull(loaded);
        Assert.IsNotNull(loaded!.InitialState);
        Assert.AreEqual(4, loaded.InitialState!.Size);
        Assert.AreEqual(1234, best);

        Assert.IsTrue(preferences.ContainsKey("SavedGame"));
        Assert.IsTrue(preferences.ContainsKey("BestScore"));
    }

    [TestMethod]
    public void LoadGame_WhenSlotContainsMismatchedSize_ReturnsNull()
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
        var loaded = repository.LoadGame(config5);

        // Assert
        Assert.IsNull(loaded);
    }

    [TestMethod]
    public void UpdateBestScoreIfHigher_ClassicMode_UpdatesOnlyWhenHigher()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();
        var repository = new GameStateRepository(preferences, logger.Object);
        var config = new GameConfig { Size = 4, Mode = GameMode.Classic };

        // Act & Assert - first score sets the baseline
        repository.UpdateBestScoreIfHigher(config, 500);
        Assert.AreEqual(500, repository.GetBestScore(config));

        // Lower score should not update
        repository.UpdateBestScoreIfHigher(config, 300);
        Assert.AreEqual(500, repository.GetBestScore(config));

        // Higher score should update
        repository.UpdateBestScoreIfHigher(config, 800);
        Assert.AreEqual(800, repository.GetBestScore(config));
    }

    [TestMethod]
    public void UpdateBestScoreIfHigher_AdversarialMode_UpdatesOnlyWhenLower()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();
        var repository = new GameStateRepository(preferences, logger.Object);
        var config = new GameConfig { Size = 4, Mode = GameMode.Adversarial };

        // Act & Assert - first score sets the baseline
        repository.UpdateBestScoreIfHigher(config, 500);
        Assert.AreEqual(500, repository.GetBestScore(config));

        // Higher score (worse in Adversarial) should not update
        repository.UpdateBestScoreIfHigher(config, 800);
        Assert.AreEqual(500, repository.GetBestScore(config));

        // Lower score (better in Adversarial) should update
        repository.UpdateBestScoreIfHigher(config, 300);
        Assert.AreEqual(300, repository.GetBestScore(config));
    }

    [TestMethod]
    public void UpdateBestScoreIfHigher_AdversarialMode_ZeroScoreBeatsNonZero()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();
        var repository = new GameStateRepository(preferences, logger.Object);
        var config = new GameConfig { Size = 4, Mode = GameMode.Adversarial };

        // Set initial score
        repository.UpdateBestScoreIfHigher(config, 500);
        Assert.AreEqual(500, repository.GetBestScore(config));

        // Zero is the best possible score in Adversarial (AI made no merges)
        repository.UpdateBestScoreIfHigher(config, 0);
        Assert.AreEqual(0, repository.GetBestScore(config));
    }

    [TestMethod]
    public void UpdateBestScoreIfHigher_AdversarialMode_FirstScoreSetsBaseline()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();
        var repository = new GameStateRepository(preferences, logger.Object);
        var config = new GameConfig { Size = 4, Mode = GameMode.Adversarial };

        // Initial best score is 0 (unset)
        Assert.AreEqual(0, repository.GetBestScore(config));

        // First score should always be recorded (even if it's high)
        repository.UpdateBestScoreIfHigher(config, 1000);
        Assert.AreEqual(1000, repository.GetBestScore(config));
    }

    [TestMethod]
    public void UpdateBestScoreIfHigher_NegativeScore_IsIgnored()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();
        var repository = new GameStateRepository(preferences, logger.Object);
        var config = new GameConfig { Size = 4, Mode = GameMode.Classic };

        repository.UpdateBestScoreIfHigher(config, 500);

        // Negative scores should be ignored
        repository.UpdateBestScoreIfHigher(config, -100);
        Assert.AreEqual(500, repository.GetBestScore(config));
    }

    [TestMethod]
    public void UpdateBestScoreIfHigher_AdversarialMode_ZeroIsUnsetSentinel()
    {
        // Arrange
        var preferences = new InMemoryPreferencesService();
        var logger = new Mock<ILogger<GameStateRepository>>();
        var repository = new GameStateRepository(preferences, logger.Object);
        var config = new GameConfig { Size = 4, Mode = GameMode.Adversarial };

        // Initial best score is 0 (unset sentinel)
        Assert.AreEqual(0, repository.GetBestScore(config));

        // First score should set the baseline
        repository.UpdateBestScoreIfHigher(config, 500);
        Assert.AreEqual(500, repository.GetBestScore(config));

        // Zero is treated as unset, so if we somehow get to 0, it becomes "unset" again
        // This is a limitation: we can't distinguish "never played" from "perfect score of 0"
        // In practice, achieving exactly 0 is nearly impossible (game starts with 2 tiles)
        repository.UpdateBestScoreIfHigher(config, 0);
        Assert.AreEqual(0, repository.GetBestScore(config));

        // When 0 (unset), any score is accepted as the new baseline
        repository.UpdateBestScoreIfHigher(config, 50);
        Assert.AreEqual(50, repository.GetBestScore(config));
    }
}
