using Microsoft.Extensions.Logging;
using Plugin.MauiMTAdmob;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Implementation of IAdsService using Plugin.MauiMTAdmob for cross-platform AdMob support.
/// Supports Android, iOS, and Mac Catalyst.
/// </summary>
public sealed partial class AdsService(
    ISettingsService settingsService,
    ILogger<AdsService> logger) : IAdsService
{
    private bool _isInitialized;
    private bool _isBannerVisible;

    /// <summary>
    /// Gets whether ads are supported on this platform.
    /// </summary>
    public bool IsSupported => true;

    /// <summary>
    /// Gets whether ads are currently enabled (supported and not removed via purchase).
    /// </summary>
    public bool AreAdsEnabled => IsSupported && !settingsService.AdsRemoved;

    /// <summary>
    /// Gets the banner ad unit ID for the current platform.
    /// Implemented in platform-specific partial class files.
    /// </summary>
    private static partial string GetBannerAdUnitId();

    /// <summary>
    /// Initializes the ads service.
    /// </summary>
    public Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return Task.CompletedTask;
        }

        try
        {
            CrossMauiMTAdmob.Current.OnInterstitialLoaded += OnInterstitialLoaded;
            CrossMauiMTAdmob.Current.OnInterstitialFailedToLoad += OnInterstitialFailedToLoad;
            _isInitialized = true;
            LogAdsInitialized();
        }
        catch (Exception ex)
        {
            LogAdsInitializationFailed(ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows a banner ad.
    /// </summary>
    public void ShowBannerAd(object container)
    {
        if (!AreAdsEnabled || _isBannerVisible)
        {
            return;
        }

        try
        {
            CrossMauiMTAdmob.Current.LoadBanner(GetBannerAdUnitId());
            _isBannerVisible = true;
            LogBannerAdShown();
        }
        catch (Exception ex)
        {
            LogShowBannerAdFailed(ex);
        }
    }

    /// <summary>
    /// Hides the currently displayed banner ad.
    /// </summary>
    public void HideBannerAd()
    {
        if (!_isBannerVisible)
        {
            return;
        }

        try
        {
            CrossMauiMTAdmob.Current.HideBanner();
            _isBannerVisible = false;
            LogBannerAdHidden();
        }
        catch (Exception ex)
        {
            LogHideBannerAdFailed(ex);
        }
    }

    /// <summary>
    /// Disables ads permanently (called after successful in-app purchase).
    /// </summary>
    public void DisableAds()
    {
        settingsService.AdsRemoved = true;
        HideBannerAd();
        LogAdsDisabled();
    }

    private void OnInterstitialLoaded(object? sender, EventArgs e)
    {
        LogInterstitialLoaded();
    }

    private void OnInterstitialFailedToLoad(object? sender, EventArgs e)
    {
        LogInterstitialFailedToLoad();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Ads service initialized")]
    partial void LogAdsInitialized();

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to initialize ads service")]
    partial void LogAdsInitializationFailed(Exception ex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Banner ad shown")]
    partial void LogBannerAdShown();

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to show banner ad")]
    partial void LogShowBannerAdFailed(Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Banner ad hidden")]
    partial void LogBannerAdHidden();

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to hide banner ad")]
    partial void LogHideBannerAdFailed(Exception ex);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Ads disabled via purchase")]
    partial void LogAdsDisabled();

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Interstitial ad loaded")]
    partial void LogInterstitialLoaded();

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "Interstitial ad failed to load")]
    partial void LogInterstitialFailedToLoad();
}
