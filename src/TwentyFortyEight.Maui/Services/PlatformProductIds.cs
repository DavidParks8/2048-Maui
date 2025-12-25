namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Platform-specific product identifiers for in-app purchases and ad unit IDs.
/// These IDs must be configured in the respective platform's developer console:
/// - Android: Google Play Console (for IAP) and AdMob Console (for ads)
/// - iOS: App Store Connect (for IAP) and AdMob Console (for ads)
/// - Windows: Partner Center (for Microsoft Store IAP and ads)
/// </summary>
public static class PlatformProductIds
{
    /// <summary>
    /// Android product IDs and ad unit IDs.
    /// </summary>
    public static class Android
    {
        /// <summary>
        /// Product ID for the "Remove Ads" in-app purchase (Google Play Console).
        /// </summary>
        public const string RemoveAdsProductId = "com.davidparks.twentyfourtyeight.remove_ads";

        /// <summary>
        /// AdMob banner ad unit ID for Android.
        /// Replace with your actual ad unit ID from AdMob Console.
        /// The sample ID below is Google's test banner ad unit ID.
        /// </summary>
        public const string BannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
    }

    /// <summary>
    /// iOS product IDs and ad unit IDs.
    /// </summary>
    public static class iOS
    {
        /// <summary>
        /// Product ID for the "Remove Ads" in-app purchase (App Store Connect).
        /// </summary>
        public const string RemoveAdsProductId = "com.davidparks.twentyfourtyeight.remove_ads";

        /// <summary>
        /// AdMob banner ad unit ID for iOS.
        /// Replace with your actual ad unit ID from AdMob Console.
        /// The sample ID below is Google's test banner ad unit ID.
        /// </summary>
        public const string BannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
    }

    /// <summary>
    /// Windows product IDs and ad unit IDs.
    /// </summary>
    public static class Windows
    {
        /// <summary>
        /// Product ID for the "Remove Ads" in-app purchase (Partner Center).
        /// </summary>
        public const string RemoveAdsProductId = "remove_ads";

        /// <summary>
        /// Microsoft Advertising application ID.
        /// Replace with your actual application ID from Partner Center.
        /// </summary>
        public const string ApplicationId = "d25517cb-12d4-4699-8bdc-52040c712cab";

        /// <summary>
        /// Microsoft Advertising banner ad unit ID.
        /// Replace with your actual ad unit ID from Partner Center.
        /// The sample ID below is Microsoft's test ad unit ID.
        /// </summary>
        public const string BannerAdUnitId = "test";
    }
}
