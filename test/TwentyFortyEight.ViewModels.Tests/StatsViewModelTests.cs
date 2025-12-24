using Moq;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for StatsViewModel.
/// </summary>
[TestClass]
public class StatsViewModelTests
{
    private Mock<IStatisticsTracker> _statisticsTrackerMock = null!;
    private Mock<IAlertService> _alertServiceMock = null!;
    private Mock<INavigationService> _navigationServiceMock = null!;
    private Mock<ILocalizationService> _localizationServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _statisticsTrackerMock = new Mock<IStatisticsTracker>();
        _alertServiceMock = new Mock<IAlertService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup default statistics
        _statisticsTrackerMock.Setup(s => s.GetStatistics()).Returns(new GameStatistics());
    }

    private StatsViewModel CreateViewModel()
    {
        return new StatsViewModel(
            _statisticsTrackerMock.Object,
            _alertServiceMock.Object,
            _navigationServiceMock.Object,
            _localizationServiceMock.Object
        );
    }

    [TestMethod]
    public void Constructor_LoadsStatisticsFromTracker()
    {
        // Arrange
        var stats = new GameStatistics
        {
            GamesPlayed = 10,
            GamesWon = 5,
            BestScore = 10000,
            HighestTile = 2048,
            TotalMoves = 500,
            CurrentStreak = 2,
            BestStreak = 3,
        };
        _statisticsTrackerMock.Setup(s => s.GetStatistics()).Returns(stats);

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
        var initialStats = new GameStatistics { GamesPlayed = 5 };
        var updatedStats = new GameStatistics { GamesPlayed = 10 };
        _statisticsTrackerMock.Setup(s => s.GetStatistics()).Returns(initialStats);
        var viewModel = CreateViewModel();

        _statisticsTrackerMock.Setup(s => s.GetStatistics()).Returns(updatedStats);

        // Act
        viewModel.RefreshStatistics();

        // Assert
        Assert.AreEqual(10, viewModel.GamesPlayed);
    }

    [TestMethod]
    public async Task GoBackCommand_CallsNavigationService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert
        _navigationServiceMock.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [TestMethod]
    public async Task ResetStatisticsAsync_WhenConfirmed_ResetsTracker()
    {
        // Arrange
        _alertServiceMock
            .Setup(a =>
                a.ShowConfirmationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(true);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ResetStatisticsCommand.ExecuteAsync(null);

        // Assert
        _statisticsTrackerMock.Verify(s => s.Reset(), Times.Once);
    }

    [TestMethod]
    public async Task ResetStatisticsAsync_WhenCancelled_DoesNotResetTracker()
    {
        // Arrange
        _alertServiceMock
            .Setup(a =>
                a.ShowConfirmationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(false);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.ResetStatisticsCommand.ExecuteAsync(null);

        // Assert
        _statisticsTrackerMock.Verify(s => s.Reset(), Times.Never);
    }

    [TestMethod]
    public void WinRate_FormatsCorrectly()
    {
        // Arrange
        var stats = new GameStatistics { GamesPlayed = 10, GamesWon = 3 }; // 30% win rate
        _statisticsTrackerMock.Setup(s => s.GetStatistics()).Returns(stats);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual("30.0%", viewModel.WinRate);
    }
}
