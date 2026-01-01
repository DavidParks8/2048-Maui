using Moq;
using TwentyFortyEight.Core;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Models;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class VictoryViewModelTests
{
    private Mock<IReduceMotionService> _reduceMotionMock = null!;
    private Mock<IUserFeedbackService> _userFeedbackMock = null!;
    private Mock<ILocalizationService> _localizationMock = null!;
    private VictoryViewModel _viewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _reduceMotionMock = new Mock<IReduceMotionService>();
        _userFeedbackMock = new Mock<IUserFeedbackService>();
        _localizationMock = new Mock<ILocalizationService>();
        _localizationMock
            .Setup(x => x.FormatScore(It.IsAny<int>()))
            .Returns((int score) => $"Score: {score}");
        _viewModel = new VictoryViewModel(
            _reduceMotionMock.Object,
            _userFeedbackMock.Object,
            _localizationMock.Object
        );
    }

    [TestMethod]
    public void Constructor_InitializesStateToInactive()
    {
        Assert.IsFalse(_viewModel.State.IsActive);
        Assert.IsFalse(_viewModel.State.IsModalVisible);
        Assert.AreEqual(VictoryAnimationPhase.None, _viewModel.State.Phase);
    }

    [TestMethod]
    public void TriggerVictory_WithReduceMotion_SkipsAnimationAndShowsModal()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        var args = new VictoryEventArgs { WinningTileRow = 1, WinningTileColumn = 2 };

        // Act
        _viewModel.TriggerVictory(args, score: 5000);

        // Assert
        Assert.IsTrue(_viewModel.State.IsActive);
        Assert.IsTrue(_viewModel.State.IsModalVisible);
        Assert.AreEqual(VictoryAnimationPhase.ModalVisible, _viewModel.State.Phase);
        Assert.AreEqual(5000, _viewModel.State.Score);
        Assert.AreEqual(1, _viewModel.State.WinningTileRow);
        Assert.AreEqual(2, _viewModel.State.WinningTileColumn);

        _userFeedbackMock.Verify(x => x.PerformVictoryHaptic(), Times.Once);
        _userFeedbackMock.Verify(x => x.AnnounceWin(), Times.Once);
    }

    [TestMethod]
    public void TriggerVictory_WithoutReduceMotion_StartsAnimationSequence()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(false);
        var args = new VictoryEventArgs { WinningTileRow = 3, WinningTileColumn = 0 };
        VictoryAnimationStartEventArgs? receivedArgs = null;
        _viewModel.AnimationStartRequested += (_, e) => receivedArgs = e;

        // Act
        _viewModel.TriggerVictory(args, score: 8192);

        // Assert
        Assert.IsTrue(_viewModel.State.IsActive);
        Assert.IsFalse(_viewModel.State.IsModalVisible);
        Assert.AreEqual(VictoryAnimationPhase.Impact, _viewModel.State.Phase);
        Assert.AreEqual(0f, _viewModel.State.PhaseProgress);

        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(3, receivedArgs.WinningTileRow);
        Assert.AreEqual(0, receivedArgs.WinningTileColumn);
        Assert.AreEqual(8192, receivedArgs.Score);
    }

    [TestMethod]
    public void UpdateAnimationProgress_UpdatesStateProperties()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(false);
        var args = new VictoryEventArgs { WinningTileRow = 0, WinningTileColumn = 0 };
        _viewModel.TriggerVictory(args, score: 2048);

        // Act
        _viewModel.UpdateAnimationProgress(VictoryAnimationPhase.WarpTransition, 0.5f);

        // Assert
        Assert.AreEqual(VictoryAnimationPhase.WarpTransition, _viewModel.State.Phase);
        Assert.AreEqual(0.5f, _viewModel.State.PhaseProgress);
    }

    [TestMethod]
    public void ShowModal_SetsModalVisibleAndAnnouncesWin()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(false);
        var args = new VictoryEventArgs { WinningTileRow = 0, WinningTileColumn = 0 };
        _viewModel.TriggerVictory(args, score: 2048);

        // Act
        _viewModel.ShowModal();

        // Assert
        Assert.IsTrue(_viewModel.State.IsModalVisible);
        Assert.AreEqual(VictoryAnimationPhase.ModalVisible, _viewModel.State.Phase);
        _userFeedbackMock.Verify(x => x.AnnounceWin(), Times.Once);
    }

    [TestMethod]
    public void KeepPlayingCommand_ResetsStateAndRaisesEvent()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        var args = new VictoryEventArgs { WinningTileRow = 0, WinningTileColumn = 0 };
        _viewModel.TriggerVictory(args, score: 2048);

        bool keepPlayingRaised = false;
        bool animationStopRaised = false;
        _viewModel.KeepPlayingRequested += (_, _) => keepPlayingRaised = true;
        _viewModel.AnimationStopRequested += (_, _) => animationStopRaised = true;

        // Act
        _viewModel.KeepPlayingCommand.Execute(null);

        // Assert
        Assert.IsTrue(keepPlayingRaised);
        Assert.IsTrue(animationStopRaised);
        Assert.IsFalse(_viewModel.State.IsActive);
        Assert.IsFalse(_viewModel.State.IsModalVisible);
        Assert.AreEqual(VictoryAnimationPhase.None, _viewModel.State.Phase);
    }

    [TestMethod]
    public void NewGameCommand_ResetsStateAndRaisesEvent()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        var args = new VictoryEventArgs { WinningTileRow = 0, WinningTileColumn = 0 };
        _viewModel.TriggerVictory(args, score: 2048);

        bool newGameRaised = false;
        bool animationStopRaised = false;
        _viewModel.NewGameRequested += (_, _) => newGameRaised = true;
        _viewModel.AnimationStopRequested += (_, _) => animationStopRaised = true;

        // Act
        _viewModel.NewGameCommand.Execute(null);

        // Assert
        Assert.IsTrue(newGameRaised);
        Assert.IsTrue(animationStopRaised);
        Assert.IsFalse(_viewModel.State.IsActive);
        Assert.IsFalse(_viewModel.State.IsModalVisible);
    }

    [TestMethod]
    public void ShouldReduceMotion_DelegatesToService()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Assert
        Assert.IsTrue(_viewModel.ShouldReduceMotion);

        // Change mock behavior
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(false);

        // Assert
        Assert.IsFalse(_viewModel.ShouldReduceMotion);
    }

    [TestMethod]
    public void TriggerVictory_SetsWinningValue()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        var args = new VictoryEventArgs { WinningTileRow = 0, WinningTileColumn = 0 };

        // Act
        _viewModel.TriggerVictory(args, score: 4096, winningValue: 4096);

        // Assert
        Assert.AreEqual(4096, _viewModel.State.WinningValue);
    }

    [TestMethod]
    public void ScoreDisplayText_ReturnsLocalizedScore()
    {
        // Arrange
        _reduceMotionMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        var args = new VictoryEventArgs { WinningTileRow = 0, WinningTileColumn = 0 };

        // Act
        _viewModel.TriggerVictory(args, score: 12345);

        // Assert
        Assert.AreEqual("Score: 12345", _viewModel.ScoreDisplayText);
        _localizationMock.Verify(x => x.FormatScore(12345), Times.AtLeastOnce);
    }
}
