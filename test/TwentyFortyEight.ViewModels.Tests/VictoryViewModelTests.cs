using Moq;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class VictoryViewModelTests
{
    private Mock<IAccessibilitySettingsService> _accessibilitySettingsMock = null!;
    private Mock<IUserFeedbackService> _userFeedbackMock = null!;
    private Mock<ILocalizationService> _localizationMock = null!;
    private VictoryViewModel _viewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _accessibilitySettingsMock = new Mock<IAccessibilitySettingsService>();
        _userFeedbackMock = new Mock<IUserFeedbackService>();
        _localizationMock = new Mock<ILocalizationService>();
        _localizationMock
            .Setup(x => x.FormatScore(It.IsAny<int>()))
            .Returns((int score) => $"Score: {score}");
        _localizationMock.Setup(x => x.GetVictorySubtitle(false)).Returns("You reached 2048!");
        _localizationMock.Setup(x => x.GetVictorySubtitle(true)).Returns("You blocked 2048!");
        _viewModel = new VictoryViewModel(
            _accessibilitySettingsMock.Object,
            _userFeedbackMock.Object,
            _localizationMock.Object
        );
    }

    [TestMethod]
    public void Constructor_InitializesStateToInactive()
    {
        Assert.IsFalse(_viewModel.State.IsActive);
        Assert.IsFalse(_viewModel.State.IsModalVisible);
    }

    [TestMethod]
    public void TriggerVictory_WithReduceMotion_SkipsAnimationAndShowsModal()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 5000);

        // Assert
        Assert.IsTrue(_viewModel.State.IsActive);
        Assert.IsTrue(_viewModel.State.IsModalVisible);
        Assert.AreEqual(5000, _viewModel.State.Score);

        _userFeedbackMock.Verify(x => x.PerformVictoryHaptic(), Times.Once);
        _userFeedbackMock.Verify(x => x.AnnounceWin(), Times.Once);
    }

    [TestMethod]
    public void TriggerVictory_WithoutReduceMotion_StartsAnimationSequence()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(false);
        bool animationStartRaised = false;
        _viewModel.AnimationStartRequested += (_, _) => animationStartRaised = true;

        // Act
        _viewModel.TriggerVictory(score: 8192);

        // Assert
        Assert.IsTrue(_viewModel.State.IsActive);
        Assert.IsFalse(_viewModel.State.IsModalVisible);
        Assert.AreEqual(8192, _viewModel.State.Score);

        Assert.IsTrue(animationStartRaised);
    }

    [TestMethod]
    public void ShowModal_SetsModalVisibleAndAnnouncesWin()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(false);
        _viewModel.TriggerVictory(score: 2048);

        // Act
        _viewModel.ShowModal();

        // Assert
        Assert.IsTrue(_viewModel.State.IsModalVisible);
        _userFeedbackMock.Verify(x => x.AnnounceWin(), Times.Once);
    }

    [TestMethod]
    public void KeepPlayingCommand_ResetsStateAndRaisesEvent()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        _viewModel.TriggerVictory(score: 2048);

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
    }

    [TestMethod]
    public void NewGameCommand_ResetsStateAndRaisesEvent()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        _viewModel.TriggerVictory(score: 2048);

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
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Assert
        Assert.IsTrue(_viewModel.ShouldReduceMotion);

        // Change mock behavior
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(false);

        // Assert
        Assert.IsFalse(_viewModel.ShouldReduceMotion);
    }

    [TestMethod]
    public void TriggerVictory_SetsWinningValue()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 4096, winningValue: 4096);

        // Assert
        Assert.AreEqual(4096, _viewModel.State.WinningValue);
    }

    [TestMethod]
    public void ScoreDisplayText_ReturnsLocalizedScore()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 12345);

        // Assert
        Assert.AreEqual("Score: 12345", _viewModel.ScoreDisplayText);
        _localizationMock.Verify(x => x.FormatScore(12345), Times.AtLeastOnce);
    }

    [TestMethod]
    public void TriggerVictory_StandardMode_SetsIsAdversarialModeFalse()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 2048, isAdversarialMode: false);

        // Assert
        Assert.IsFalse(_viewModel.State.IsAdversarialMode);
    }

    [TestMethod]
    public void TriggerVictory_AdversarialMode_SetsIsAdversarialModeTrue()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 100, isAdversarialMode: true);

        // Assert
        Assert.IsTrue(_viewModel.State.IsAdversarialMode);
    }

    [TestMethod]
    public void VictorySubtitleText_StandardMode_ReturnsReachedSubtitle()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 2048, isAdversarialMode: false);

        // Assert
        Assert.AreEqual("You reached 2048!", _viewModel.VictorySubtitleText);
        _localizationMock.Verify(x => x.GetVictorySubtitle(false), Times.AtLeastOnce);
    }

    [TestMethod]
    public void VictorySubtitleText_AdversarialMode_ReturnsBlockedSubtitle()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 100, isAdversarialMode: true);

        // Assert
        Assert.AreEqual("You blocked 2048!", _viewModel.VictorySubtitleText);
        _localizationMock.Verify(x => x.GetVictorySubtitle(true), Times.AtLeastOnce);
    }

    [TestMethod]
    public void Reset_ClearsIsAdversarialMode()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        _viewModel.TriggerVictory(score: 100, isAdversarialMode: true);
        Assert.IsTrue(_viewModel.State.IsAdversarialMode);

        // Act
        _viewModel.KeepPlayingCommand.Execute(null);

        // Assert
        Assert.IsFalse(_viewModel.State.IsAdversarialMode);
    }

    [TestMethod]
    public void TriggerVictory_NotifiesVictorySubtitleTextChanged()
    {
        // Arrange
        _accessibilitySettingsMock.Setup(x => x.ShouldReduceMotion()).Returns(true);
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        _viewModel.TriggerVictory(score: 2048, isAdversarialMode: true);

        // Assert
        CollectionAssert.Contains(
            propertyChangedEvents,
            nameof(VictoryViewModel.VictorySubtitleText)
        );
    }
}
