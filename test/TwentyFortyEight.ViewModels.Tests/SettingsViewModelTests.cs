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
        var hapticServiceMock = new Mock<IHapticService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);

        // Act
        var viewModel = new SettingsViewModel(settingsServiceMock.Object, hapticServiceMock.Object);

        // Assert
        Assert.IsTrue(viewModel.AnimationsEnabled);
    }

    [TestMethod]
    public void Constructor_LoadsAnimationSpeedFromService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        var hapticServiceMock = new Mock<IHapticService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.5);
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);

        // Act
        var viewModel = new SettingsViewModel(settingsServiceMock.Object, hapticServiceMock.Object);

        // Assert
        Assert.AreEqual(1.5, viewModel.AnimationSpeed);
    }

    [TestMethod]
    public void Constructor_LoadsHapticsEnabledFromService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        var hapticServiceMock = new Mock<IHapticService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(false);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);

        // Act
        var viewModel = new SettingsViewModel(settingsServiceMock.Object, hapticServiceMock.Object);

        // Assert
        Assert.IsFalse(viewModel.HapticsEnabled);
    }

    [TestMethod]
    public void IsHapticsSupported_ReturnsValueFromHapticService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        var hapticServiceMock = new Mock<IHapticService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(false);

        // Act
        var viewModel = new SettingsViewModel(settingsServiceMock.Object, hapticServiceMock.Object);

        // Assert
        Assert.IsFalse(viewModel.IsHapticsSupported);
    }

    [TestMethod]
    public void AnimationsEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        var hapticServiceMock = new Mock<IHapticService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);
        var viewModel = new SettingsViewModel(settingsServiceMock.Object, hapticServiceMock.Object);

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
        var hapticServiceMock = new Mock<IHapticService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);
        var viewModel = new SettingsViewModel(settingsServiceMock.Object, hapticServiceMock.Object);

        // Act
        viewModel.AnimationSpeed = 0.5;

        // Assert
        settingsServiceMock.VerifySet(s => s.AnimationSpeed = 0.5, Times.Once);
    }

    [TestMethod]
    public void HapticsEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        var settingsServiceMock = new Mock<ISettingsService>();
        var hapticServiceMock = new Mock<IHapticService>();
        settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);
        var viewModel = new SettingsViewModel(settingsServiceMock.Object, hapticServiceMock.Object);

        // Act
        viewModel.HapticsEnabled = false;

        // Assert
        settingsServiceMock.VerifySet(s => s.HapticsEnabled = false, Times.Once);
    }
}
