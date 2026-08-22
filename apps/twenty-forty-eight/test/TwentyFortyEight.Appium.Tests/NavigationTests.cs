namespace TwentyFortyEight.Appium.Tests;

/// <summary>
/// Tests for navigation between pages.
/// Note: Settings, Statistics, About are in the secondary toolbar menu ("More" button).
/// </summary>
[TestClass]
public class NavigationTests : AppiumTestBase
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        InitializeDriver();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        QuitDriver();
    }

    /// <summary>
    /// Opens the secondary toolbar menu (More button) to access Settings, Statistics, About, etc.
    /// </summary>
    private void OpenSecondaryMenu()
    {
        // The "More" button has AutomationId "SecondaryToolbarMenuButton"
        var moreButton = FindByAutomationId("SecondaryToolbarMenuButton");
        moreButton.Click();
        Thread.Sleep(500); // Wait for menu to appear
    }

    [TestMethod]
    [TestCategory("Navigation")]
    public void Navigate_ToSettings_AndBack()
    {
        // Arrange - Ensure we're on the main page
        WaitForElement(MainGamePage.AutomationIds.ToolbarNewGameButton);

        // Act - Open secondary menu and navigate to Settings
        OpenSecondaryMenu();

        var settingsButton = FindByAutomationId(MainGamePage.AutomationIds.ToolbarSettingsButton);
        settingsButton.Click();
        Thread.Sleep(1000);

        // Assert - Settings page elements should be visible
        var coachSwitch = TryFindByAutomationId(SettingsPage.AutomationIds.CoachEnabledSwitch);
        Assert.IsNotNull(coachSwitch, "Settings page should display Coach toggle");

        // Navigate back
        NavigateBack();
        Thread.Sleep(500);

        // Assert - Back on main page
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "Should be back on main game page"
        );
    }

    [TestMethod]
    [TestCategory("Navigation")]
    public void Navigate_ToStatistics_AndBack()
    {
        // Arrange
        WaitForElement(MainGamePage.AutomationIds.ToolbarNewGameButton);

        // Act - Open secondary menu and navigate to Statistics
        OpenSecondaryMenu();

        var statsButton = FindByAutomationId(MainGamePage.AutomationIds.ToolbarStatisticsButton);
        statsButton.Click();
        Thread.Sleep(1000);

        // Navigate back
        NavigateBack();
        Thread.Sleep(500);

        // Assert - Back on main page
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "Should be back on main game page"
        );
    }

    [TestMethod]
    [TestCategory("Navigation")]
    public void Navigate_ToAbout_AndBack()
    {
        // Arrange
        WaitForElement(MainGamePage.AutomationIds.ToolbarNewGameButton);

        // Act - Open secondary menu and navigate to About
        OpenSecondaryMenu();

        var aboutButton = FindByAutomationId(MainGamePage.AutomationIds.ToolbarAboutButton);
        aboutButton.Click();
        Thread.Sleep(1000);

        // Navigate back
        NavigateBack();
        Thread.Sleep(500);

        // Assert - Back on main page
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "Should be back on main game page"
        );
    }
}
