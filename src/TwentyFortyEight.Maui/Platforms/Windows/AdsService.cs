namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Windows-specific implementation of AdsService.
/// </summary>
public sealed partial class AdsService
{
    private static partial string GetBannerAdUnitId() => PlatformProductIds.Windows.BannerAdUnitId;
}
