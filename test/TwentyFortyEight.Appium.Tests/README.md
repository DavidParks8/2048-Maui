# TwentyFortyEight Appium Tests

This project contains end-to-end UI tests for the 2048 MAUI application using Appium on iOS Simulator.

## Prerequisites

### 1. Install Appium Server and XCUITest Driver

```bash
npm install -g appium
appium driver install xcuitest
```

### 2. Have Xcode Installed

The tests require Xcode to be installed for iOS Simulator support.

## Running Tests

Simply run `dotnet test` - the test setup will automatically:

1. **Start Appium server** if not already running
2. **Boot iOS Simulator** if not already booted
3. **Build and install the app** if not found

```bash
dotnet test test/TwentyFortyEight.Appium.Tests
```

### Run by Category

```bash
# Smoke tests only
dotnet test test/TwentyFortyEight.Appium.Tests --filter "TestCategory=Smoke"

# Gameplay tests
dotnet test test/TwentyFortyEight.Appium.Tests --filter "TestCategory=Gameplay"

# Navigation tests
dotnet test test/TwentyFortyEight.Appium.Tests --filter "TestCategory=Navigation"

# Settings tests
dotnet test test/TwentyFortyEight.Appium.Tests --filter "TestCategory=Settings"
```

## Test Structure

### Test Base Class

- `AppiumTestBase.cs` - Base class for iOS Simulator tests using XCUITest driver
- `TestSetup.cs` - Assembly-level setup that auto-starts Appium and simulator

### Page Objects

Page objects are defined in `PageObjects/PageObjects.cs` and contain AutomationId constants that match the XAML elements.

### Test Categories

| Category   | Description                                    |
| ---------- | ---------------------------------------------- |
| Smoke      | Basic launch and functionality verification    |
| UI         | User interface element presence and visibility |
| Gameplay   | Game movement and interaction tests            |
| Navigation | Page navigation tests                          |
| Settings   | Settings page functionality                    |

## Configuration

You can override the default simulator settings with environment variables:

```bash
# Use a different simulator
export IOS_SIMULATOR_NAME="iPhone 16"
export IOS_PLATFORM_VERSION="18.0"
dotnet test test/TwentyFortyEight.Appium.Tests
```

## Adding New Tests

### 1. Add AutomationId to XAML

Ensure your UI elements have `AutomationId` attributes:

```xml
<Button AutomationId="MyButton" Text="Click Me" />
```

### 2. Update Page Objects

Add the AutomationId constant to the appropriate page object:

```csharp
public static class AutomationIds
{
    public const string MyButton = "MyButton";
}
```

### 3. Write Test

```csharp
[TestMethod]
[TestCategory("UI")]
public void MyButton_IsVisible()
{
    var button = TryFindByAutomationId(MainGamePage.AutomationIds.MyButton);
    Assert.IsNotNull(button, "My button should be visible");
}
```

## Troubleshooting

### Appium Not Installed

The tests auto-start Appium, but it must be installed first:

```bash
npm install -g appium
appium driver install xcuitest
```

### No Simulators Available

Make sure you have iOS Simulators installed via Xcode:

```bash
xcrun simctl list devices available
```

### WebDriverAgent Issues

For iOS testing, WDA needs to be properly configured:

```bash
# Download pre-built WDA
appium driver run xcuitest install-wda
```

### Element Not Found

1. Check that the AutomationId in XAML matches exactly
2. Increase timeouts if the app is slow to load
3. Take a screenshot to see the current state:

```csharp
TakeScreenshot("debug");
```

## Notes on Movement Buttons

The movement buttons (MoveLeftButton, MoveUpButton, etc.) are **only visible when VoiceOver is enabled**. These are accessibility-only UI elements. For standard UI testing, use:

- Toolbar buttons (always visible)
- Swipe gestures for gameplay
- Game board element (has semantic description)
