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
    private const string SavedGameKey = "SavedGame";
    private const string BestScoreKey = "BestScore";

    // Debouncing for best score saves
    private CancellationTokenSource? _bestScoreSaveDebounce;
    private Task _bestScoreSaveTask = Task.CompletedTask;
    private int _currentBestScore = preferencesService.GetInt(BestScoreKey, 0);

    public GameState? LoadGameState()
    {
        try
        {
            var savedJson = preferencesService.GetString(SavedGameKey, string.Empty);
            if (!string.IsNullOrEmpty(savedJson))
            {
                var dto = JsonSerializer.Deserialize(
                    savedJson,
                    GameSerializationContext.Default.GameStateDto
                );
                return dto?.ToGameState();
            }
        }
        catch (Exception ex)
        {
            LogLoadGameStateFailed(logger, ex);
        }

        return null;
    }

    public void SaveGameState(GameState state)
    {
        try
        {
            var dto = GameStateDto.FromGameState(state);
            var json = JsonSerializer.Serialize(dto, GameSerializationContext.Default.GameStateDto);
            preferencesService.SetString(SavedGameKey, json);
        }
        catch (Exception ex)
        {
            LogSaveGameStateFailed(logger, ex);
        }
    }

    public int GetBestScore() => _currentBestScore;

    public void UpdateBestScoreIfHigher(int score)
    {
        if (score <= _currentBestScore)
        {
            return;
        }

        _currentBestScore = score;

        // Debounce saves to avoid hammering storage during rapid play
        _bestScoreSaveDebounce?.Cancel();
        _bestScoreSaveDebounce?.Dispose();
        _bestScoreSaveDebounce = new CancellationTokenSource();

        _bestScoreSaveTask = DebouncedSaveBestScoreAsync(score, _bestScoreSaveDebounce.Token);
    }

    public Task FlushAsync() => _bestScoreSaveTask;

    private async Task DebouncedSaveBestScoreAsync(int value, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(500, cancellationToken);
            preferencesService.SetInt(BestScoreKey, value);
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
