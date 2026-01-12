using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.iOS;

namespace TwentyFortyEight.Appium.Tests;

/// <summary>
/// Base test class that manages Appium driver lifecycle for iOS Simulator MAUI app testing.
/// </summary>
public abstract class AppiumTestBase
{
    private static AppiumDriver? _driver;
    private static readonly Lock _driverLock = new();

    /// <summary>
    /// Gets the Appium driver instance.
    /// </summary>
    protected static AppiumDriver Driver =>
        _driver
        ?? throw new InvalidOperationException(
            "Driver not initialized. Call InitializeDriver first."
        );

    /// <summary>
    /// Gets the Appium server URL.
    /// </summary>
    protected static Uri AppiumServerUrl => new("http://127.0.0.1:4723");

    /// <summary>
    /// Default timeout for element waits.
    /// </summary>
    protected static TimeSpan DefaultTimeout => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Short timeout for quick checks.
    /// </summary>
    protected static TimeSpan ShortTimeout => TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the bundle identifier for the iOS app.
    /// </summary>
    protected static string BundleId => "com.davidparks.twentyfortyeight";

    /// <summary>
    /// Gets the path to the iOS Simulator app bundle.
    /// </summary>
    protected static string GetAppPath()
    {
        // The iOS simulator app is built to this location
        var basePath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "TwentyFortyEight.Maui",
                "bin",
                "Debug",
                "net10.0-ios",
                "iossimulator-arm64",
                "TwentyFortyEight.Maui.app"
            )
        );

        return basePath;
    }

    /// <summary>
    /// Initializes the Appium driver for iOS Simulator testing.
    /// Uses environment variables IOS_SIMULATOR_NAME and IOS_PLATFORM_VERSION if set.
    /// </summary>
    /// <param name="deviceName">The iOS simulator device name (e.g., "iPhone 17 Pro")</param>
    /// <param name="platformVersion">The iOS version (e.g., "26.2")</param>
    protected static void InitializeDriver(
        string? deviceName = null,
        string? platformVersion = null
    )
    {
        // Use environment variables if parameters not specified
        deviceName ??= Environment.GetEnvironmentVariable("IOS_SIMULATOR_NAME") ?? "iPhone 17 Pro";
        platformVersion ??= Environment.GetEnvironmentVariable("IOS_PLATFORM_VERSION") ?? "26.2";

        lock (_driverLock)
        {
            if (_driver != null)
            {
                return;
            }

            var options = new AppiumOptions
            {
                PlatformName = "iOS",
                AutomationName = "XCUITest",
                DeviceName = deviceName,
                PlatformVersion = platformVersion,
            };

            var appPath = GetAppPath();
            if (Directory.Exists(appPath))
            {
                options.App = appPath;
            }
            else
            {
                // Fall back to bundle ID if app path doesn't exist
                options.App = BundleId;
            }

            options.AddAdditionalAppiumOption("newCommandTimeout", 300);
            options.AddAdditionalAppiumOption("wdaLaunchTimeout", 120000);
            options.AddAdditionalAppiumOption("wdaConnectionTimeout", 120000);
            options.AddAdditionalAppiumOption("launchTimeout", 120000);
            options.AddAdditionalAppiumOption("fullReset", false);
            options.AddAdditionalAppiumOption("noReset", false);

            _driver = new IOSDriver(AppiumServerUrl, options, TimeSpan.FromMinutes(3));
            _driver.Manage().Timeouts().ImplicitWait = DefaultTimeout;

            // Wait for the app to fully launch
            Thread.Sleep(3000);
        }
    }

    /// <summary>
    /// Quits the Appium driver.
    /// </summary>
    protected static void QuitDriver()
    {
        lock (_driverLock)
        {
            _driver?.Quit();
            _driver = null;
        }
    }

    /// <summary>
    /// Finds an element by its AutomationId (accessibility identifier on iOS).
    /// </summary>
    protected static AppiumElement FindByAutomationId(string automationId)
    {
        return Driver.FindElement(MobileBy.AccessibilityId(automationId));
    }

    /// <summary>
    /// Finds an element by its AutomationId, returning null if not found.
    /// </summary>
    protected static AppiumElement? TryFindByAutomationId(string automationId)
    {
        try
        {
            Driver.Manage().Timeouts().ImplicitWait = ShortTimeout;
            return Driver.FindElement(MobileBy.AccessibilityId(automationId));
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return null;
        }
        finally
        {
            Driver.Manage().Timeouts().ImplicitWait = DefaultTimeout;
        }
    }

    /// <summary>
    /// Finds an element by XPath.
    /// </summary>
    protected static AppiumElement FindByXPath(string xpath)
    {
        return Driver.FindElement(OpenQA.Selenium.By.XPath(xpath));
    }

    /// <summary>
    /// Finds an element by XPath, returning null if not found.
    /// </summary>
    protected static AppiumElement? TryFindByXPath(string xpath)
    {
        try
        {
            Driver.Manage().Timeouts().ImplicitWait = ShortTimeout;
            return Driver.FindElement(OpenQA.Selenium.By.XPath(xpath));
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return null;
        }
        finally
        {
            Driver.Manage().Timeouts().ImplicitWait = DefaultTimeout;
        }
    }

    /// <summary>
    /// Waits for an element to be present.
    /// </summary>
    protected static AppiumElement WaitForElement(string automationId, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? DefaultTimeout;
        var endTime = DateTime.UtcNow.Add(effectiveTimeout);

        while (DateTime.UtcNow < endTime)
        {
            try
            {
                var element = FindByAutomationId(automationId);
                if (element.Displayed)
                {
                    return element;
                }
            }
            catch (OpenQA.Selenium.NoSuchElementException)
            {
                // Continue waiting
            }

            Thread.Sleep(500);
        }

        throw new TimeoutException(
            $"Element with AutomationId '{automationId}' was not found within {effectiveTimeout.TotalSeconds} seconds."
        );
    }

    /// <summary>
    /// Checks if an element exists.
    /// </summary>
    protected static bool ElementExists(string automationId)
    {
        return TryFindByAutomationId(automationId) != null;
    }

    /// <summary>
    /// Takes a screenshot and saves it to the test results directory.
    /// </summary>
    protected static string TakeScreenshot(string name)
    {
        var screenshot = Driver.GetScreenshot();
        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        screenshot.SaveAsFile(filePath);
        return filePath;
    }

    /// <summary>
    /// Gets the page source for debugging.
    /// </summary>
    protected static string GetPageSource()
    {
        return Driver.PageSource;
    }

    /// <summary>
    /// Logs the page source to the console for debugging.
    /// </summary>
    protected static void DebugPageSource()
    {
        Console.WriteLine("=== Page Source ===");
        Console.WriteLine(Driver.PageSource);
        Console.WriteLine("=== End Page Source ===");
    }

    /// <summary>
    /// Navigates back using the iOS navigation.
    /// </summary>
    protected static void NavigateBack()
    {
        try
        {
            // Try finding a back button first (common iOS navigation pattern)
            var backButton = Driver.FindElement(
                OpenQA.Selenium.By.XPath("//XCUIElementTypeButton[@name='Back']")
            );
            backButton.Click();
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            // Try generic back button by accessibility ID
            try
            {
                var backButton = Driver.FindElement(MobileBy.AccessibilityId("Back"));
                backButton.Click();
            }
            catch (OpenQA.Selenium.NoSuchElementException)
            {
                // No back button found - some pages may not have navigation
            }
        }
    }

    /// <summary>
    /// Performs a swipe gesture in the specified direction.
    /// </summary>
    protected static void Swipe(SwipeDirection direction, int duration = 500)
    {
        var screenSize = Driver.Manage().Window.Size;
        var centerX = screenSize.Width / 2;
        var centerY = screenSize.Height / 2;

        int startX,
            startY,
            endX,
            endY;

        switch (direction)
        {
            case SwipeDirection.Left:
                startX = (int)(screenSize.Width * 0.8);
                endX = (int)(screenSize.Width * 0.2);
                startY = endY = centerY;
                break;
            case SwipeDirection.Right:
                startX = (int)(screenSize.Width * 0.2);
                endX = (int)(screenSize.Width * 0.8);
                startY = endY = centerY;
                break;
            case SwipeDirection.Up:
                startX = endX = centerX;
                startY = (int)(screenSize.Height * 0.7);
                endY = (int)(screenSize.Height * 0.3);
                break;
            case SwipeDirection.Down:
                startX = endX = centerX;
                startY = (int)(screenSize.Height * 0.3);
                endY = (int)(screenSize.Height * 0.7);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction));
        }

        // Use W3C Actions API for swiping
        var finger = new OpenQA.Selenium.Appium.Interactions.PointerInputDevice(
            OpenQA.Selenium.Interactions.PointerKind.Touch,
            "finger"
        );
        var swipe = new OpenQA.Selenium.Interactions.ActionSequence(finger);

        swipe.AddAction(
            finger.CreatePointerMove(
                OpenQA.Selenium.Interactions.CoordinateOrigin.Viewport,
                startX,
                startY,
                TimeSpan.Zero
            )
        );
        swipe.AddAction(finger.CreatePointerDown(OpenQA.Selenium.Interactions.MouseButton.Left));
        swipe.AddAction(
            finger.CreatePointerMove(
                OpenQA.Selenium.Interactions.CoordinateOrigin.Viewport,
                endX,
                endY,
                TimeSpan.FromMilliseconds(duration)
            )
        );
        swipe.AddAction(finger.CreatePointerUp(OpenQA.Selenium.Interactions.MouseButton.Left));

        Driver.PerformActions([swipe]);
    }
}

/// <summary>
/// Swipe direction enumeration.
/// </summary>
public enum SwipeDirection
{
    Left,
    Right,
    Up,
    Down,
}
