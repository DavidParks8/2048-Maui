using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using TwentyFortyEight.ViewModels.Messages;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels.Tests;

/// <summary>
/// Unit tests for SettingsViewModel.
/// </summary>
[TestClass]
public class SettingsViewModelTests
{
    private static SettingsViewModel CreateViewModel(
        ISettingsService settingsServiceMock,
        IHapticService hapticServiceMock,
        IMessenger? messenger = null
    )
    {
        return new SettingsViewModel(
            settingsServiceMock,
            hapticServiceMock,
            messenger ?? new WeakReferenceMessenger()
        );
    }

    [TestMethod]
    public void Constructor_LoadsHapticsEnabledFromService()
    {
        // Arrange
        ISettingsService settingsServiceMock = Substitute.For<ISettingsService>();
        IHapticService hapticServiceMock = Substitute.For<IHapticService>();
        settingsServiceMock.HapticsEnabled.Returns(false);
        settingsServiceMock.CoachEnabled.Returns(false);
        hapticServiceMock.IsSupported.Returns(true);

        // Act
        SettingsViewModel viewModel = CreateViewModel(settingsServiceMock, hapticServiceMock);

        // Assert
        Assert.IsFalse(viewModel.HapticsEnabled);
    }

    [TestMethod]
    public void IsHapticsSupported_ReturnsValueFromHapticService()
    {
        // Arrange
        ISettingsService settingsServiceMock = Substitute.For<ISettingsService>();
        IHapticService hapticServiceMock = Substitute.For<IHapticService>();
        settingsServiceMock.HapticsEnabled.Returns(true);
        settingsServiceMock.CoachEnabled.Returns(false);
        hapticServiceMock.IsSupported.Returns(false);

        // Act
        SettingsViewModel viewModel = CreateViewModel(settingsServiceMock, hapticServiceMock);

        // Assert
        Assert.IsFalse(viewModel.IsHapticsSupported);
    }

    [TestMethod]
    public void HapticsEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        ISettingsService settingsServiceMock = Substitute.For<ISettingsService>();
        IHapticService hapticServiceMock = Substitute.For<IHapticService>();
        settingsServiceMock.HapticsEnabled.Returns(true);
        settingsServiceMock.CoachEnabled.Returns(false);
        hapticServiceMock.IsSupported.Returns(true);
        SettingsViewModel viewModel = CreateViewModel(settingsServiceMock, hapticServiceMock);

        // Act
        viewModel.HapticsEnabled = false;

        // Assert
        settingsServiceMock.Received(1).HapticsEnabled = false;
    }

    [TestMethod]
    public void CoachEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        ISettingsService settingsServiceMock = Substitute.For<ISettingsService>();
        IHapticService hapticServiceMock = Substitute.For<IHapticService>();
        settingsServiceMock.HapticsEnabled.Returns(true);
        settingsServiceMock.CoachEnabled.Returns(false);
        hapticServiceMock.IsSupported.Returns(true);
        SettingsViewModel viewModel = CreateViewModel(settingsServiceMock, hapticServiceMock);

        // Act
        viewModel.CoachEnabled = true;

        // Assert
        settingsServiceMock.Received(1).CoachEnabled = true;
    }

    [TestMethod]
    public void CoachNudgesEnabled_WhenChanged_SendsMessageAndUpdatesService()
    {
        // Arrange
        ISettingsService settingsServiceMock = Substitute.For<ISettingsService>();
        IHapticService hapticServiceMock = Substitute.For<IHapticService>();
        settingsServiceMock.HapticsEnabled.Returns(true);
        settingsServiceMock.CoachEnabled.Returns(false);
        settingsServiceMock.CoachNudgesEnabled.Returns(true);
        hapticServiceMock.IsSupported.Returns(true);

        var messenger = new WeakReferenceMessenger();
        SettingsViewModel viewModel = CreateViewModel(
            settingsServiceMock,
            hapticServiceMock,
            messenger
        );

        bool? receivedValue = null;
        object recipient = new();
        messenger.Register<CoachNudgesEnabledChangedMessage>(
            recipient,
            (_, message) => receivedValue = message.IsEnabled
        );

        // Act
        viewModel.CoachNudgesEnabled = false;

        // Assert
        settingsServiceMock.Received(1).CoachNudgesEnabled = false;
        Assert.IsNotNull(receivedValue);
        Assert.IsFalse(receivedValue.Value);

        messenger.UnregisterAll(recipient);
    }

    [TestMethod]
    public void Constructor_LoadsUndoButtonVisibleFromService()
    {
        // Arrange
        ISettingsService settingsServiceMock = Substitute.For<ISettingsService>();
        IHapticService hapticServiceMock = Substitute.For<IHapticService>();
        settingsServiceMock.HapticsEnabled.Returns(true);
        settingsServiceMock.CoachEnabled.Returns(false);
        settingsServiceMock.UndoButtonVisible.Returns(false);
        hapticServiceMock.IsSupported.Returns(true);

        // Act
        SettingsViewModel viewModel = CreateViewModel(settingsServiceMock, hapticServiceMock);

        // Assert
        Assert.IsFalse(viewModel.UndoButtonVisible);
    }

    [TestMethod]
    public void UndoButtonVisible_WhenChanged_SendsMessageAndUpdatesService()
    {
        // Arrange
        ISettingsService settingsServiceMock = Substitute.For<ISettingsService>();
        IHapticService hapticServiceMock = Substitute.For<IHapticService>();
        settingsServiceMock.HapticsEnabled.Returns(true);
        settingsServiceMock.CoachEnabled.Returns(false);
        settingsServiceMock.UndoButtonVisible.Returns(true);
        hapticServiceMock.IsSupported.Returns(true);

        var messenger = new WeakReferenceMessenger();
        SettingsViewModel viewModel = CreateViewModel(
            settingsServiceMock,
            hapticServiceMock,
            messenger
        );

        bool? receivedValue = null;
        object recipient = new();
        messenger.Register<UndoButtonVisibilityChangedMessage>(
            recipient,
            (_, message) => receivedValue = message.IsVisible
        );

        // Act
        viewModel.UndoButtonVisible = false;

        // Assert
        settingsServiceMock.Received(1).UndoButtonVisible = false;
        Assert.IsNotNull(receivedValue);
        Assert.IsFalse(receivedValue.Value);

        messenger.UnregisterAll(recipient);
    }
}
