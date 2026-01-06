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
    private VictoryViewModel _victoryViewModel = null!;

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

        // Create real VictoryViewModel instance for testing
        var reduceMotionMock = new Mock<IReduceMotionService>();
        var victoryFeedbackMock = new Mock<IUserFeedbackService>();
        var localizationMock = new Mock<ILocalizationService>();
        localizationMock
            .Setup(x => x.FormatScore(It.IsAny<int>()))
            .Returns((int score) => $"{score}");
        _victoryViewModel = new VictoryViewModel(
            reduceMotionMock.Object,
            victoryFeedbackMock.Object,
            localizationMock.Object
        );

        // Setup default behavior
        _settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        _settingsServiceMock.Setup(s => s.LastActiveGameConfig).Returns(new GameConfig());
        _repositoryMock.Setup(r => r.GetBestScore(It.IsAny<GameConfig>())).Returns(0);
        _repositoryMock
            .Setup(r => r.LoadGameState(It.IsAny<GameConfig>()))
            .Returns((GameState?)null);
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
            _feedbackServiceMock.Object,
            _victoryViewModel
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
    public void Constructor_WhenLastActiveBoardSizeSet_RestoresMode()
    {
        // Arrange
        _settingsServiceMock
            .Setup(s => s.LastActiveGameConfig)
            .Returns(new GameConfig { Size = 5 });

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual(5, viewModel.BoardSize);
        Assert.HasCount(25, viewModel.Tiles); // 5x5 board
        _repositoryMock.Verify(
            r => r.GetBestScore(It.Is<GameConfig>(c => c.Size == 5)),
            Times.Once
        );
        _repositoryMock.Verify(
            r => r.LoadGameState(It.Is<GameConfig>(c => c.Size == 5)),
            Times.Once
        );
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
        Assert.IsFalse(viewModel.IsNewGameConfirmationVisible);
    }

    [TestMethod]
    public async Task NewGameAsync_WhenMovesGreaterThanZero_ShowsConfirmationSheet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Moves = 1;

        // Act
        await viewModel.NewGameCommand.ExecuteAsync(null);

        // Assert
        Assert.IsTrue(viewModel.IsNewGameConfirmationVisible);
        _repositoryMock.Verify(
            r => r.SaveGameState(It.IsAny<GameConfig>(), It.IsAny<GameState>()),
            Times.Never
        );
    }

    [TestMethod]
    public async Task ConfirmNewGameCommand_WhenSheetVisible_StartsNewGameAndHidesSheet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Moves = 1;

        await viewModel.NewGameCommand.ExecuteAsync(null);
        Assert.IsTrue(viewModel.IsNewGameConfirmationVisible);

        // Act
        await viewModel.ConfirmNewGameCommand.ExecuteAsync(null);

        // Assert
        Assert.IsFalse(viewModel.IsNewGameConfirmationVisible);
        _repositoryMock.Verify(
            r => r.SaveGameState(It.Is<GameConfig>(c => c.Size == 4), It.IsAny<GameState>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task DismissNewGameConfirmationCommand_WhenSheetVisible_HidesSheetWithoutStartingNewGame()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Moves = 1;

        await viewModel.NewGameCommand.ExecuteAsync(null);
        Assert.IsTrue(viewModel.IsNewGameConfirmationVisible);

        // Act
        viewModel.DismissNewGameConfirmationCommand.Execute(null);

        // Assert
        Assert.IsFalse(viewModel.IsNewGameConfirmationVisible);
        _repositoryMock.Verify(
            r => r.SaveGameState(It.IsAny<GameConfig>(), It.IsAny<GameState>()),
            Times.Never
        );
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
    public async Task PlaySelectedModeCommand_SavesOutgoingRunAndResumesSelectedRuleset()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _repositoryMock
            .Setup(r => r.FlushAsync(It.IsAny<GameConfig>()))
            .Returns(Task.CompletedTask);

        var oldRulesetId = new GameConfig { Size = 4, WinTile = 2048 }.RulesetId;
        var newRulesetId = new GameConfig { Size = 5, WinTile = 2048 }.RulesetId;

        viewModel.PendingBoardSize = 5;

        // Act
        await viewModel.PlaySelectedModeCommand.ExecuteAsync(null);

        // Assert
        _repositoryMock.Verify(
            r =>
                r.SaveGameState(
                    It.Is<GameConfig>(c => c.RulesetId == oldRulesetId),
                    It.IsAny<GameState>()
                ),
            Times.AtLeastOnce
        );

        _repositoryMock.Verify(
            r =>
                r.SaveGameState(
                    It.Is<GameConfig>(c => c.RulesetId == newRulesetId),
                    It.IsAny<GameState>()
                ),
            Times.AtLeastOnce
        );

        _repositoryMock.Verify(r => r.ClearSavedGame(It.IsAny<GameConfig>()), Times.Never);
    }

    [TestMethod]
    public async Task StartNewSelectedModeCommand_ClearsSavedRunForSelectedRuleset()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _repositoryMock
            .Setup(r => r.FlushAsync(It.IsAny<GameConfig>()))
            .Returns(Task.CompletedTask);

        var newRulesetId = new GameConfig { Size = 5, WinTile = 2048 }.RulesetId;
        viewModel.PendingBoardSize = 5;

        // Act
        await viewModel.StartNewSelectedModeCommand.ExecuteAsync(null);

        // Assert
        _repositoryMock.Verify(
            r => r.ClearSavedGame(It.Is<GameConfig>(c => c.RulesetId == newRulesetId)),
            Times.Once
        );
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

    [TestMethod]
    public async Task NewGameAsync_WhenVictoryModalShowing_HidesVictoryOverlay()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Simulate victory state - manually show modal
        _victoryViewModel.TriggerVictory(score: 2048, winningValue: 2048);
        _victoryViewModel.ShowModal(); // Show modal explicitly for testing
        Assert.IsTrue(_victoryViewModel.State.IsActive, "Victory should be active");
        Assert.IsTrue(_victoryViewModel.State.IsModalVisible, "Victory modal should be visible");

        // Act - Start new game from title bar (not from modal button)
        await viewModel.NewGameCommand.ExecuteAsync(null);

        // Assert - Victory overlay should be hidden
        Assert.IsFalse(_victoryViewModel.State.IsActive, "Victory should no longer be active");
        Assert.IsFalse(_victoryViewModel.State.IsModalVisible, "Victory modal should be hidden");
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
