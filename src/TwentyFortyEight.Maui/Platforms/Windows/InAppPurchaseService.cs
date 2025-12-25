namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Windows-specific implementation of InAppPurchaseService.
/// </summary>
public sealed partial class InAppPurchaseService
{
    private static partial string GetRemoveAdsProductId() => PlatformProductIds.Windows.RemoveAdsProductId;
}
