using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels;

/// <summary>
/// ViewModel for the settings page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IHapticService _hapticService;
    private readonly IAdsService _adsService;
    private readonly IInAppPurchaseService _purchaseService;

    [ObservableProperty]
    private bool _animationsEnabled;

    [ObservableProperty]
    private double _animationSpeed;

    [ObservableProperty]
    private bool _hapticsEnabled;

    [ObservableProperty]
    private bool _adsRemoved;

    [ObservableProperty]
    private string? _removeAdsPrice;

    [ObservableProperty]
    private bool _isPurchaseInProgress;

    /// <summary>
    /// Gets a value indicating whether haptic feedback is supported on this device.
    /// </summary>
    public bool IsHapticsSupported => _hapticService.IsSupported;

    /// <summary>
    /// Gets a value indicating whether ads are supported on this platform.
    /// </summary>
    public bool AreAdsSupported => _adsService.IsSupported;

    /// <summary>
    /// Gets a value indicating whether in-app purchases are supported on this platform.
    /// </summary>
    public bool IsPurchaseSupported => _purchaseService.IsSupported;

    /// <summary>
    /// Gets whether the Remove Ads section should be visible.
    /// This is true when ads are supported and not yet removed, and purchases are supported.
    /// </summary>
    public bool ShowRemoveAdsSection => AreAdsSupported && !AdsRemoved && IsPurchaseSupported;

    public SettingsViewModel(
        ISettingsService settingsService,
        IHapticService hapticService,
        IAdsService adsService,
        IInAppPurchaseService purchaseService)
    {
        _settingsService = settingsService;
        _hapticService = hapticService;
        _adsService = adsService;
        _purchaseService = purchaseService;

        // Load current settings
        _animationsEnabled = _settingsService.AnimationsEnabled;
        _animationSpeed = _settingsService.AnimationSpeed;
        _hapticsEnabled = _settingsService.HapticsEnabled;
        _adsRemoved = _settingsService.AdsRemoved;

        // Load remove ads price if purchases are supported
        if (IsPurchaseSupported && !AdsRemoved)
        {
            _ = SafeLoadRemoveAdsPriceAsync();
        }
    }

    private async Task SafeLoadRemoveAdsPriceAsync()
    {
        try
        {
            RemoveAdsPrice = await _purchaseService.GetPriceAsync(_purchaseService.RemoveAdsProductId);
        }
        catch
        {
            // Silently ignore errors loading price - the button will show without price
        }
    }

    partial void OnAnimationsEnabledChanged(bool value)
    {
        _settingsService.AnimationsEnabled = value;
    }

    partial void OnAnimationSpeedChanged(double value)
    {
        _settingsService.AnimationSpeed = value;
    }

    partial void OnHapticsEnabledChanged(bool value)
    {
        _settingsService.HapticsEnabled = value;
    }

    partial void OnAdsRemovedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowRemoveAdsSection));
    }

    [RelayCommand]
    private async Task PurchaseRemoveAdsAsync()
    {
        if (IsPurchaseInProgress || AdsRemoved)
        {
            return;
        }

        try
        {
            IsPurchaseInProgress = true;

            var result = await _purchaseService.PurchaseAsync(_purchaseService.RemoveAdsProductId);

            switch (result)
            {
                case PurchaseResult.Success:
                case PurchaseResult.AlreadyOwned:
                    _adsService.DisableAds();
                    AdsRemoved = true;
                    break;

                case PurchaseResult.Cancelled:
                    // User cancelled, do nothing
                    break;

                case PurchaseResult.Failed:
                case PurchaseResult.NotSupported:
                default:
                    // Could show an error message here
                    break;
            }
        }
        finally
        {
            IsPurchaseInProgress = false;
        }
    }

    [RelayCommand]
    private async Task RestorePurchasesAsync()
    {
        if (IsPurchaseInProgress)
        {
            return;
        }

        try
        {
            IsPurchaseInProgress = true;

            var restored = await _purchaseService.RestorePurchasesAsync();
            if (restored)
            {
                // The restore operation updates settings via AdsService.DisableAds()
                // Refresh our local state to match
                AdsRemoved = true;
            }
        }
        finally
        {
            IsPurchaseInProgress = false;
        }
    }
}
