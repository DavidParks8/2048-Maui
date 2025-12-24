using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels;

/// <summary>
/// ViewModel for the Statistics page.
/// </summary>
public partial class StatsViewModel : ObservableObject
{
    private readonly IStatisticsTracker _statisticsTracker;
    private readonly IAlertService _alertService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private int _gamesPlayed;

    [ObservableProperty]
    private int _gamesWon;

    [ObservableProperty]
    private string _winRate = "0%";

    [ObservableProperty]
    private int _bestScore;

    [ObservableProperty]
    private int _averageScore;

    [ObservableProperty]
    private int _highestTile;

    [ObservableProperty]
    private long _totalMoves;

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private int _bestStreak;

    public StatsViewModel(
        IStatisticsTracker statisticsTracker,
        IAlertService alertService,
        INavigationService navigationService,
        ILocalizationService localizationService
    )
    {
        _statisticsTracker = statisticsTracker;
        _alertService = alertService;
        _navigationService = navigationService;
        _localizationService = localizationService;
        RefreshStatistics();
    }

    /// <summary>
    /// Refreshes all statistics from the tracker.
    /// </summary>
    public void RefreshStatistics()
    {
        var stats = _statisticsTracker.GetStatistics();

        GamesPlayed = stats.GamesPlayed;
        GamesWon = stats.GamesWon;
        WinRate = FormatWinRate(stats.WinRate);
        BestScore = stats.BestScore;
        AverageScore = stats.AverageScore;
        HighestTile = stats.HighestTile;
        TotalMoves = stats.TotalMoves;
        CurrentStreak = stats.CurrentStreak;
        BestStreak = stats.BestStreak;
    }

    [RelayCommand]
    private async Task ResetStatisticsAsync()
    {
        bool confirmed = await _alertService.ShowConfirmationAsync(
            _localizationService.ResetStatisticsTitle,
            _localizationService.ResetStatisticsMessage,
            _localizationService.Reset,
            _localizationService.Cancel
        );

        if (confirmed)
        {
            _statisticsTracker.Reset();
            RefreshStatistics();
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
    }

    private static string FormatWinRate(double winRate)
    {
        return $"{winRate:F1}%";
    }
}
