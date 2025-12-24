using Moq;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for SettingsViewModel.
/// </summary>
[TestClass]
public class SettingsViewModelTests
{
    [TestMethod]
    public void Constructor_LoadsAnimationsEnabledFromService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);

        // Act
        var viewModel = new SettingsViewModel(settingsServiceMock.Object);

        // Assert
        Assert.IsTrue(viewModel.AnimationsEnabled);
    }

    [TestMethod]
    public void Constructor_LoadsAnimationSpeedFromService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.5);

        // Act
        var viewModel = new SettingsViewModel(settingsServiceMock.Object);

        // Assert
        Assert.AreEqual(1.5, viewModel.AnimationSpeed);
    }

    [TestMethod]
    public void AnimationsEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        var viewModel = new SettingsViewModel(settingsServiceMock.Object);

        // Act
        viewModel.AnimationsEnabled = false;

        // Assert
        settingsServiceMock.VerifySet(s => s.AnimationsEnabled = false, Times.Once);
    }

    [TestMethod]
    public void AnimationSpeed_WhenChanged_UpdatesService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        var viewModel = new SettingsViewModel(settingsServiceMock.Object);

        // Act
        viewModel.AnimationSpeed = 0.5;

        // Assert
        settingsServiceMock.VerifySet(s => s.AnimationSpeed = 0.5, Times.Once);
    }
}
