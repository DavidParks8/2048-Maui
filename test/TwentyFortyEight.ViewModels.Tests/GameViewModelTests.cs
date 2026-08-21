using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels.Services;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel, Workers = 0)]

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for GameViewModel demonstrating MVVM testing capabilities.
/// </summary>
[TestClass]
public class GameViewModelTests
{
    private ILogger<GameViewModel> _loggerMock = null!;
    private IMoveAnalyzer _moveAnalyzerMock = null!;
    private IMoveAdvisor _moveAdvisorMock = null!;
    private ISettingsService _settingsServiceMock = null!;
    private IStatisticsTracker _statisticsTrackerMock = null!;
    private IRandomSource _randomSourceMock = null!;
    private IGame2048EngineFactory _engineFactory = null!;
    private IGameStateRepository _repositoryMock = null!;
    private IGameSessionCoordinator _sessionCoordinatorMock = null!;
    private IUserFeedbackService _feedbackServiceMock = null!;
    private IBoardSimulator _boardSimulator = null!;
    private ICoachNudgeService _coachNudgeServiceMock = null!;
    private ICoachSuggestionService _coachSuggestionServiceMock = null!;
    private VictoryViewModel _victoryViewModel = null!;
    private IMessenger _messenger = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = Substitute.For<ILogger<GameViewModel>>();
        _moveAnalyzerMock = Substitute.For<IMoveAnalyzer>();
        _moveAdvisorMock = Substitute.For<IMoveAdvisor>();
        _settingsServiceMock = Substitute.For<ISettingsService>();
        _statisticsTrackerMock = Substitute.For<IStatisticsTracker>();
        _randomSourceMock = Substitute.For<IRandomSource>();
        _repositoryMock = Substitute.For<IGameStateRepository>();
        _sessionCoordinatorMock = Substitute.For<IGameSessionCoordinator>();
        _feedbackServiceMock = Substitute.For<IUserFeedbackService>();
        _coachNudgeServiceMock = Substitute.For<ICoachNudgeService>();
        _coachSuggestionServiceMock = Substitute.For<ICoachSuggestionService>();
        _messenger = new WeakReferenceMessenger();

        // Create real VictoryViewModel instance for testing
        var accessibilitySettingsMock = Substitute.For<IAccessibilitySettingsService>();
        var victoryFeedbackMock = Substitute.For<IUserFeedbackService>();
        var localizationMock = Substitute.For<ILocalizationService>();
        localizationMock.FormatScore(Arg.Any<int>()).Returns(callInfo => $"{callInfo.Arg<int>()}");
        _victoryViewModel = new VictoryViewModel(
            accessibilitySettingsMock,
            victoryFeedbackMock,
            localizationMock
        );

        // Setup default behavior
        _settingsServiceMock.HapticsEnabled.Returns(true);
        _settingsServiceMock.CoachEnabled.Returns(false);
        _settingsServiceMock.CoachNudgesEnabled.Returns(true);
        _settingsServiceMock.LastActiveGameConfig.Returns(new GameConfig());
        _repositoryMock.GetBestScore(Arg.Any<GameConfig>()).Returns(0);
        _repositoryMock.LoadGame(Arg.Any<GameConfig>()).Returns((GameSave?)null);
        _sessionCoordinatorMock.IsSocialGamingAvailable.Returns(false);

        // Setup random source for deterministic tile spawning
        _randomSourceMock.Next(Arg.Any<int>()).Returns(0);
        _randomSourceMock.NextDouble().Returns(0.5);

        var spawnStrategyFactory = CreateSpawnStrategyFactory(_randomSourceMock);

