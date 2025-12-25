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
    private Mock<ISettingsService> _settingsServiceMock = null!;
    private Mock<IHapticService> _hapticServiceMock = null!;
    private Mock<IAdsService> _adsServiceMock = null!;
    private Mock<IInAppPurchaseService> _purchaseServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _hapticServiceMock = new Mock<IHapticService>();
        _adsServiceMock = new Mock<IAdsService>();
        _purchaseServiceMock = new Mock<IInAppPurchaseService>();

        // Default setup
        _settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);
        _settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.0);
        _settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(true);
        _settingsServiceMock.Setup(s => s.AdsRemoved).Returns(false);
        _hapticServiceMock.Setup(h => h.IsSupported).Returns(true);
        _adsServiceMock.Setup(a => a.IsSupported).Returns(false);
        _purchaseServiceMock.Setup(p => p.IsSupported).Returns(false);
        _purchaseServiceMock.Setup(p => p.RemoveAdsProductId).Returns("remove_ads");
    }

    private SettingsViewModel CreateViewModel()
    {
        return new SettingsViewModel(
            _settingsServiceMock.Object,
            _hapticServiceMock.Object,
            _adsServiceMock.Object,
            _purchaseServiceMock.Object);
    }

    [TestMethod]
    public void Constructor_LoadsAnimationsEnabledFromService()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.AnimationsEnabled).Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsTrue(viewModel.AnimationsEnabled);
    }

    [TestMethod]
    public void Constructor_LoadsAnimationSpeedFromService()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.AnimationSpeed).Returns(1.5);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual(1.5, viewModel.AnimationSpeed);
    }

    [TestMethod]
    public void Constructor_LoadsHapticsEnabledFromService()
    {
        // Arrange
        _settingsServiceMock.Setup(s => s.HapticsEnabled).Returns(false);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsFalse(viewModel.HapticsEnabled);
    }

    [TestMethod]
    public void IsHapticsSupported_ReturnsValueFromHapticService()
    {
        // Arrange
        _hapticServiceMock.Setup(h => h.IsSupported).Returns(false);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsFalse(viewModel.IsHapticsSupported);
    }

    [TestMethod]
    public void AnimationsEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AnimationsEnabled = false;

        // Assert
        _settingsServiceMock.VerifySet(s => s.AnimationsEnabled = false, Times.Once);
    }

    [TestMethod]
    public void AnimationSpeed_WhenChanged_UpdatesService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AnimationSpeed = 0.5;

        // Assert
        _settingsServiceMock.VerifySet(s => s.AnimationSpeed = 0.5, Times.Once);
    }

    [TestMethod]
    public void HapticsEnabled_WhenChanged_UpdatesService()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.HapticsEnabled = false;

        // Assert
        _settingsServiceMock.VerifySet(s => s.HapticsEnabled = false, Times.Once);
    }

    [TestMethod]
    public void AreAdsSupported_ReturnsValueFromAdsService()
    {
        // Arrange
        _adsServiceMock.Setup(a => a.IsSupported).Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsTrue(viewModel.AreAdsSupported);
    }

    [TestMethod]
    public void IsPurchaseSupported_ReturnsValueFromPurchaseService()
    {
        // Arrange
        _purchaseServiceMock.Setup(p => p.IsSupported).Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsTrue(viewModel.IsPurchaseSupported);
    }

    [TestMethod]
    public void ShowRemoveAdsSection_TrueWhenAdsAndPurchaseSupportedAndNotRemoved()
    {
        // Arrange
        _adsServiceMock.Setup(a => a.IsSupported).Returns(true);
        _purchaseServiceMock.Setup(p => p.IsSupported).Returns(true);
        _settingsServiceMock.Setup(s => s.AdsRemoved).Returns(false);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsTrue(viewModel.ShowRemoveAdsSection);
    }

    [TestMethod]
    public void ShowRemoveAdsSection_FalseWhenAdsNotSupported()
    {
        // Arrange
        _adsServiceMock.Setup(a => a.IsSupported).Returns(false);
        _purchaseServiceMock.Setup(p => p.IsSupported).Returns(true);
        _settingsServiceMock.Setup(s => s.AdsRemoved).Returns(false);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsFalse(viewModel.ShowRemoveAdsSection);
    }

    [TestMethod]
    public void ShowRemoveAdsSection_FalseWhenAdsAlreadyRemoved()
    {
        // Arrange
        _adsServiceMock.Setup(a => a.IsSupported).Returns(true);
        _purchaseServiceMock.Setup(p => p.IsSupported).Returns(true);
        _settingsServiceMock.Setup(s => s.AdsRemoved).Returns(true);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsFalse(viewModel.ShowRemoveAdsSection);
    }

    [TestMethod]
    public async Task PurchaseRemoveAdsCommand_WhenSuccessful_DisablesAds()
    {
        // Arrange
        _adsServiceMock.Setup(a => a.IsSupported).Returns(true);
        _purchaseServiceMock.Setup(p => p.IsSupported).Returns(true);
        _purchaseServiceMock.Setup(p => p.PurchaseAsync(It.IsAny<string>()))
            .ReturnsAsync(PurchaseResult.Success);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.PurchaseRemoveAdsCommand.ExecuteAsync(null);

        // Assert
        _adsServiceMock.Verify(a => a.DisableAds(), Times.Once);
    }

    [TestMethod]
    public async Task PurchaseRemoveAdsCommand_WhenCancelled_DoesNotDisableAds()
    {
        // Arrange
        _adsServiceMock.Setup(a => a.IsSupported).Returns(true);
        _purchaseServiceMock.Setup(p => p.IsSupported).Returns(true);
        _purchaseServiceMock.Setup(p => p.PurchaseAsync(It.IsAny<string>()))
            .ReturnsAsync(PurchaseResult.Cancelled);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.PurchaseRemoveAdsCommand.ExecuteAsync(null);

        // Assert
        _adsServiceMock.Verify(a => a.DisableAds(), Times.Never);
    }

    [TestMethod]
    public async Task RestorePurchasesCommand_WhenSuccessful_UpdatesAdsRemoved()
    {
        // Arrange
        _adsServiceMock.Setup(a => a.IsSupported).Returns(true);
        _purchaseServiceMock.Setup(p => p.IsSupported).Returns(true);
        _purchaseServiceMock.Setup(p => p.RestorePurchasesAsync())
            .ReturnsAsync(true);

        var viewModel = CreateViewModel();

        // Act
        await viewModel.RestorePurchasesCommand.ExecuteAsync(null);

        // Assert
        _purchaseServiceMock.Verify(p => p.RestorePurchasesAsync(), Times.Once);
    }
}
