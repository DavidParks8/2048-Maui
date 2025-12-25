namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// iOS-specific implementation of AdsService.
/// </summary>
public sealed partial class AdsService
{
    private static partial string GetBannerAdUnitId() => PlatformProductIds.iOS.BannerAdUnitId;
}
