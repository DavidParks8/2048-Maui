using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for StatsViewModel.
/// </summary>
[TestClass]
public class StatsViewModelTests
{
    private IStatisticsTracker _statisticsTrackerMock = null!;
    private IAlertService _alertServiceMock = null!;
    private ILocalizationService _localizationServiceMock = null!;
    private ISettingsService _settingsServiceMock = null!;
    private IMessenger _messenger = null!;

    [TestInitialize]
    public void Setup()
    {
        _statisticsTrackerMock = Substitute.For<IStatisticsTracker>();
        _alertServiceMock = Substitute.For<IAlertService>();
        _localizationServiceMock = Substitute.For<ILocalizationService>();
        _settingsServiceMock = Substitute.For<ISettingsService>();
        _messenger = new WeakReferenceMessenger();

        // Setup default statistics
        _statisticsTrackerMock.GetStatistics().Returns(new GameStatistics());

        // Default mode/scope
        _settingsServiceMock.LastActiveGameConfig.Returns(
            new GameConfig { Size = 4, WinTile = 2048 }
        );
    }

    private StatsViewModel CreateViewModel()
    {
        return new StatsViewModel(
            _statisticsTrackerMock,
            _alertServiceMock,
            _localizationServiceMock,
            _settingsServiceMock,
            _messenger
        );
    }

    [TestMethod]
    public void Constructor_SetsBoardSizeDisplay_FromSettings()
    {
        _settingsServiceMock.LastActiveGameConfig.Returns(
            new GameConfig { Size = 5, WinTile = 2048 }
        );

        var viewModel = CreateViewModel();

        Assert.AreEqual("5×5", viewModel.BoardSizeDisplay);
    }

    [TestMethod]
    public void Constructor_LoadsStatisticsFromTracker()
    {
        // Arrange
        GameStatistics stats = new()
        {
            GamesPlayed = 10,
            GamesWon = 5,
            BestScore = 10000,
            HighestTile = 2048,
            TotalMoves = 500,
            CurrentStreak = 2,
            BestStreak = 3,
        };
        _statisticsTrackerMock.GetStatistics().Returns(stats);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual(10, viewModel.GamesPlayed);
        Assert.AreEqual(5, viewModel.GamesWon);
        Assert.AreEqual(10000, viewModel.BestScore);
        Assert.AreEqual(2048, viewModel.HighestTile);
        Assert.AreEqual(500, viewModel.TotalMoves);
        Assert.AreEqual(2, viewModel.CurrentStreak);
        Assert.AreEqual(3, viewModel.BestStreak);
    }

    [TestMethod]
    public void RefreshStatistics_ReloadsFromTracker()
    {
        // Arrange
        GameStatistics initialStats = new() { GamesPlayed = 5 };
        GameStatistics updatedStats = new() { GamesPlayed = 10 };
        _statisticsTrackerMock.GetStatistics().Returns(initialStats);
        var viewModel = CreateViewModel();

        _statisticsTrackerMock.GetStatistics().Returns(updatedStats);

        // Act
        viewModel.RefreshStatistics();

        // Assert
        Assert.AreEqual(10, viewModel.GamesPlayed);
    }

    [TestMethod]
    public async Task ResetStatisticsAsync_WhenConfirmed_ResetsTracker()
    {
        // Arrange
        _alertServiceMock
            .ShowConfirmationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .Returns(Task.FromResult(true));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ResetStatisticsCommand.ExecuteAsync(null);

        // Assert
        _statisticsTrackerMock.Received(1).Reset();
    }

    [TestMethod]
    public async Task ResetStatisticsAsync_WhenCancelled_DoesNotResetTracker()
    {
        // Arrange
        _alertServiceMock
            .ShowConfirmationAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .Returns(Task.FromResult(false));

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ResetStatisticsCommand.ExecuteAsync(null);

        // Assert
        _statisticsTrackerMock.DidNotReceive().Reset();
    }

    [TestMethod]
    public void WinRate_FormatsCorrectly()
    {
        // Arrange
        GameStatistics stats = new() { GamesPlayed = 10, GamesWon = 3 }; // 30% win rate
        _statisticsTrackerMock.GetStatistics().Returns(stats);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual("30.0%", viewModel.WinRate);
    }
}
