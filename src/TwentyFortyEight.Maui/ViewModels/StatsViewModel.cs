using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Resources.Strings;

namespace TwentyFortyEight.Maui.ViewModels;

/// <summary>
/// ViewModel for the Statistics page.
/// </summary>
public partial class StatsViewModel : ObservableObject
{
    private readonly IStatisticsTracker _statisticsTracker;

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

    public StatsViewModel(IStatisticsTracker statisticsTracker)
    {
        _statisticsTracker = statisticsTracker;
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
        var window = Application.Current?.Windows.FirstOrDefault();
        var page = window?.Page;
        if (page is null)
        {
            return;
        }

        bool confirmed = await page.DisplayAlertAsync(
            AppStrings.ResetStatisticsTitle,
            AppStrings.ResetStatisticsMessage,
            AppStrings.Reset,
            AppStrings.Cancel
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
        await Shell.Current.GoToAsync("..");
    }

    private static string FormatWinRate(double winRate)
    {
        return $"{winRate:F1}%";
    }
}