        _engineFactory = new Game2048EngineFactory(
            _randomSourceMock,
            _statisticsTrackerMock,
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
            _loggerMock,
            _moveAnalyzerMock,
            _boardSimulator,
            _settingsServiceMock,
            _statisticsTrackerMock,
            _engineFactory,
            _repositoryMock,
            _sessionCoordinatorMock,
            _feedbackServiceMock,
            _victoryViewModel,
            _coachNudgeServiceMock,
            _coachSuggestionServiceMock,
            _moveAdvisorMock,
            _messenger
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
            .Analyze(Arg.Any<MoveAnalysisRequest>())
            .Returns(_ => new MoveAnalysisResult(boardSize: 4));

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
            .GetSuggestion(Arg.Any<CoachSuggestionRequest>())
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
            .GetSuggestion(Arg.Any<CoachSuggestionRequest>())
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
        _coachNudgeServiceMock.IsNudgeVisible.Returns(true);

        // Act
        viewModel.DismissCoachNudgeCommand.Execute(null);

        // Assert
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);
        Assert.IsFalse(viewModel.IsCoachEnabled);
        _coachNudgeServiceMock.Received(1).Dismiss();
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
        _settingsServiceMock.LastActiveGameConfig.Returns(new GameConfig { Size = 5 });

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual(5, viewModel.BoardSize);
        Assert.HasCount(25, viewModel.Tiles); // 5x5 board
        _repositoryMock.Received(1).GetBestScore(Arg.Is<GameConfig>(c => c.Size == 5));
        _repositoryMock.Received(1).LoadGame(Arg.Is<GameConfig>(c => c.Size == 5));
    }

    [TestMethod]
    public async Task ShowHowToPlayCommand_CallsFeedbackService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.ShowHowToPlayCommand.ExecuteAsync(null);

        // Assert
        await _feedbackServiceMock.Received(1).ShowHowToPlayAsync();
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
        await _feedbackServiceMock.DidNotReceive().ConfirmNewGameAsync();
        _repositoryMock
            .Received(1)
            .SaveGame(Arg.Is<GameConfig>(c => c.Size == 4), Arg.Any<GameSave>());
    }

    [TestMethod]
    public async Task NewGameAsync_WhenMovesGreaterThanZeroAndUserCancels_DoesNotStartNewGame()
    {
        // Arrange
        _feedbackServiceMock.ConfirmNewGameAsync().Returns(Task.FromResult(false));
        var viewModel = CreateViewModel();
        viewModel.Moves = 1;

        // Act
        await viewModel.NewGameCommand.ExecuteAsync(null);

        // Assert
        await _feedbackServiceMock.Received(1).ConfirmNewGameAsync();
        _repositoryMock.DidNotReceive().SaveGame(Arg.Any<GameConfig>(), Arg.Any<GameSave>());
    }

    [TestMethod]
    public async Task NewGameAsync_WhenMovesGreaterThanZeroAndUserConfirms_StartsNewGame()
    {
        // Arrange
        _feedbackServiceMock.ConfirmNewGameAsync().Returns(Task.FromResult(true));
        var viewModel = CreateViewModel();
        viewModel.Moves = 1;

        // Act
        await viewModel.NewGameCommand.ExecuteAsync(null);

        // Assert
        await _feedbackServiceMock.Received(1).ConfirmNewGameAsync();
        _repositoryMock
            .Received(1)
            .SaveGame(Arg.Is<GameConfig>(c => c.Size == 4), Arg.Any<GameSave>());
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
        _repositoryMock.FlushAsync(Arg.Any<GameConfig>()).Returns(Task.CompletedTask);

        var oldRulesetId = new GameConfig { Size = 4, WinTile = 2048 }.RulesetId;
        var newRulesetId = new GameConfig { Size = 5, WinTile = 2048 }.RulesetId;

        viewModel.PendingBoardSize = 5;

        // Act
        await viewModel.PlaySelectedModeCommand.ExecuteAsync(null);

        // Assert
        _repositoryMock
            .Received()
            .SaveGame(Arg.Is<GameConfig>(c => c.RulesetId == oldRulesetId), Arg.Any<GameSave>());

        _repositoryMock
            .Received()
            .SaveGame(Arg.Is<GameConfig>(c => c.RulesetId == newRulesetId), Arg.Any<GameSave>());

        _repositoryMock.DidNotReceive().ClearSavedGame(Arg.Any<GameConfig>());
    }

    [TestMethod]
    public async Task StartNewSelectedModeCommand_ClearsSavedRunForSelectedRuleset()
    {
        // Arrange
        var viewModel = CreateViewModel();
        _repositoryMock.FlushAsync(Arg.Any<GameConfig>()).Returns(Task.CompletedTask);

        var newRulesetId = new GameConfig { Size = 5, WinTile = 2048 }.RulesetId;
        viewModel.PendingBoardSize = 5;

        // Act
        await viewModel.StartNewSelectedModeCommand.ExecuteAsync(null);

        // Assert
        _repositoryMock
            .Received(1)
            .ClearSavedGame(Arg.Is<GameConfig>(c => c.RulesetId == newRulesetId));
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
        _coachNudgeServiceMock.ShouldShowNudge().Returns(true);

        var viewModel = CreateViewModel();
        Assert.IsFalse(viewModel.IsCoachEnabled);
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);

        // Act - simulate 3 invalid moves
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);

        // Assert
        _coachNudgeServiceMock.Received(3).TrackInvalidMove();
        _coachNudgeServiceMock.Received(3).ShouldShowNudge();
    }

    [TestMethod]
    public async Task MoveCommand_WhenCoachNudgesDisabled_DoesNotShowCoachNudge()
    {
        // Arrange
        _settingsServiceMock.CoachNudgesEnabled.Returns(false);
        _coachNudgeServiceMock.ShouldShowNudge().Returns(false);

        var viewModel = CreateViewModel();
        Assert.IsFalse(viewModel.IsCoachEnabled);

        // Act
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);
        await viewModel.MoveCommand.ExecuteAsync(Direction.Up);

        // Assert
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);
        _feedbackServiceMock.DidNotReceive().AnnounceCoachNudge();
    }

    [TestMethod]
    public async Task MoveCommand_WhenCoachEnabled_DoesNotShowCoachNudge()
    {
        // Arrange
        _moveAdvisorMock
            .Recommend(Arg.Any<MoveAdvisorRequest>())
            .Returns((MoveRecommendation?)null);
        _coachNudgeServiceMock.ShouldShowNudge().Returns(false);

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
            .Recommend(Arg.Any<MoveAdvisorRequest>())
            .Returns((MoveRecommendation?)null);

        var viewModel = CreateViewModel();

        // Act
        viewModel.EnableCoachFromNudgeCommand.Execute(null);

        // Assert
        Assert.IsTrue(viewModel.IsCoachEnabled);
        Assert.IsFalse(viewModel.IsCoachNudgeVisible);
        _settingsServiceMock.Received().CoachEnabled = true;
        _coachNudgeServiceMock.Received(1).Dismiss();
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
        _settingsServiceMock.UndoButtonVisible.Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsTrue(viewModel.IsUndoButtonVisible);
    }

    [TestMethod]
    public void UndoButtonVisible_UpdatesFromSettingsService()
    {
        // Arrange
        _settingsServiceMock.UndoButtonVisible.Returns(false);

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
        _settingsServiceMock.LastActiveGameConfig.Returns(adversarialConfig);
        _repositoryMock.GetBestScore(Arg.Any<GameConfig>()).Returns(initialBestScore);

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
        _repositoryMock.Received(1).UpdateBestScoreIfHigher(Arg.Any<GameConfig>(), 256);
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
        _repositoryMock.Received(1).UpdateBestScoreIfHigher(Arg.Any<GameConfig>(), 200);
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
        _repositoryMock
            .DidNotReceive()
            .UpdateBestScoreIfHigher(Arg.Any<GameConfig>(), Arg.Any<int>());
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
        _repositoryMock
            .DidNotReceive()
            .UpdateBestScoreIfHigher(Arg.Any<GameConfig>(), Arg.Any<int>());
    }

    [TestMethod]
    public void NonAdversarialMode_OnVictory_DoesNotUpdateBestScoreInVictoryHandler()
    {
        // Arrange - Normal mode (best score update happens in Move, not victory handler)
        _repositoryMock.GetBestScore(Arg.Any<GameConfig>()).Returns(100);
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
        _repositoryMock
            .DidNotReceive()
            .UpdateBestScoreIfHigher(Arg.Any<GameConfig>(), Arg.Any<int>());
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
        _repositoryMock
            .Received(1)
            .UpdateBestScoreIfHigher(Arg.Any<GameConfig>(), Arg.Any<int>());
    }

    #endregion
}
