namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// Platform-specific achievement and leaderboard identifiers.
/// These IDs must be configured in the respective platform's developer console:
/// - iOS/macOS: App Store Connect (Game Center)
/// - Windows: Partner Center (Xbox Live)
/// - Android: Google Play Console
/// </summary>
public static class PlatformAchievementIds
{
    // iOS Game Center IDs
    public static class iOS
    {
        public const string LeaderboardId = "com.dappermagna.twentyfortyeight.highscores";
        public const string Achievement_Tile128 = "com.dappermagna.twentyfortyeight.tile128";
        public const string Achievement_Tile256 = "com.dappermagna.twentyfortyeight.tile256";
        public const string Achievement_Tile512 = "com.dappermagna.twentyfortyeight.tile512";
        public const string Achievement_Tile1024 = "com.dappermagna.twentyfortyeight.tile1024";
        public const string Achievement_Tile2048 = "com.dappermagna.twentyfortyeight.tile2048";
        public const string Achievement_Tile4096 = "com.dappermagna.twentyfortyeight.tile4096";
        public const string Achievement_FirstWin = "com.dappermagna.twentyfortyeight.firstwin";
        public const string Achievement_Score10000 = "com.dappermagna.twentyfortyeight.score10000";
        public const string Achievement_Score25000 = "com.dappermagna.twentyfortyeight.score25000";
        public const string Achievement_Score50000 = "com.dappermagna.twentyfortyeight.score50000";
        public const string Achievement_Score100000 = "com.dappermagna.twentyfortyeight.score100000";
    }

    // Future: Xbox achievement IDs for Windows
    // public static class Xbox { ... }

    // Future: Google Play achievement IDs for Android
    // public static class GooglePlay { ... }
}
