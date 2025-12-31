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
    public void Constructor_LoadsHapticsEnabledFromService()
    {
        // Arrange
        Mock<ISettingsService> settingsServiceMock = new();
        Mock<IHapticService> hapticServiceMock = new();
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(false);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);

        // Act
        SettingsViewModel viewModel = new(settingsServiceMock.Object, hapticServiceMock.Object);

        // Assert
        Assert.IsFalse(viewModel.HapticsEnabled);
    }

    [TestMethod]
    public void IsHapticsSupported_ReturnsValueFromHapticService()
    {
        // Arrange
        Mock<ISettingsService> settingsServiceMock = new();
        Mock<IHapticService> hapticServiceMock = new();
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(false);

        // Act
        SettingsViewModel viewModel = new(settingsServiceMock.Object, hapticServiceMock.Object);

        // Assert
        Assert.IsFalse(viewModel.IsHapticsSupported);
    }

    [TestMethod]
    public void HapticsEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        Mock<ISettingsService> settingsServiceMock = new();
        Mock<IHapticService> hapticServiceMock = new();
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);
        SettingsViewModel viewModel = new(settingsServiceMock.Object, hapticServiceMock.Object);

        // Act
        viewModel.HapticsEnabled = false;

        // Assert
        settingsServiceMock.VerifySet(s => s.HapticsEnabled = false, Times.Once);
    }
}
