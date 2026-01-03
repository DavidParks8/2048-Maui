#if __MACCATALYST__
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MacCatalyst implementation of achievement ID mapper using Game Center achievement IDs.
/// </summary>
public class AchievementIdMapper : IAchievementIdMapper
{
    public string? GetTileAchievementId(int tileValue)
    {
        return tileValue switch
        {
            4096 => PlatformAchievementIds.iOS.Achievement_Tile4096,
            2048 => PlatformAchievementIds.iOS.Achievement_Tile2048,
            1024 => PlatformAchievementIds.iOS.Achievement_Tile1024,
            512 => PlatformAchievementIds.iOS.Achievement_Tile512,
            256 => PlatformAchievementIds.iOS.Achievement_Tile256,
            128 => PlatformAchievementIds.iOS.Achievement_Tile128,
            _ => null,
        };
    }

    public string? GetFirstWinAchievementId()
    {
        return PlatformAchievementIds.iOS.Achievement_FirstWin;
    }

    public string? GetScoreAchievementId(int score)
    {
        return score switch
        {
            10000 => PlatformAchievementIds.iOS.Achievement_Score10000,
            25000 => PlatformAchievementIds.iOS.Achievement_Score25000,
            50000 => PlatformAchievementIds.iOS.Achievement_Score50000,
            100000 => PlatformAchievementIds.iOS.Achievement_Score100000,
            _ => null,
        };
    }
}
#endif
