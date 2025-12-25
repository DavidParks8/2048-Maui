using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 0)]

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for GameViewModel demonstrating MVVM testing capabilities.
/// </summary>
[TestClass]
public class GameViewModelTests
{
    private Mock<ILogger<GameViewModel>> _loggerMock = null!;
    private Mock<IMoveAnalyzer> _moveAnalyzerMock = null!;
    private Mock<ISettingsService> _settingsServiceMock = null!;
    private Mock<IStatisticsTracker> _statisticsTrackerMock = null!;
    private Mock<IRandomSource> _randomSourceMock = null!;
    private Mock<IPreferencesService> _preferencesServiceMock = null!;
    private Mock<IAlertService> _alertServiceMock = null!;
    private Mock<INavigationService> _navigationServiceMock = null!;
    private Mock<ILocalizationService> _localizationServiceMock = null!;
    private Mock<IScreenReaderService> _screenReaderServiceMock = null!;
    private Mock<IHapticService> _hapticServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<GameViewModel>>();
        _moveAnalyzerMock = new Mock<IMoveAnalyzer>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _statisticsTrackerMock = new Mock<IStatisticsTracker>();
        _randomSourceMock = new Mock<IRandomSource>();
        _preferencesServiceMock = new Mock<IPreferencesService>();
        _alertServiceMock = new Mock<IAlertService>();
        _navigationServiceMock = new Mock<INavigationService>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _screenReaderServiceMock = new Mock<IScreenReaderService>();
        _hapticServiceMock = new Mock<IHapticService>();

        // Setup default behavior
        _settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        _settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        _hapticServiceMock.Setup(h => h.IsSupported).Returns(true);
        _preferencesServiceMock
            .Setup(p => p.GetInt(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(0);
        _preferencesServiceMock
            .Setup(p => p.GetString(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(string.Empty);

        // Setup random source for deterministic tile spawning
        _randomSourceMock.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        _randomSourceMock.Setup(r => r.NextDouble()).Returns(0.5);
    }

    private GameViewModel CreateViewModel()
    {
        return new GameViewModel(
            _loggerMock.Object,
            _moveAnalyzerMock.Object,
            _settingsServiceMock.Object,
            _statisticsTrackerMock.Object,
            _randomSourceMock.Object,
            _preferencesServiceMock.Object,
            _alertServiceMock.Object,
            _navigationServiceMock.Object,
            _localizationServiceMock.Object,
            _screenReaderServiceMock.Object,
            _hapticServiceMock.Object
        );
    }

    [TestMethod]
    public void Constructor_InitializesTilesCollection()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsNotNull(viewModel.Tiles);
        Assert.HasCount(16, viewModel.Tiles); // 4x4 board
    }

    [TestMethod]
    public void Constructor_InitializesScoreToZero()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual(0, viewModel.Score);
    }

    [TestMethod]
    public void BoardSize_Returns4()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual(4, viewModel.BoardSize);
    }

    [TestMethod]
    public void IsGameOver_InitiallyFalse()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsFalse(viewModel.IsGameOver);
    }

    [TestMethod]
    public void IsHowToPlayVisible_InitiallyFalse()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsFalse(viewModel.IsHowToPlayVisible);
    }

    [TestMethod]
    public void ShowHowToPlayCommand_SetsIsHowToPlayVisibleToTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.ShowHowToPlayCommand.Execute(null);

        // Assert
        Assert.IsTrue(viewModel.IsHowToPlayVisible);
    }

    [TestMethod]
    public void CloseHowToPlayCommand_SetsIsHowToPlayVisibleToFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ShowHowToPlayCommand.Execute(null);

        // Act
        viewModel.CloseHowToPlayCommand.Execute(null);

        // Assert
        Assert.IsFalse(viewModel.IsHowToPlayVisible);
    }

    [TestMethod]
    public async Task OpenStatsAsync_NavigatesToStats()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.OpenStatsCommand.ExecuteAsync(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateToAsync("stats"), Times.Once);
    }

    [TestMethod]
    public async Task OpenSettingsAsync_NavigatesToSettings()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.OpenSettingsCommand.ExecuteAsync(null);

        // Assert
        _navigationServiceMock.Verify(n => n.NavigateToAsync("settings"), Times.Once);
    }

    [TestMethod]
    public async Task BestScoreChanged_SavesToPreferencesAfterDebounce()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.BestScore = 1000;

        await viewModel.WaitForBestScoreSaveAsync();

        // Assert
        _preferencesServiceMock.Verify(p => p.SetInt("BestScore", 1000), Times.Once);
    }

    [TestMethod]
    public async Task NewGameAsync_WhenNoMovesAndNotGameOver_DoesNotShowConfirmation()
    {
        // Arrange
        var viewModel = CreateViewModel();
        // New game has 0 moves

        // Act
        await viewModel.NewGameCommand.ExecuteAsync(null);

        // Assert - Should not show confirmation dialog
        _alertServiceMock.Verify(
            a =>
                a.ShowConfirmationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Never
        );
    }

    [TestMethod]
    public void SignalAnimationComplete_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert - Should not throw even when no animation is pending
        viewModel.SignalAnimationComplete();
    }
}
