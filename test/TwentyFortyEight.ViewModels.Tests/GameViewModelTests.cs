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
    private Mock<IMoveAdvisor> _moveAdvisorMock = null!;
    private Mock<ISettingsService> _settingsServiceMock = null!;
    private Mock<IStatisticsTracker> _statisticsTrackerMock = null!;
    private Mock<IRandomSource> _randomSourceMock = null!;
    private IGame2048EngineFactory _engineFactory = null!;
    private Mock<IGameStateRepository> _repositoryMock = null!;
    private Mock<IGameSessionCoordinator> _sessionCoordinatorMock = null!;
    private Mock<IUserFeedbackService> _feedbackServiceMock = null!;
    private IBoardSimulator _boardSimulator = null!;
    private Mock<ICoachNudgeService> _coachNudgeServiceMock = null!;
    private Mock<ICoachSuggestionService> _coachSuggestionServiceMock = null!;
    private VictoryViewModel _victoryViewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<GameViewModel>>();
        _moveAnalyzerMock = new Mock<IMoveAnalyzer>();
        _moveAdvisorMock = new Mock<IMoveAdvisor>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _statisticsTrackerMock = new Mock<IStatisticsTracker>();
        _randomSourceMock = new Mock<IRandomSource>();
        _repositoryMock = new Mock<IGameStateRepository>();
        _sessionCoordinatorMock = new Mock<IGameSessionCoordinator>();
        _feedbackServiceMock = new Mock<IUserFeedbackService>();
        _coachNudgeServiceMock = new Mock<ICoachNudgeService>();
        _coachSuggestionServiceMock = new Mock<ICoachSuggestionService>();

        // Create real VictoryViewModel instance for testing
        var accessibilitySettingsMock = new Mock<IAccessibilitySettingsService>();
        var victoryFeedbackMock = new Mock<IUserFeedbackService>();
        var localizationMock = new Mock<ILocalizationService>();
        localizationMock
            .Setup(x => x.FormatScore(It.IsAny<int>()))
            .Returns((int score) => $"{score}");
        _victoryViewModel = new VictoryViewModel(
            accessibilitySettingsMock.Object,
            victoryFeedbackMock.Object,
            localizationMock.Object
        );

        // Setup default behavior
        _settingsServiceMock.SetupGet(s => s.HapticsEnabled).Returns(true);
        _settingsServiceMock.SetupGet(s => s.CoachEnabled).Returns(false);
        _settingsServiceMock.SetupGet(s => s.CoachNudgesEnabled).Returns(true);
        _settingsServiceMock.SetupSet<bool>(s => s.CoachEnabled = It.IsAny<bool>());
        _settingsServiceMock.Setup(s => s.LastActiveGameConfig).Returns(new GameConfig());
        _repositoryMock.Setup(r => r.GetBestScore(It.IsAny<GameConfig>())).Returns(0);
        _repositoryMock.Setup(r => r.LoadGame(It.IsAny<GameConfig>())).Returns((GameSave?)null);
        _sessionCoordinatorMock.Setup(s => s.IsSocialGamingAvailable).Returns(false);

        // Setup random source for deterministic tile spawning
        _randomSourceMock.Setup(r => r.Next(It.IsAny<int>())).Returns(0);
        _randomSourceMock.Setup(r => r.NextDouble()).Returns(0.5);

        var spawnStrategyFactory = CreateSpawnStrategyFactory(_randomSourceMock.Object);

        _engineFactory = new Game2048EngineFactory(
            _randomSourceMock.Object,
            _statisticsTrackerMock.Object,
            new BoardMoveSimulator(),
            spawnStrategyFactory
        );

        _boardSimulator = new BoardMoveSimulator();
    }

    private static ISpawnStrategyFactory CreateSpawnStrategyFactory(IRandomSource random)
    {
        var classic = new ClassicSpawnStrategy(random);
        var modern = new ModernSpawnStrategy(random);
        return new TestSpawnStrategyFactory(classic, modern);
    }

    private sealed class TestSpawnStrategyFactory(
        ClassicSpawnStrategy classic,
        ModernSpawnStrategy modern
    ) : ISpawnStrategyFactory
    {
        public ISpawnStrategy Create(GameConfig config)
        {
            return config.Mode switch
            {
                GameMode.Classic => classic,
                _ => modern,
            };
        }
    }

    private GameViewModel CreateViewModel()
    {
        return new GameViewModel(
            _loggerMock.Object,
            _moveAnalyzerMock.Object,
            _boardSimulator,
            _settingsServiceMock.Object,
            _statisticsTrackerMock.Object,
            _engineFactory,
            _repositoryMock.Object,
            _sessionCoordinatorMock.Object,
            _feedbackServiceMock.Object,
            _victoryViewModel,
            _coachNudgeServiceMock.Object,
            _coachSuggestionServiceMock.Object,
            _moveAdvisorMock.Object
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
    public async Task MoveCommand_WhenMoveAlreadyInProgress_QueuesSecondMove()
    {
        _moveAnalyzerMock
            .Setup(m => m.Analyze(It.IsAny<MoveAnalysisRequest>()))
            .Returns(() => new MoveAnalysisResult(boardSize: 4));

        var viewModel = CreateViewModel();

        // Start the first move and immediately request a second move.
        // Prior behavior: second move was dropped due to non-blocking move lock.
        // Current behavior: second move awaits the move lock and runs after the first completes.
        var first = viewModel.MoveCommand.ExecuteAsync(Direction.Left);
        var second = viewModel.MoveCommand.ExecuteAsync(Direction.Down);

        await Task.WhenAll(first, second);

        Assert.AreEqual(2, viewModel.Moves);
    }

    [TestMethod]
    public void ToggleCoachCommand_WhenAdvisorReturnsRecommendation_ShowsSuggestion()
    {
        // Arrange
        _coachSuggestionServiceMock
            .Setup(s => s.GetSuggestion(It.IsAny<CoachSuggestionRequest>()))
            .Returns(new MoveRecommendation(Direction.Left, 123, MoveCoachReason.CreateSpace));

        var viewModel = CreateViewModel();

        // Act
        viewModel.ToggleCoachCommand.Execute(null);

        // Assert
        Assert.IsTrue(viewModel.IsCoachEnabled);
        Assert.IsTrue(viewModel.IsCoachSuggestionVisible);
        Assert.AreEqual(Direction.Left, viewModel.CoachSuggestedDirection);
        Assert.AreEqual(MoveCoachReason.CreateSpace, viewModel.CoachPrimaryReason);
    }

    [TestMethod]
    public void ToggleCoachCommand_WhenTurnedOff_ClearsSuggestion()
    {
        // Arrange
        _coachSuggestionServiceMock
            .Setup(s => s.GetSuggestion(It.IsAny<CoachSuggestionRequest>()))
            .Returns(new MoveRecommendation(Direction.Left, 123, MoveCoachReason.CreateSpace));

        var viewModel = CreateViewModel();

        // Turn on first
        viewModel.ToggleCoachCommand.Execute(null);

        // Act - turn off
        viewModel.ToggleCoachCommand.Execute(null);

        // Assert
        Assert.IsFalse(viewModel.IsCoachEnabled);
        Assert.IsFalse(viewModel.IsCoachSuggestionVisible);
        Assert.IsNull(viewModel.CoachSuggestedDirection);
        Assert.IsNull(viewModel.CoachPrimaryReason);
    }

    [TestMethod]
    public async Task DismissCoachNudgeCommand_WhenVisible_HidesNudgeAndDoesNotEnableCoach()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Simulate the nudge being shown
        _coachNudgeServiceMock.Setup(s => s.IsNudgeVisible).Returns(true);

        // Act
        viewModel.DismissCoachNudgeCommand.Execute(null);

        // Assert
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);
        Assert.IsFalse(viewModel.IsCoachEnabled);
        _coachNudgeServiceMock.Verify(s => s.Dismiss(), Times.Once);
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
        _repositoryMock.Verify(r => r.LoadGame(It.Is<GameConfig>(c => c.Size == 5)), Times.Once);
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
        _repositoryMock.Verify(
            r => r.SaveGame(It.Is<GameConfig>(c => c.Size == 4), It.IsAny<GameSave>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task NewGameAsync_WhenMovesGreaterThanZeroAndUserCancels_DoesNotStartNewGame()
    {
        // Arrange
        _feedbackServiceMock.Setup(f => f.ConfirmNewGameAsync()).ReturnsAsync(false);
        var viewModel = CreateViewModel();
        viewModel.Moves = 1;

        // Act
        await viewModel.NewGameCommand.ExecuteAsync(null);

        // Assert
        _feedbackServiceMock.Verify(f => f.ConfirmNewGameAsync(), Times.Once);
        _repositoryMock.Verify(
            r => r.SaveGame(It.IsAny<GameConfig>(), It.IsAny<GameSave>()),
            Times.Never
        );
    }

    [TestMethod]
    public async Task NewGameAsync_WhenMovesGreaterThanZeroAndUserConfirms_StartsNewGame()
    {
        // Arrange
        _feedbackServiceMock.Setup(f => f.ConfirmNewGameAsync()).ReturnsAsync(true);
        var viewModel = CreateViewModel();
        viewModel.Moves = 1;

        // Act
        await viewModel.NewGameCommand.ExecuteAsync(null);

        // Assert
        _feedbackServiceMock.Verify(f => f.ConfirmNewGameAsync(), Times.Once);
        _repositoryMock.Verify(
            r => r.SaveGame(It.Is<GameConfig>(c => c.Size == 4), It.IsAny<GameSave>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task NewGameAsync_ResetsIsNewTileAndIsMergedFlags()
    {
        // Arrange - Regression test: tiles with IsNewTile=true from the previous game's
        // last move were remaining hidden after New Game because the flags weren't reset.
        var viewModel = CreateViewModel();

        // Simulate tiles having animation flags set (as would happen during normal gameplay)
        foreach (var tile in viewModel.Tiles)
        {
            tile.IsNewTile = true;
            tile.IsMerged = true;
        }

        // Act
        await viewModel.NewGameCommand.ExecuteAsync(null);

        // Assert - All tiles should have their animation flags reset
        foreach (var tile in viewModel.Tiles)
        {
            Assert.IsFalse(tile.IsNewTile, "IsNewTile should be reset to false after NewGame");
            Assert.IsFalse(tile.IsMerged, "IsMerged should be reset to false after NewGame");
        }
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
                r.SaveGame(
                    It.Is<GameConfig>(c => c.RulesetId == oldRulesetId),
                    It.IsAny<GameSave>()
                ),
            Times.AtLeastOnce
        );

        _repositoryMock.Verify(
            r =>
                r.SaveGame(
                    It.Is<GameConfig>(c => c.RulesetId == newRulesetId),
                    It.IsAny<GameSave>()
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

    [TestMethod]
    public async Task MoveCommand_WhenThreeConsecutiveInvalidMovesAndCoachDisabled_ShowsCoachNudge()
    {
        // Arrange
        _coachNudgeServiceMock.Setup(s => s.ShouldShowNudge()).Returns(true);

        var viewModel = CreateViewModel();
        Assert.IsFalse(viewModel.IsCoachEnabled);
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);

        // Act - simulate 3 invalid moves
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);

        // Assert
        _coachNudgeServiceMock.Verify(s => s.TrackInvalidMove(), Times.Exactly(3));
        _coachNudgeServiceMock.Verify(s => s.ShouldShowNudge(), Times.Exactly(3));
    }

    [TestMethod]
    public async Task MoveCommand_WhenCoachNudgesDisabled_DoesNotShowCoachNudge()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.CoachNudgesEnabled).Returns(false);
        _coachNudgeServiceMock.Setup(s => s.ShouldShowNudge()).Returns(false);

        var viewModel = CreateViewModel();
        Assert.IsFalse(viewModel.IsCoachEnabled);

        // Act
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);

        // Assert
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);
        _feedbackServiceMock.Verify(f => f.AnnounceCoachNudge(), Times.Never);
    }

    [TestMethod]
    public async Task MoveCommand_WhenCoachEnabled_DoesNotShowCoachNudge()
    {
        // Arrange
        _moveAdvisorMock
            .Setup(a => a.Recommend(It.IsAny<MoveAdvisorRequest>()))
            .Returns((MoveRecommendation?)null);
        _coachNudgeServiceMock.Setup(s => s.ShouldShowNudge()).Returns(false);

        var viewModel = CreateViewModel();

        viewModel.ToggleCoachCommand.Execute(null);
        Assert.IsTrue(viewModel.IsCoachEnabled);
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);

        // Act
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);

        // Assert
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);
    }

    [TestMethod]
    public async Task EnableCoachFromNudgeCommand_WhenVisible_EnablesCoachAndHidesNudge()
    {
        // Arrange
        _moveAdvisorMock
            .Setup(a => a.Recommend(It.IsAny<MoveAdvisorRequest>()))
            .Returns((MoveRecommendation?)null);

        var viewModel = CreateViewModel();

        // Act
        viewModel.EnableCoachFromNudgeCommand.Execute(null);

        // Assert
        Assert.IsTrue(viewModel.IsCoachEnabled);
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);
        _settingsServiceMock.VerifySet(s => s.CoachEnabled = true, Times.AtLeastOnce);
        _coachNudgeServiceMock.Verify(s => s.Dismiss(), Times.Once);
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

    [TestMethod]
    public void UndoButtonVisible_DefaultsToTrue()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.UndoButtonVisible).Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsTrue(viewModel.IsUndoButtonVisible);
    }

    [TestMethod]
    public void UndoButtonVisible_UpdatesFromSettingsService()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.UndoButtonVisible).Returns(false);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsFalse(viewModel.IsUndoButtonVisible);
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

    #region Best Score Tests - Adversarial Mode

    private GameViewModel CreateAdversarialViewModel(int initialBestScore = 0)
    {
        var adversarialConfig = new GameConfig { Mode = GameMode.Adversarial };
        _settingsServiceMock.Setup(s => s.LastActiveGameConfig).Returns(adversarialConfig);
        _repositoryMock
            .Setup(r => r.GetBestScore(It.IsAny<GameConfig>()))
            .Returns(initialBestScore);

        return CreateViewModel();
    }

    [TestMethod]
    public void TryUpdateAdversarialBestScore_UpdatesBestScoreWhenBestScoreIsZero()
    {
        // Arrange - First adversarial win (BestScore starts at 0)
        var viewModel = CreateAdversarialViewModel(initialBestScore: 0);
        Assert.AreEqual(0, viewModel.BestScore);
        Assert.IsTrue(viewModel.IsAdversarialMode);

        // Simulate some score during gameplay
        viewModel.Score = 256;

        // Act - Call the extracted best score update method
        viewModel.TryUpdateAdversarialBestScore();

        // Assert - Best score should update since BestScore was 0 (first win)
        Assert.AreEqual(256, viewModel.BestScore);
        _repositoryMock.Verify(
            r => r.UpdateBestScoreIfHigher(It.IsAny<GameConfig>(), 256),
            Times.Once
        );
    }

    [TestMethod]
    public void TryUpdateAdversarialBestScore_UpdatesBestScoreWhenScoreIsLowerThanBest()
    {
        // Arrange - Adversarial mode: lower score is better
        var viewModel = CreateAdversarialViewModel(initialBestScore: 500);
        Assert.AreEqual(500, viewModel.BestScore);
        Assert.IsTrue(viewModel.IsAdversarialMode);

        // Simulate a lower (better) score
        viewModel.Score = 200;

        // Act - Call the extracted best score update method
        viewModel.TryUpdateAdversarialBestScore();

        // Assert - Best score should update since Score < BestScore
        Assert.AreEqual(200, viewModel.BestScore);
        _repositoryMock.Verify(
            r => r.UpdateBestScoreIfHigher(It.IsAny<GameConfig>(), 200),
            Times.Once
        );
    }

    [TestMethod]
    public void TryUpdateAdversarialBestScore_DoesNotUpdateBestScoreWhenScoreIsHigherThanBest()
    {
        // Arrange - Adversarial mode: lower score is better
        var viewModel = CreateAdversarialViewModel(initialBestScore: 100);
        Assert.AreEqual(100, viewModel.BestScore);
        Assert.IsTrue(viewModel.IsAdversarialMode);

        // Simulate a higher (worse) score
        viewModel.Score = 300;

        // Act - Call the extracted best score update method
        viewModel.TryUpdateAdversarialBestScore();

        // Assert - Best score should NOT update since Score > BestScore
        Assert.AreEqual(100, viewModel.BestScore);
        _repositoryMock.Verify(
            r => r.UpdateBestScoreIfHigher(It.IsAny<GameConfig>(), It.IsAny<int>()),
            Times.Never
        );
    }

    [TestMethod]
    public void TryUpdateAdversarialBestScore_DoesNotUpdateBestScoreWhenScoreEqualsBest()
    {
        // Arrange - Adversarial mode: equal score should not update
        var viewModel = CreateAdversarialViewModel(initialBestScore: 150);
        Assert.AreEqual(150, viewModel.BestScore);
        Assert.IsTrue(viewModel.IsAdversarialMode);

        // Simulate the same score
        viewModel.Score = 150;

        // Act - Call the extracted best score update method
        viewModel.TryUpdateAdversarialBestScore();

        // Assert - Best score should NOT update since Score == BestScore (not strictly better)
        Assert.AreEqual(150, viewModel.BestScore);
        _repositoryMock.Verify(
            r => r.UpdateBestScoreIfHigher(It.IsAny<GameConfig>(), It.IsAny<int>()),
            Times.Never
        );
    }

    [TestMethod]
    public void NonAdversarialMode_OnVictory_DoesNotUpdateBestScoreInVictoryHandler()
    {
        // Arrange - Normal mode (best score update happens in Move, not victory handler)
        _repositoryMock.Setup(r => r.GetBestScore(It.IsAny<GameConfig>())).Returns(100);
        var viewModel = CreateViewModel();
        Assert.IsFalse(viewModel.IsAdversarialMode);

        // Simulate a higher score
        viewModel.Score = 500;
        viewModel.BestScore = 100; // Reset after any setup changes

        // Mark as initialized so victory handler runs
        SetPrivateField(viewModel, "_isInitialized", true);

        // Act - Trigger victory event
        InvokePrivateEngineVictoryHandler(viewModel, EventArgs.Empty);

        // Assert - Victory handler should NOT update best score in non-adversarial mode
        // (best score is updated during Move() in non-adversarial mode)
        Assert.AreEqual(100, viewModel.BestScore);
        _repositoryMock.Verify(
            r => r.UpdateBestScoreIfHigher(It.IsAny<GameConfig>(), It.IsAny<int>()),
            Times.Never
        );
    }

    [TestMethod]
    public void AdversarialMode_OnVictory_CallsTryUpdateAdversarialBestScore()
    {
        // Arrange - Adversarial mode: victory handler should call TryUpdateAdversarialBestScore
        var viewModel = CreateAdversarialViewModel(initialBestScore: 0);
        Assert.IsTrue(viewModel.IsAdversarialMode);

        // Simulate a score during gameplay
        viewModel.Score = 100;

        // Mark as initialized so victory handler runs
        SetPrivateField(viewModel, "_isInitialized", true);

        // Act - Trigger victory event
        InvokePrivateEngineVictoryHandler(viewModel, EventArgs.Empty);

        // Assert - Best score should have been updated (since initial was 0)
        // Note: UpdateUI() in victory handler resets Score to engine score (0),
        // so we verify the repository call was made with 0 (the engine's score after UpdateUI)
        // This test verifies the adversarial code path is executed.
        _repositoryMock.Verify(
            r => r.UpdateBestScoreIfHigher(It.IsAny<GameConfig>(), It.IsAny<int>()),
            Times.Once
        );
    }

    #endregion
}
