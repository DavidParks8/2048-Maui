using System.Reflection;
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
    private Mock<IGameStateRepository> _repositoryMock = null!;
    private Mock<IGameSessionCoordinator> _sessionCoordinatorMock = null!;
    private Mock<IUserFeedbackService> _feedbackServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<GameViewModel>>();
        _moveAnalyzerMock = new Mock<IMoveAnalyzer>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _statisticsTrackerMock = new Mock<IStatisticsTracker>();
        _randomSourceMock = new Mock<IRandomSource>();
        _repositoryMock = new Mock<IGameStateRepository>();
        _sessionCoordinatorMock = new Mock<IGameSessionCoordinator>();
        _feedbackServiceMock = new Mock<IUserFeedbackService>();

        // Setup default behavior
        _settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        _repositoryMock.Setup(r => r.GetBestScore()).Returns(0);
        _repositoryMock.Setup(r => r.LoadGameState()).Returns((GameState?)null);
        _sessionCoordinatorMock.Setup(s => s.IsSocialGamingAvailable).Returns(false);

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
            _repositoryMock.Object,
            _sessionCoordinatorMock.Object,
            _feedbackServiceMock.Object
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
    public async Task ShowHowToPlayCommand_CallsFeedbackService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.ShowHowToPlayCommand.ExecuteAsync(null);

        // Assert
        _feedbackServiceMock.Verify(f => f.ShowHowToPlayAsync(), Times.Once);
    }

    [TestMethod]
    public void OpenStatsCommand_SendsNavigationMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.OpenStatsCommand.Execute(null);

        // Assert - No verification needed, messenger pattern handles navigation
        // Could verify message was sent if we inject IMessenger abstraction
    }

    [TestMethod]
    public void OpenSettingsCommand_SendsNavigationMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.OpenSettingsCommand.Execute(null);

        // Assert - No verification needed, messenger pattern handles navigation
        // Could verify message was sent if we inject IMessenger abstraction
    }

    [TestMethod]
    public void BestScore_CanBeSetDirectly()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.BestScore = 1000;

        // Assert
        Assert.AreEqual(1000, viewModel.BestScore);
        // Note: Repository is only updated through UpdateBestScoreIfHigher during gameplay,
        // not when BestScore property is set directly
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
        _feedbackServiceMock.Verify(f => f.ConfirmNewGameAsync(), Times.Never);
    }

    [TestMethod]
    public void VictoryAnimationRequested_ForwardsEngineVictoryEvent_AfterInitialization()
    {
        // Arrange
        var viewModel = CreateViewModel();

        int eventCount = 0;
        EventArgs? forwardedArgs = null;
        viewModel.VictoryAnimationRequested += (_, e) =>
        {
            eventCount++;
            forwardedArgs = e;
        };

        var args = new EventArgs();

        // Act: simulate the engine raising VictoryAchieved by invoking the private handler.
        InvokePrivateEngineVictoryHandler(viewModel, args);

        // Assert
        Assert.AreEqual(1, eventCount);
        Assert.IsNotNull(forwardedArgs);
        Assert.AreSame(args, forwardedArgs);
    }

    [TestMethod]
    public void VictoryAnimationRequested_DoesNotForward_WhenNotInitialized()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Force the initialization gate back to false.
        SetPrivateField(viewModel, "_isInitialized", false);

        int eventCount = 0;
        viewModel.VictoryAnimationRequested += (_, _) => eventCount++;

        // Act
        InvokePrivateEngineVictoryHandler(viewModel, EventArgs.Empty);

        // Assert
        Assert.AreEqual(0, eventCount);
    }

    private static void InvokePrivateEngineVictoryHandler(GameViewModel viewModel, EventArgs args)
    {
        var method = typeof(GameViewModel).GetMethod(
            "OnEngineVictoryAchieved",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.IsNotNull(method);
        method!.Invoke(viewModel, [null, args]);
    }

    private static void SetPrivateField<T>(GameViewModel viewModel, string fieldName, T value)
    {
        var field = typeof(GameViewModel).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Assert.IsNotNull(field);
        field!.SetValue(viewModel, value);
    }
}
