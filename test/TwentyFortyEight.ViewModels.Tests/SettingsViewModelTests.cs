using CommunityToolkit.Mvvm.Messaging;
using Moq;
using TwentyFortyEight.ViewModels;
using TwentyFortyEight.ViewModels.Messages;
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
        settingsServiceMock.Setup(s => s.CoachEnabled).Returns(false);
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
        settingsServiceMock.Setup(s => s.CoachEnabled).Returns(false);
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
        settingsServiceMock.Setup(s => s.CoachEnabled).Returns(false);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);
        SettingsViewModel viewModel = new(settingsServiceMock.Object, hapticServiceMock.Object);

        // Act
        viewModel.HapticsEnabled = false;

        // Assert
        settingsServiceMock.VerifySet(s => s.HapticsEnabled = false, Times.Once);
    }

    [TestMethod]
    public void CoachEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        Mock<ISettingsService> settingsServiceMock = new();
        Mock<IHapticService> hapticServiceMock = new();
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.CoachEnabled).Returns(false);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);
        SettingsViewModel viewModel = new(settingsServiceMock.Object, hapticServiceMock.Object);

        // Act
        viewModel.CoachEnabled = true;

        // Assert
        settingsServiceMock.VerifySet(s => s.CoachEnabled = true, Times.Once);
    }

    [TestMethod]
    public void CoachNudgesEnabled_WhenChanged_SendsMessageAndUpdatesService()
    {
        // Arrange
        Mock<ISettingsService> settingsServiceMock = new();
        Mock<IHapticService> hapticServiceMock = new();
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.CoachEnabled).Returns(false);
        settingsServiceMock.Setup(s => s.CoachNudgesEnabled).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);

        SettingsViewModel viewModel = new(settingsServiceMock.Object, hapticServiceMock.Object);

        bool? receivedValue = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<CoachNudgesEnabledChangedMessage>(
            recipient,
            (_, message) => receivedValue = message.IsEnabled
        );

        // Act
        viewModel.CoachNudgesEnabled = false;

        // Assert
        settingsServiceMock.VerifySet(s => s.CoachNudgesEnabled = false, Times.Once);
        Assert.IsNotNull(receivedValue);
        Assert.IsFalse(receivedValue.Value);

        WeakReferenceMessenger.Default.UnregisterAll(recipient);
    }

    [TestMethod]
    public void Constructor_LoadsUndoButtonVisibleFromService()
    {
        // Arrange
        Mock<ISettingsService> settingsServiceMock = new();
        Mock<IHapticService> hapticServiceMock = new();
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.CoachEnabled).Returns(false);
        settingsServiceMock.Setup(s => s.UndoButtonVisible).Returns(false);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);

        // Act
        SettingsViewModel viewModel = new(settingsServiceMock.Object, hapticServiceMock.Object);

        // Assert
        Assert.IsFalse(viewModel.UndoButtonVisible);
    }

    [TestMethod]
    public void UndoButtonVisible_WhenChanged_SendsMessageAndUpdatesService()
    {
        // Arrange
        Mock<ISettingsService> settingsServiceMock = new();
        Mock<IHapticService> hapticServiceMock = new();
        settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        settingsServiceMock.Setup(s => s.CoachEnabled).Returns(false);
        settingsServiceMock.Setup(s => s.UndoButtonVisible).Returns(true);
        hapticServiceMock.Setup(h => h.IsSupported).Returns(true);

        SettingsViewModel viewModel = new(settingsServiceMock.Object, hapticServiceMock.Object);

        bool? receivedValue = null;
        object recipient = new();
        WeakReferenceMessenger.Default.Register<UndoButtonVisibilityChangedMessage>(
            recipient,
            (_, message) => receivedValue = message.IsVisible
        );

        // Act
        viewModel.UndoButtonVisible = false;

        // Assert
        settingsServiceMock.VerifySet(s => s.UndoButtonVisible = false, Times.Once);
        Assert.IsNotNull(receivedValue);
        Assert.IsFalse(receivedValue.Value);

        WeakReferenceMessenger.Default.UnregisterAll(recipient);
    }
}
