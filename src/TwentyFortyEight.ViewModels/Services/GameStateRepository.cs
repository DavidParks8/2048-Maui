using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Serialization;
// Alias to avoid conflict with Apple's GameKit.GameSave namespace on iOS/Mac Catalyst.
using CoreGameSave = TwentyFortyEight.Core.GameSave;

namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Handles persistence of game state and best score.
/// </summary>
internal sealed partial class GameStateRepository(
    IPreferencesService preferencesService,
    ILogger<GameStateRepository> logger
) : IGameStateRepository
{
    private const string SavedGameKeyPrefix = "SavedGame.";
    private const string BestScoreKeyPrefix = "BestScore.";

    private const string LegacySavedGameKey = "SavedGame";
    private const string LegacyBestScoreKey = "BestScore";

    private readonly Lock _sync = new();

    // Per-ruleset debouncing for game state saves
    private readonly Dictionary<string, CancellationTokenSource> _gameSaveDebounceByRulesetId = [];
    private readonly Dictionary<string, Task> _gameSaveTaskByRulesetId = [];
    private readonly Dictionary<
        string,
        (GameConfig Config, CoreGameSave Save)
    > _pendingGameSaveByRulesetId = [];

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

    public CoreGameSave? LoadGame(GameConfig config)
    {
        var boardSize = config.Size;
        try
        {
            var savedJson = preferencesService.GetString(
                GetSavedGameKey(config.RulesetId),
                string.Empty
            );

            if (string.IsNullOrEmpty(savedJson))
            {
                return null;
            }

            // New format: full session save (undo history, initial state, cursor).
            var save = JsonSerializer.Deserialize(
                savedJson,
                GameSerializationContext.Default.GameSave
            );

            if (save?.InitialState is not null)
            {
                var initial = save.InitialState.ToGameState();
                if (initial.Size != boardSize)
                {
                    // Size mismatch: treat as no save for this slot.
                    return null;
                }

                save.CurrentMoveIndex = Math.Clamp(
                    save.CurrentMoveIndex,
                    0,
                    save.MoveHistory?.Length ?? 0
                );

                return save;
            }

            // Legacy format: state-only save (no undo history).
            var legacy = JsonSerializer.Deserialize(
                savedJson,
                GameSerializationContext.Default.GameStateDto
            );

            var state = legacy?.ToGameState();
            if (state != null && state.Size != boardSize)
            {
                // Size mismatch: treat as no save for this slot.
                return null;
            }

            return state is null
                ? null
                : new CoreGameSave
                {
                    InitialState = GameStateDto.FromGameState(state),
                    MoveHistory = Array.Empty<MoveRecord>(),
                    CurrentMoveIndex = 0,
                    VictoryEventRaised = false,
                };
        }
        catch (Exception ex)
        {
            LogLoadGameStateFailed(logger, ex);
        }

        return null;
    }

    public void SaveGame(GameConfig config, CoreGameSave save)
    {
        var boardSize = config.Size;
        if (boardSize <= 0 || boardSize > GameConfig.MaxReasonableBoardSize)
        {
            return;
        }

        var rulesetId = config.RulesetId;

        Task saveTask;
        CancellationTokenSource cts;

        lock (_sync)
        {
            _pendingGameSaveByRulesetId[rulesetId] = (config, save);

            if (_gameSaveDebounceByRulesetId.TryGetValue(rulesetId, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            cts = new CancellationTokenSource();
            _gameSaveDebounceByRulesetId[rulesetId] = cts;

            saveTask = DebouncedSaveGameAsync(rulesetId, cts.Token);
            _gameSaveTaskByRulesetId[rulesetId] = saveTask;
        }

        _ = saveTask.ContinueWith(
            static (t, state) =>
            {
                var tuple =
                    (Tuple<GameStateRepository, string, Task, CancellationTokenSource>)state!;
                tuple.Item1.CleanupAfterGameSave(tuple.Item2, tuple.Item3, tuple.Item4);
            },
            Tuple.Create(this, rulesetId, saveTask, cts),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    public void ClearSavedGame(GameConfig config)
    {
        try
        {
            var rulesetId = config.RulesetId;
            lock (_sync)
            {
                _pendingGameSaveByRulesetId.Remove(rulesetId);

                if (_gameSaveDebounceByRulesetId.TryGetValue(rulesetId, out var cts))
                {
                    _gameSaveDebounceByRulesetId.Remove(rulesetId);
                    cts.Cancel();
                    cts.Dispose();
                }

                _gameSaveTaskByRulesetId.Remove(rulesetId);
            }

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
        if (score < 0)
        {
            // Scores are expected to be non-negative.
            return;
        }

        var boardSize = config.Size;
        if (boardSize <= 0 || boardSize > GameConfig.MaxReasonableBoardSize)
        {
            return;
        }

        var rulesetId = config.RulesetId;

        var currentBest = EnsureBestScoreLoaded(config);
        if (config.Mode == GameMode.Adversarial && currentBest < 0)
        {
            // Defensive migration: older Adversarial builds used negative scores.
            // Treat as unset so the next valid score becomes the baseline.
            currentBest = 0;
        }

        var shouldUpdate = config.Mode switch
        {
            GameMode.Adversarial => currentBest == 0 || score < currentBest,
            _ => score > currentBest,
        };

        if (!shouldUpdate)
            return;

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
        Task? bestScoreTask = null;
        Task? gameSaveTask = null;
        Task? forcedGameSaveTask = null;

        lock (_sync)
        {
            if (_bestScoreSaveTaskByRulesetId.TryGetValue(rulesetId, out var task))
            {
                bestScoreTask = task;
            }

            if (_pendingGameSaveByRulesetId.TryGetValue(rulesetId, out var pending))
            {
                // Cancel any pending debounce and flush the latest save immediately.
                if (_gameSaveDebounceByRulesetId.TryGetValue(rulesetId, out var cts))
                {
                    _gameSaveDebounceByRulesetId.Remove(rulesetId);
                    cts.Cancel();
                    cts.Dispose();
                }

                forcedGameSaveTask = Task.Run(() => PersistGame(pending.Config, pending.Save));
                _gameSaveTaskByRulesetId[rulesetId] = forcedGameSaveTask;
            }
            else if (_gameSaveTaskByRulesetId.TryGetValue(rulesetId, out var saveTask))
            {
                gameSaveTask = saveTask;
            }
        }

        if (forcedGameSaveTask is not null)
        {
            gameSaveTask = forcedGameSaveTask;
        }

        if (bestScoreTask is null)
        {
            return gameSaveTask ?? Task.CompletedTask;
        }

        if (gameSaveTask is null)
        {
            return bestScoreTask;
        }

        return Task.WhenAll(bestScoreTask, gameSaveTask);
    }

    private void PersistGame(GameConfig config, CoreGameSave save)
    {
        try
        {
            if (save.InitialState is null)
            {
                return;
            }

            var boardSize = config.Size;
            if (boardSize <= 0 || boardSize > GameConfig.MaxReasonableBoardSize)
            {
                return;
            }

            var initial = save.InitialState.ToGameState();
            if (initial.Size != boardSize)
            {
                return;
            }

            save.CurrentMoveIndex = Math.Clamp(
                save.CurrentMoveIndex,
                0,
                save.MoveHistory?.Length ?? 0
            );

            var json = JsonSerializer.Serialize(save, GameSerializationContext.Default.GameSave);
            preferencesService.SetString(GetSavedGameKey(config.RulesetId), json);
        }
        catch (Exception ex)
        {
            LogSaveGameStateFailed(logger, ex);
        }
    }

    private void CleanupAfterGameSave(
        string rulesetId,
        Task completedTask,
        CancellationTokenSource cts
    )
    {
        lock (_sync)
        {
            if (
                _gameSaveTaskByRulesetId.TryGetValue(rulesetId, out var currentTask)
                && ReferenceEquals(currentTask, completedTask)
            )
            {
                _gameSaveTaskByRulesetId.Remove(rulesetId);
            }

            if (
                _gameSaveDebounceByRulesetId.TryGetValue(rulesetId, out var currentCts)
                && ReferenceEquals(currentCts, cts)
            )
            {
                _gameSaveDebounceByRulesetId.Remove(rulesetId);
                cts.Dispose();
            }
        }
    }

    private async Task DebouncedSaveGameAsync(string rulesetId, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);

            (GameConfig Config, CoreGameSave Save) pending;
            lock (_sync)
            {
                if (!_pendingGameSaveByRulesetId.TryGetValue(rulesetId, out pending))
                {
                    return;
                }
            }

            PersistGame(pending.Config, pending.Save);
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled - expected
        }
        catch (Exception ex)
        {
            LogSaveGameStateFailed(logger, ex);
        }
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
