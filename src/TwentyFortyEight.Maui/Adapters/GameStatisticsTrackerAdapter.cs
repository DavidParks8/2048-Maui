using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Services;

namespace TwentyFortyEight.Maui.Adapters;

/// <summary>
/// Adapter that implements IGameStatisticsTracker from the core engine
/// and delegates to IStatisticsService in the MAUI layer.
/// </summary>
public class GameStatisticsTrackerAdapter : IGameStatisticsTracker
{
    private readonly IStatisticsService _statisticsService;

    public GameStatisticsTrackerAdapter(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public void OnGameStarted()
    {
        _statisticsService.IncrementGamesPlayed();
        _statisticsService.StartTimeTracking();
    }

    public void OnMoveMade(int currentScore, int highestTile)
    {
        _statisticsService.UpdateBestScore(currentScore);
        _statisticsService.UpdateHighestTile(highestTile);
        _statisticsService.AddMoves(1);
    }

    public void OnGameWon()
    {
        _statisticsService.IncrementGamesWon();
    }

    public void OnGameOver(int finalScore, bool wasWon)
    {
        _statisticsService.StopTimeTracking();
        _statisticsService.AddScore(finalScore);
        
        if (!wasWon)
        {
            _statisticsService.RecordGameLoss();
        }
    }
}
