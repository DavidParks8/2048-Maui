using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Default achievement ID mapper that returns null for all platforms.
/// Platform-specific implementations override this in their respective platform folders.
/// </summary>
public partial class AchievementIdMapper : IAchievementIdMapper
{
    public virtual string? GetTileAchievementId(int tileValue) => null;

    public virtual string? GetFirstWinAchievementId() => null;

    public virtual string? GetScoreAchievementId(int score) => null;
}
