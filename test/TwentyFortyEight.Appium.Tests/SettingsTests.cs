namespace TwentyFortyEight.Appium.Tests;

/// <summary>
/// Tests for the Settings page functionality.
/// </summary>
[TestClass]
public class SettingsTests : AppiumTestBase
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

    [TestInitialize]
    public void TestInitialize()
    {
        // Navigate to Settings page before each test
        NavigateToSettings();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Navigate back to main page after each test
        NavigateBack();
        Thread.Sleep(500);
    }

    private static void NavigateToSettings()
    {
        // Ensure we're on the main page first
        if (!ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton))
        {
            // Try to get back to main page
            NavigateBack();
            Thread.Sleep(500);
        }

        // Open secondary menu first
        var moreButton = FindByAutomationId("SecondaryToolbarMenuButton");
        moreButton.Click();
        Thread.Sleep(500);

        var settingsButton = FindByAutomationId(MainGamePage.AutomationIds.ToolbarSettingsButton);
        settingsButton.Click();
        Thread.Sleep(1000);
    }

    [TestMethod]
    [TestCategory("Settings")]
    public void SettingsPage_DisplaysCoachToggle()
    {
        // Assert
        var coachSwitch = TryFindByAutomationId(SettingsPage.AutomationIds.CoachEnabledSwitch);
        Assert.IsNotNull(coachSwitch, "Coach toggle should be visible on Settings page");
    }

    [TestMethod]
    [TestCategory("Settings")]
    public void SettingsPage_DisplaysCoachNudgesToggle()
    {
        // Assert
        var nudgesSwitch = TryFindByAutomationId(
            SettingsPage.AutomationIds.CoachNudgesEnabledSwitch
        );
        Assert.IsNotNull(nudgesSwitch, "Coach Nudges toggle should be visible on Settings page");
    }

    [TestMethod]
    [TestCategory("Settings")]
    public void SettingsPage_DisplaysUndoButtonToggle()
    {
        // Assert
        var undoSwitch = TryFindByAutomationId(SettingsPage.AutomationIds.UndoButtonVisibleSwitch);
        Assert.IsNotNull(undoSwitch, "Undo Button toggle should be visible on Settings page");
    }

    [TestMethod]
    [TestCategory("Settings")]
    public void CoachToggle_CanBeToggled()
    {
        // Arrange
        var coachSwitch = FindByAutomationId(SettingsPage.AutomationIds.CoachEnabledSwitch);
        var initialState = coachSwitch.GetAttribute("value");

        // Act
        coachSwitch.Click();
        Thread.Sleep(500);

        // Assert
        var newState = coachSwitch.GetAttribute("value");
        Assert.AreNotEqual(
            initialState,
            newState,
            "Coach toggle state should change after clicking"
        );

        // Cleanup - toggle back
        coachSwitch.Click();
        Thread.Sleep(500);
    }

    [TestMethod]
    [TestCategory("Settings")]
    public void UndoButtonToggle_CanBeToggled()
    {
        // Arrange
        var undoSwitch = FindByAutomationId(SettingsPage.AutomationIds.UndoButtonVisibleSwitch);
        var initialState = undoSwitch.GetAttribute("value");

        // Act
        undoSwitch.Click();
        Thread.Sleep(500);

        // Assert
        var newState = undoSwitch.GetAttribute("value");
        Assert.AreNotEqual(
            initialState,
            newState,
            "Undo Button toggle state should change after clicking"
        );

        // Cleanup - toggle back
        undoSwitch.Click();
        Thread.Sleep(500);
    }

    [TestMethod]
    [TestCategory("Settings")]
    public void SettingsChanges_PersistAfterNavigatingAway()
    {
        // Arrange - Get current coach state
        var coachSwitch = FindByAutomationId(SettingsPage.AutomationIds.CoachEnabledSwitch);
        var initialState = coachSwitch.GetAttribute("value");

        // Act - Toggle the switch
        coachSwitch.Click();
        Thread.Sleep(500);

        // Navigate away and back
        NavigateBack();
        Thread.Sleep(500);

        NavigateToSettings();

        // Assert - State should be persisted
        coachSwitch = FindByAutomationId(SettingsPage.AutomationIds.CoachEnabledSwitch);
        var currentState = coachSwitch.GetAttribute("value");

        Assert.AreNotEqual(
            initialState,
            currentState,
            "Settings change should persist after navigating away"
        );

        // Cleanup - toggle back to original state
        coachSwitch.Click();
        Thread.Sleep(500);
    }
}
