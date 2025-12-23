using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwentyFortyEight.Maui.Models;
using TwentyFortyEight.Maui.Services;

namespace TwentyFortyEight.Maui.ViewModels;

/// <summary>
/// ViewModel for the statistics page.
/// </summary>
public partial class StatsViewModel : ObservableObject
{
    private readonly IStatisticsService _statisticsService;

    [ObservableProperty]
    private GameStatistics _statistics;

    public StatsViewModel(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        _statistics = _statisticsService.GetStatistics();
    }

    /// <summary>
    /// Refreshes the statistics from the service.
    /// </summary>
    public void RefreshStatistics()
    {
        Statistics = _statisticsService.GetStatistics();
    }

    [RelayCommand]
    private async Task ResetStatistics()
    {
        var mainPage = Shell.Current?.CurrentPage;
        if (mainPage == null)
            return;

        var result = await mainPage.DisplayAlertAsync(
            "Reset Statistics",
            "Are you sure you want to reset all statistics? This action cannot be undone.",
            "Reset",
            "Cancel"
        );

        if (result)
        {
            _statisticsService.ResetStatistics();
            RefreshStatistics();
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}
