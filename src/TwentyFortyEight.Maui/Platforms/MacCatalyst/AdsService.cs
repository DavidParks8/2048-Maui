namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Mac Catalyst-specific implementation of AdsService.
/// </summary>
public sealed partial class AdsService
{
    private static partial string GetBannerAdUnitId() => PlatformProductIds.iOS.BannerAdUnitId;
}
