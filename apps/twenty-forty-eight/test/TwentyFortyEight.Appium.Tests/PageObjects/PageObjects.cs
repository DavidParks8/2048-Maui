namespace TwentyFortyEight.Appium.Tests;

/// <summary>
/// Page object for the main game page.
/// Encapsulates element locators and common interactions.
/// </summary>
public class MainGamePage
{
    private readonly AppiumTestBase _testBase;

    public MainGamePage(AppiumTestBase testBase)
    {
        _testBase = testBase;
    }

    // AutomationIds from MainPage.xaml
    public static class AutomationIds
    {
        // Toolbar items (always visible)
        public const string ToolbarNewGameButton = "ToolbarNewGameButton";
        public const string ToolbarModeButton = "ToolbarModeButton";

        // Secondary menu items (visible after clicking "More" button)
        public const string SecondaryToolbarMenuButton = "SecondaryToolbarMenuButton";
        public const string ToolbarLeaderboardButton = "ToolbarLeaderboardButton";
        public const string ToolbarAchievementsButton = "ToolbarAchievementsButton";
        public const string ToolbarStatisticsButton = "ToolbarStatisticsButton";
        public const string ToolbarSettingsButton = "ToolbarSettingsButton";
        public const string ToolbarHowToPlayButton = "ToolbarHowToPlayButton";
        public const string ToolbarAboutButton = "ToolbarAboutButton";
        public const string ToolbarCoachButton = "ToolbarCoachButton";

        // Movement buttons (only visible when VoiceOver/screen reader is enabled)
        public const string MoveLeftButton = "MoveLeftButton";
        public const string MoveUpButton = "MoveUpButton";
        public const string MoveDownButton = "MoveDownButton";
        public const string MoveRightButton = "MoveRightButton";
    }
}

/// <summary>
/// Page object for the Settings page.
/// </summary>
public class SettingsPage
{
    public static class AutomationIds
    {
        public const string CoachEnabledSwitch = "CoachEnabledSwitch";
        public const string CoachNudgesEnabledSwitch = "CoachNudgesEnabledSwitch";
        public const string UndoButtonVisibleSwitch = "UndoButtonVisibleSwitch";
        public const string HapticsEnabledSwitch = "HapticsEnabledSwitch";
    }
}
