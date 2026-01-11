using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Messages;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels;

/// <summary>
/// ViewModel for the Statistics page.
/// </summary>
public partial class StatsViewModel : ObservableObject
{
    private readonly IStatisticsTracker _statisticsTracker;
    private readonly IAlertService _alertService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private string _boardSizeDisplay = string.Empty;

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
        ILocalizationService localizationService,
        ISettingsService settingsService,
        IMessenger messenger
    )
    {
        _statisticsTracker = statisticsTracker;
        _alertService = alertService;
        _localizationService = localizationService;
        _settingsService = settingsService;
        _messenger = messenger;

        _messenger.Register<RulesetChangedMessage>(
            this,
            static (recipient, message) =>
            {
                var vm = (StatsViewModel)recipient;
                vm.UpdateBoardSizeDisplay(message.NewBoardSize);
                vm.RefreshStatistics();
            }
        );

        UpdateBoardSizeDisplayFromSettings();
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

        // Keep scope label in sync even when the page is revisited.
        UpdateBoardSizeDisplayFromSettings();
    }

    private void UpdateBoardSizeDisplayFromSettings()
    {
        var config = _settingsService.LastActiveGameConfig;
        UpdateBoardSizeDisplay(config.Size);
    }

    private void UpdateBoardSizeDisplay(int boardSize)
    {
        if (boardSize <= 0 || boardSize > GameConfig.MaxReasonableBoardSize)
        {
            boardSize = 4;
        }

        BoardSizeDisplay = $"{boardSize}×{boardSize}";
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

    private static string FormatWinRate(double winRate)
    {
        return $"{winRate:F1}%";
    }
}
