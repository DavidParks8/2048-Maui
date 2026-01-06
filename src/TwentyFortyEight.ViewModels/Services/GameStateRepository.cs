using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Serialization;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Handles persistence of game state and best score.
/// </summary>
public sealed partial class GameStateRepository(
    IPreferencesService preferencesService,
    ILogger<GameStateRepository> logger
) : IGameStateRepository
{
    private const string SavedGameKeyPrefix = "SavedGame.";
    private const string BestScoreKeyPrefix = "BestScore.";

    private const string LegacySavedGameKey = "SavedGame";
    private const string LegacyBestScoreKey = "BestScore";

    private readonly Lock _sync = new();

    // Per-ruleset debouncing for best score saves
    private readonly Dictionary<
        string,
        CancellationTokenSource
    > _bestScoreSaveDebounceByRulesetId = [];
    private readonly Dictionary<string, Task> _bestScoreSaveTaskByRulesetId = [];
    private readonly Dictionary<string, int> _currentBestScoreByRulesetId = [];

    private static string GetSavedGameKey(string rulesetId) =>
        string.IsNullOrEmpty(rulesetId) ? LegacySavedGameKey : $"{SavedGameKeyPrefix}{rulesetId}";

    private static string GetBestScoreKey(string rulesetId) =>
        string.IsNullOrEmpty(rulesetId) ? LegacyBestScoreKey : $"{BestScoreKeyPrefix}{rulesetId}";

    private int EnsureBestScoreLoaded(GameConfig config)
    {
        var boardSize = config.Size;
        if (boardSize <= 0 || boardSize > GameConfig.MaxReasonableBoardSize)
        {
            return 0;
        }

        var rulesetId = config.RulesetId;

        lock (_sync)
        {
            if (_currentBestScoreByRulesetId.TryGetValue(rulesetId, out var best))
            {
                return best;
            }

            var loadedBest = preferencesService.GetInt(GetBestScoreKey(rulesetId), 0);
            _currentBestScoreByRulesetId[rulesetId] = loadedBest;
            return loadedBest;
        }
    }

    public GameState? LoadGameState(GameConfig config)
    {
        var boardSize = config.Size;
        try
        {
            var savedJson = preferencesService.GetString(
                GetSavedGameKey(config.RulesetId),
                string.Empty
            );
            if (!string.IsNullOrEmpty(savedJson))
            {
                var dto = JsonSerializer.Deserialize(
                    savedJson,
                    GameSerializationContext.Default.GameStateDto
                );

                var state = dto?.ToGameState();
                if (state != null && state.Size != boardSize)
                {
                    // Size mismatch: treat as no save for this slot.
                    return null;
                }

                return state;
            }
        }
        catch (Exception ex)
        {
            LogLoadGameStateFailed(logger, ex);
        }

        return null;
    }

    public void SaveGameState(GameConfig config, GameState state)
    {
        var boardSize = config.Size;
        try
        {
            if (state.Size != boardSize)
            {
                throw new InvalidOperationException(
                    $"Attempted to save a {state.Size}x{state.Size} state into the {boardSize}x{boardSize} slot."
                );
            }

            var dto = GameStateDto.FromGameState(state);
            var json = JsonSerializer.Serialize(dto, GameSerializationContext.Default.GameStateDto);
            preferencesService.SetString(GetSavedGameKey(config.RulesetId), json);
        }
        catch (Exception ex)
        {
            LogSaveGameStateFailed(logger, ex);
        }
    }

    public void ClearSavedGame(GameConfig config)
    {
        try
        {
            preferencesService.Remove(GetSavedGameKey(config.RulesetId));
        }
        catch (Exception ex)
        {
            LogSaveGameStateFailed(logger, ex);
        }
    }

    public int GetBestScore(GameConfig config) => EnsureBestScoreLoaded(config);

    public void UpdateBestScoreIfHigher(GameConfig config, int score)
    {
        var boardSize = config.Size;
        if (boardSize <= 0 || boardSize > GameConfig.MaxReasonableBoardSize)
        {
            return;
        }

        var rulesetId = config.RulesetId;

        var currentBest = EnsureBestScoreLoaded(config);
        if (score <= currentBest)
        {
            return;
        }

        Task saveTask;
        CancellationTokenSource cts;

        lock (_sync)
        {
            _currentBestScoreByRulesetId[rulesetId] = score;

            // Debounce saves to avoid hammering storage during rapid play
            if (_bestScoreSaveDebounceByRulesetId.TryGetValue(rulesetId, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            cts = new CancellationTokenSource();
            _bestScoreSaveDebounceByRulesetId[rulesetId] = cts;

            saveTask = DebouncedSaveBestScoreAsync(rulesetId, score, cts.Token);
            _bestScoreSaveTaskByRulesetId[rulesetId] = saveTask;
        }

        // Clean up task/CTS tracking when the save finishes.
        _ = saveTask.ContinueWith(
            static (t, state) =>
            {
                var tuple =
                    (Tuple<GameStateRepository, string, Task, CancellationTokenSource>)state!;
                tuple.Item1.CleanupAfterBestScoreSave(tuple.Item2, tuple.Item3, tuple.Item4);
            },
            Tuple.Create(this, rulesetId, saveTask, cts),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    public Task FlushAsync(GameConfig config)
    {
        var rulesetId = config.RulesetId;
        lock (_sync)
        {
            if (_bestScoreSaveTaskByRulesetId.TryGetValue(rulesetId, out var task))
            {
                return task;
            }
        }

        return Task.CompletedTask;
    }

    private void CleanupAfterBestScoreSave(
        string rulesetId,
        Task completedTask,
        CancellationTokenSource cts
    )
    {
        lock (_sync)
        {
            if (
                _bestScoreSaveTaskByRulesetId.TryGetValue(rulesetId, out var currentTask)
                && ReferenceEquals(currentTask, completedTask)
            )
            {
                _bestScoreSaveTaskByRulesetId.Remove(rulesetId);
            }

            if (
                _bestScoreSaveDebounceByRulesetId.TryGetValue(rulesetId, out var currentCts)
                && ReferenceEquals(currentCts, cts)
            )
            {
                _bestScoreSaveDebounceByRulesetId.Remove(rulesetId);
                cts.Dispose();
            }
        }
    }

    private async Task DebouncedSaveBestScoreAsync(
        string rulesetId,
        int value,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Task.Delay(500, cancellationToken);
            preferencesService.SetInt(GetBestScoreKey(rulesetId), value);
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled - expected
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to load game state")]
    private static partial void LogLoadGameStateFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to save game state")]
    private static partial void LogSaveGameStateFailed(ILogger logger, Exception ex);
}
