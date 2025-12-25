namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Android-specific implementation of AdsService.
/// </summary>
public sealed partial class AdsService
{
    private static partial string GetBannerAdUnitId() => PlatformProductIds.Android.BannerAdUnitId;
}
