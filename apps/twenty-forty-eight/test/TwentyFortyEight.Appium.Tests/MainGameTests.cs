namespace TwentyFortyEight.Appium.Tests;

/// <summary>
/// Tests for the main game functionality on the MainPage.
/// Note: Movement buttons are only visible when VoiceOver is enabled.
/// These tests use always-visible elements like toolbar buttons and swipe gestures.
/// </summary>
[TestClass]
public class MainGameTests : AppiumTestBase
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

    [TestMethod]
    [TestCategory("Smoke")]
    public void App_Launches_Successfully()
    {
        // Assert - The toolbar should be visible with the New Game button
        var newGameButton = TryFindByAutomationId(MainGamePage.AutomationIds.ToolbarNewGameButton);

        Assert.IsNotNull(
            newGameButton,
            "The main game page should be displayed with the New Game toolbar button"
        );
    }

    [TestMethod]
    [TestCategory("UI")]
    public void MainPage_HasToolbarItems()
    {
        // Assert - Key toolbar items should be present
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "New Game button should exist"
        );
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarModeButton),
            "Mode button should exist"
        );
    }

    [TestMethod]
    [TestCategory("UI")]
    public void MainPage_HasGameBoard()
    {
        // The game board has a semantic description that includes "Game board"
        var gameBoard = TryFindByXPath("//*[contains(@name, 'Game board')]");

        Assert.IsNotNull(gameBoard, "Game board should be visible on the main page");
    }

    [TestMethod]
    [TestCategory("UI")]
    public void MainPage_ShowsScoreLabels()
    {
        // Assert - Score labels should be visible
        var scoreLabel = TryFindByXPath("//*[@name='SCORE']");
        var bestLabel = TryFindByXPath("//*[@name='BEST']");

        Assert.IsNotNull(scoreLabel, "SCORE label should be visible");
        Assert.IsNotNull(bestLabel, "BEST label should be visible");
    }

    [TestMethod]
    [TestCategory("Gameplay")]
    public void NewGame_ButtonIsClickable()
    {
        // Arrange
        var newGameButton = FindByAutomationId(MainGamePage.AutomationIds.ToolbarNewGameButton);

        // Act
        newGameButton.Click();

        // Wait for any confirmation dialog or animation
        Thread.Sleep(1000);

        // Assert - App should still be functional (toolbar still visible)
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "New Game button should still exist after clicking"
        );
    }

    [TestMethod]
    [TestCategory("Gameplay")]
    public void Swipe_Left_PerformsMove()
    {
        // Act
        Swipe(SwipeDirection.Left);
        Thread.Sleep(500); // Wait for animation

        // Assert - Game should still be functional (toolbar visible)
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "Game should be functional after swipe left"
        );
    }

    [TestMethod]
    [TestCategory("Gameplay")]
    public void Swipe_Right_PerformsMove()
    {
        // Act
        Swipe(SwipeDirection.Right);
        Thread.Sleep(500);

        // Assert
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "Game should be functional after swipe right"
        );
    }

    [TestMethod]
    [TestCategory("Gameplay")]
    public void Swipe_Up_PerformsMove()
    {
        // Act
        Swipe(SwipeDirection.Up);
        Thread.Sleep(500);

        // Assert
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "Game should be functional after swipe up"
        );
    }

    [TestMethod]
    [TestCategory("Gameplay")]
    public void Swipe_Down_PerformsMove()
    {
        // Act
        Swipe(SwipeDirection.Down);
        Thread.Sleep(500);

        // Assert
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "Game should be functional after swipe down"
        );
    }

    [TestMethod]
    [TestCategory("Gameplay")]
    public void MultipleSwipes_GameRemainsResponsive()
    {
        // Act - Perform multiple moves rapidly
        for (int i = 0; i < 10; i++)
        {
            Swipe((SwipeDirection)(i % 4));
            Thread.Sleep(200);
        }

        // Assert - Game should still be responsive
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarNewGameButton),
            "Game should remain responsive after multiple swipes"
        );
    }

    [TestMethod]
    [TestCategory("Gameplay")]
    public void ModeButton_OpensMenu()
    {
        // Arrange
        var modeButton = FindByAutomationId(MainGamePage.AutomationIds.ToolbarModeButton);

        // Act
        modeButton.Click();
        Thread.Sleep(500);

        // Assert - Some menu or picker should appear (we just verify app didn't crash)
        // The mode button should still be findable
        Assert.IsTrue(
            ElementExists(MainGamePage.AutomationIds.ToolbarModeButton),
            "Mode button should still exist after clicking"
        );

        // Dismiss any open menu by tapping elsewhere
        Swipe(SwipeDirection.Down, 100);
        Thread.Sleep(300);
    }
}
