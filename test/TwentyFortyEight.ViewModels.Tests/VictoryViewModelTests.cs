using NSubstitute;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

[TestClass]
public class VictoryViewModelTests
{
    private IAccessibilitySettingsService _accessibilitySettingsMock = null!;
    private IUserFeedbackService _userFeedbackMock = null!;
    private ILocalizationService _localizationMock = null!;
    private VictoryViewModel _viewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _accessibilitySettingsMock = Substitute.For<IAccessibilitySettingsService>();
        _userFeedbackMock = Substitute.For<IUserFeedbackService>();
        _localizationMock = Substitute.For<ILocalizationService>();
        _localizationMock
            .FormatScore(Arg.Any<int>())
            .Returns(callInfo => $"Score: {callInfo.Arg<int>()}");
        _localizationMock.GetVictorySubtitle(false).Returns("You reached 2048!");
        _localizationMock.GetVictorySubtitle(true).Returns("You blocked 2048!");
        _viewModel = new VictoryViewModel(
            _accessibilitySettingsMock,
            _userFeedbackMock,
            _localizationMock
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
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 5000);

        // Assert
        Assert.IsTrue(_viewModel.State.IsActive);
        Assert.IsTrue(_viewModel.State.IsModalVisible);
        Assert.AreEqual(5000, _viewModel.State.Score);

        _userFeedbackMock.Received(1).PerformVictoryHaptic();
        _userFeedbackMock.Received(1).AnnounceWin();
    }

    [TestMethod]
    public void TriggerVictory_WithoutReduceMotion_StartsAnimationSequence()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(false);
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
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(false);
        _viewModel.TriggerVictory(score: 2048);

        // Act
        _viewModel.ShowModal();

        // Assert
        Assert.IsTrue(_viewModel.State.IsModalVisible);
        _userFeedbackMock.Received(1).AnnounceWin();
    }

    [TestMethod]
    public void KeepPlayingCommand_ResetsStateAndRaisesEvent()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);
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
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);
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
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);

        // Assert
        Assert.IsTrue(_viewModel.ShouldReduceMotion);

        // Change mock behavior
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(false);

        // Assert
        Assert.IsFalse(_viewModel.ShouldReduceMotion);
    }

    [TestMethod]
    public void TriggerVictory_SetsWinningValue()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 4096, winningValue: 4096);

        // Assert
        Assert.AreEqual(4096, _viewModel.State.WinningValue);
    }

    [TestMethod]
    public void ScoreDisplayText_ReturnsLocalizedScore()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 12345);

        // Assert
        Assert.AreEqual("Score: 12345", _viewModel.ScoreDisplayText);
        _localizationMock.Received().FormatScore(12345);
    }

    [TestMethod]
    public void TriggerVictory_StandardMode_SetsIsAdversarialModeFalse()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 2048, isAdversarialMode: false);

        // Assert
        Assert.IsFalse(_viewModel.State.IsAdversarialMode);
    }

    [TestMethod]
    public void TriggerVictory_AdversarialMode_SetsIsAdversarialModeTrue()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 100, isAdversarialMode: true);

        // Assert
        Assert.IsTrue(_viewModel.State.IsAdversarialMode);
    }

    [TestMethod]
    public void VictorySubtitleText_StandardMode_ReturnsReachedSubtitle()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 2048, isAdversarialMode: false);

        // Assert
        Assert.AreEqual("You reached 2048!", _viewModel.VictorySubtitleText);
        _localizationMock.Received().GetVictorySubtitle(false);
    }

    [TestMethod]
    public void VictorySubtitleText_AdversarialMode_ReturnsBlockedSubtitle()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);

        // Act
        _viewModel.TriggerVictory(score: 100, isAdversarialMode: true);

        // Assert
        Assert.AreEqual("You blocked 2048!", _viewModel.VictorySubtitleText);
        _localizationMock.Received().GetVictorySubtitle(true);
    }

    [TestMethod]
    public void Reset_ClearsIsAdversarialMode()
    {
        // Arrange
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);
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
        _accessibilitySettingsMock.ShouldReduceMotion().Returns(true);
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
