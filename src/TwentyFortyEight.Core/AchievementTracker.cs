namespace TwentyFortyEight.Core;

/// <summary>
/// Tracks achievements and milestones during gameplay.
/// Platform-agnostic achievement tracking that works with any platform service
/// (Game Center, Xbox, Google Play, Steam, etc.).
/// </summary>
public class AchievementTracker : IAchievementTracker
{
    private readonly HashSet<int> _unlockedTiles = [];
    private readonly HashSet<int> _unlockedScores = [];
    private bool _firstWinUnlocked;

    // Track what was just unlocked in the current check
    private int? _lastUnlockedTileValue;
    private int? _lastUnlockedScoreMilestone;
    private bool _firstWinJustUnlocked;

    // Tile achievements: 128, 256, 512, 1024, 2048, 4096
    private static readonly int[] TileMilestones = { 128, 256, 512, 1024, 2048, 4096 };

    // Score achievements: 10K, 25K, 50K, 100K
    private static readonly int[] ScoreMilestones = { 10000, 25000, 50000, 100000 };

    public int? LastUnlockedTileValue => _lastUnlockedTileValue;
    public int? LastUnlockedScoreMilestone => _lastUnlockedScoreMilestone;
    public bool FirstWinJustUnlocked => _firstWinJustUnlocked;

    public bool CheckTileAchievement(int maxTileValue)
    {
        _lastUnlockedTileValue = null;

        // Find the highest milestone we've reached but haven't unlocked yet
        foreach (var milestone in TileMilestones)
        {
            if (maxTileValue >= milestone && !_unlockedTiles.Contains(milestone))
            {
                _unlockedTiles.Add(milestone);
                _lastUnlockedTileValue = milestone;
                return true;
            }
        }

        return false;
    }

    public bool CheckScoreAchievement(int score)
    {
        _lastUnlockedScoreMilestone = null;
        var anyUnlocked = false;

        // Check all milestones we've passed
        foreach (var milestone in ScoreMilestones)
        {
            if (score >= milestone && !_unlockedScores.Contains(milestone))
            {
                _unlockedScores.Add(milestone);
                _lastUnlockedScoreMilestone = milestone;
                anyUnlocked = true;
            }
        }

        return anyUnlocked;
    }

    public bool CheckFirstWinAchievement(bool isWon)
    {
        _firstWinJustUnlocked = false;

        if (isWon && !_firstWinUnlocked)
        {
            _firstWinUnlocked = true;
            _firstWinJustUnlocked = true;
            return true;
        }

        return false;
    }

    public void ResetJustUnlocked()
    {
        _lastUnlockedTileValue = null;
        _lastUnlockedScoreMilestone = null;
        _firstWinJustUnlocked = false;
    }
}
