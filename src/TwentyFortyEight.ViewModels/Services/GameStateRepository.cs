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
    private const int LegacyBoardSize = 4;

    private const string SavedGameKeyPrefix = "SavedGame.";
    private const string BestScoreKeyPrefix = "BestScore.";

    private const string LegacySavedGameKey = "SavedGame";
    private const string LegacyBestScoreKey = "BestScore";
    private const string MigrationKey = "Migration.SizeScopedSaveStateV1Complete";
    private const string LegacyMigrationKey = "Migration.SizeScopedPersistenceV1Complete";

    private readonly Lock _sync = new();

    // Per-size debouncing for best score saves
    private readonly Dictionary<int, CancellationTokenSource> _bestScoreSaveDebounceBySize = [];
    private readonly Dictionary<int, Task> _bestScoreSaveTaskBySize = [];
    private readonly Dictionary<int, int> _currentBestScoreBySize = [];

    private bool _migrationChecked;

    private static string GetSavedGameKey(int boardSize) => $"{SavedGameKeyPrefix}{boardSize}";

    private static string GetBestScoreKey(int boardSize) => $"{BestScoreKeyPrefix}{boardSize}";

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
                // Honor previous combined sentinel if it exists to avoid re-running migrations.
                if (
                    preferencesService.GetBool(MigrationKey, false)
                    || preferencesService.GetBool(LegacyMigrationKey, false)
                )
                {
                    _migrationChecked = true;
                    return;
                }

                // Migrate legacy saved game -> size 4 slot (only if new slot is empty)
                if (
                    preferencesService.ContainsKey(LegacySavedGameKey)
                    && !preferencesService.ContainsKey(GetSavedGameKey(LegacyBoardSize))
                )
                {
                    var legacyJson = preferencesService.GetString(LegacySavedGameKey, string.Empty);
                    if (!string.IsNullOrEmpty(legacyJson))
                    {
                        preferencesService.SetString(GetSavedGameKey(LegacyBoardSize), legacyJson);
                    }
                }

                // Migrate legacy best score -> size 4 slot (only if new slot is empty)
                if (
                    preferencesService.ContainsKey(LegacyBestScoreKey)
                    && !preferencesService.ContainsKey(GetBestScoreKey(LegacyBoardSize))
                )
                {
                    var legacyBest = preferencesService.GetInt(LegacyBestScoreKey, 0);
                    preferencesService.SetInt(GetBestScoreKey(LegacyBoardSize), legacyBest);
                }

                // Delete legacy keys after migration
                if (preferencesService.ContainsKey(LegacySavedGameKey))
                {
                    preferencesService.Remove(LegacySavedGameKey);
                }
                if (preferencesService.ContainsKey(LegacyBestScoreKey))
                {
                    preferencesService.Remove(LegacyBestScoreKey);
                }

                preferencesService.SetBool(MigrationKey, true);
                // Keep the legacy sentinel in sync for smooth downgrades.
                preferencesService.SetBool(LegacyMigrationKey, true);
            }
            catch (Exception ex)
            {
                // If migration fails, keep going using size-scoped keys.
                LogMigrationFailed(logger, ex);
            }
            finally
            {
                _migrationChecked = true;
            }
        }
    }

    private int EnsureBestScoreLoaded(int boardSize)
    {
        EnsureMigrated();

        if (boardSize <= 0 || boardSize > GameConfig.MaxReasonableBoardSize)
        {
            return 0;
        }

        lock (_sync)
        {
            if (_currentBestScoreBySize.TryGetValue(boardSize, out var best))
            {
                return best;
            }

            var loadedBest = preferencesService.GetInt(GetBestScoreKey(boardSize), 0);
            _currentBestScoreBySize[boardSize] = loadedBest;
            return loadedBest;
        }
    }

    public GameState? LoadGameState(GameConfig config)
    {
        EnsureMigrated();

        var boardSize = config.Size;
        try
        {
            var savedJson = preferencesService.GetString(GetSavedGameKey(boardSize), string.Empty);
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
        EnsureMigrated();

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
            preferencesService.SetString(GetSavedGameKey(boardSize), json);
        }
        catch (Exception ex)
        {
            LogSaveGameStateFailed(logger, ex);
        }
    }

    public int GetBestScore(GameConfig config) => EnsureBestScoreLoaded(config.Size);

    public void UpdateBestScoreIfHigher(GameConfig config, int score)
    {
        var boardSize = config.Size;
        if (boardSize <= 0 || boardSize > GameConfig.MaxReasonableBoardSize)
        {
            return;
        }

        var currentBest = EnsureBestScoreLoaded(boardSize);
        if (score <= currentBest)
        {
            return;
        }

        Task saveTask;
        CancellationTokenSource cts;

        lock (_sync)
        {
            _currentBestScoreBySize[boardSize] = score;

            // Debounce saves to avoid hammering storage during rapid play
            if (_bestScoreSaveDebounceBySize.TryGetValue(boardSize, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            cts = new CancellationTokenSource();
            _bestScoreSaveDebounceBySize[boardSize] = cts;

            saveTask = DebouncedSaveBestScoreAsync(boardSize, score, cts.Token);
            _bestScoreSaveTaskBySize[boardSize] = saveTask;
        }

        // Clean up task/CTS tracking when the save finishes.
        _ = saveTask.ContinueWith(
            static (t, state) =>
            {
                var tuple = (Tuple<GameStateRepository, int, Task, CancellationTokenSource>)state!;
                tuple.Item1.CleanupAfterBestScoreSave(tuple.Item2, tuple.Item3, tuple.Item4);
            },
            Tuple.Create(this, boardSize, saveTask, cts),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    public Task FlushAsync(GameConfig config)
    {
        var boardSize = config.Size;
        lock (_sync)
        {
            if (_bestScoreSaveTaskBySize.TryGetValue(boardSize, out var task))
            {
                return task;
            }
        }

        return Task.CompletedTask;
    }

    private void CleanupAfterBestScoreSave(
        int boardSize,
        Task completedTask,
        CancellationTokenSource cts
    )
    {
        lock (_sync)
        {
            if (
                _bestScoreSaveTaskBySize.TryGetValue(boardSize, out var currentTask)
                && ReferenceEquals(currentTask, completedTask)
            )
            {
                _bestScoreSaveTaskBySize.Remove(boardSize);
            }

            if (
                _bestScoreSaveDebounceBySize.TryGetValue(boardSize, out var currentCts)
                && ReferenceEquals(currentCts, cts)
            )
            {
                _bestScoreSaveDebounceBySize.Remove(boardSize);
                cts.Dispose();
            }
        }
    }

    private async Task DebouncedSaveBestScoreAsync(
        int boardSize,
        int value,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Task.Delay(500, cancellationToken);
            preferencesService.SetInt(GetBestScoreKey(boardSize), value);
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

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Failed to migrate legacy save/best score keys"
    )]
    private static partial void LogMigrationFailed(ILogger logger, Exception ex);
}
