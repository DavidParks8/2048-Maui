namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Android-specific implementation of InAppPurchaseService.
/// </summary>
public sealed partial class InAppPurchaseService
{
    private static partial string GetRemoveAdsProductId() => PlatformProductIds.Android.RemoveAdsProductId;
}
