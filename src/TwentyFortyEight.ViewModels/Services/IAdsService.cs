namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Platform-agnostic service for displaying advertisements.
/// Platform-specific implementations should integrate with:
/// - Android: Google AdMob
/// - iOS: Google AdMob for iOS
/// - Windows: Microsoft Advertising SDK
/// </summary>
public interface IAdsService
{
    /// <summary>
    /// Gets whether ads are supported on this platform.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Gets whether ads are currently enabled (not removed via in-app purchase).
    /// </summary>
    bool AreAdsEnabled { get; }

    /// <summary>
    /// Initializes the ads service. Should be called once at app startup.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Shows a banner ad in the specified container.
    /// </summary>
    /// <param name="container">The UI container to display the banner ad in.</param>
    void ShowBannerAd(object container);

    /// <summary>
    /// Hides the currently displayed banner ad.
    /// </summary>
    void HideBannerAd();

    /// <summary>
    /// Disables ads (typically called after successful in-app purchase).
    /// </summary>
    void DisableAds();
}
