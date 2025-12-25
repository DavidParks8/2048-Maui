namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Mac Catalyst-specific implementation of InAppPurchaseService.
/// </summary>
public sealed partial class InAppPurchaseService
{
    private static partial string GetRemoveAdsProductId() => PlatformProductIds.iOS.RemoveAdsProductId;
}
