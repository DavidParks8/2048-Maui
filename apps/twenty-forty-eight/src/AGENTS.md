# Agent Instructions

When doing a quick build check, try not to build android unless specifically attempting to do so. The android build is very slow. Dotnet build will by default build a bunch of platforms, including android.

The repo uses .net 10 Maui

## MAUI Development Guidelines

### Source Generators

Always use source generator attributes when working with MAUI projects:

- **ViewModels**: Use `[ObservableProperty]` from `CommunityToolkit.Mvvm` for observable properties
- **ContentView Components**: Use `[AutoBindable]` from `M.BindableProperty.Generator` for bindable properties
- **Commands**: Use `[RelayCommand]` from `CommunityToolkit.Mvvm` for commands

### Localization

All user-facing text must be localized using resource strings:

- Add strings to `Resources/Strings/AppStrings.resx`
- Access strings via `Resources.Strings.AppStrings.YourStringName`
- Never hardcode user-facing text directly in XAML or C# code
- Designer.cs files are not auto generated, and must always be manually updated

### Theme Support

Both light and dark mode must be supported:

- Use `{AppThemeBinding Light=..., Dark=...}` for colors in XAML
- Define theme-aware colors in `Resources/Styles/Colors.xaml`
- Test UI in both light and dark modes

### Native iOS Styling Guidelines

When creating pages that should match native iOS appearance (Settings, About, Stats):

#### Section Headers

- **Always place section headers OUTSIDE the cards/borders**
- Font size: `13`
- Text color: `NativeTextTertiaryLight/Dark`
- Left margin: `8,0,0,0`
- Example:

```xml
<VerticalStackLayout Spacing="8">
    <Label Text="SECTION NAME"
           FontSize="13"
           TextColor="{AppThemeBinding Light={StaticResource NativeTextTertiaryLight}, 
                                      Dark={StaticResource NativeTextTertiaryDark}}"
           Margin="8,0,0,0" />
    <Border><!-- Card content here --></Border>
</VerticalStackLayout>
```

#### Card/Cell Styling

- Background: `NativeSettingsCellBackgroundLight/Dark`
- Corner radius: Use `{StaticResource NativeCardCornerRadius}`
- Padding: `16` for general content, `16,0` for row-based layouts
- Font sizes for content:
  - Primary labels: `17` (bold for emphasis)
  - Secondary text: `15`
  - Body text: `17` with `LineHeight="1.5"`

#### Page Background

- Use `NativeSettingsBackgroundLight/Dark` (not `GamePageBackground`)
- ScrollView padding: `16`

#### Text Color Hierarchy

- Primary text: `NativeTextPrimaryLight/Dark` (main labels, titles)
- Secondary text: `NativeTextSecondaryLight/Dark` (descriptions, body text)
- Tertiary text: `NativeTextTertiaryLight/Dark` (section headers, footer text)

### Examples

#### ViewModel (using CommunityToolkit.Mvvm)

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [RelayCommand]
    private void DoSomething() { }
}
```

#### ContentView Component (using M.BindableProperty.Generator)

```csharp
using Maui.BindableProperty.Generator.Core;

public partial class MyComponent : ContentView
{
#pragma warning disable CS0169
    [AutoBindable]
    private readonly string _title;

    [AutoBindable]
    private readonly ICommand? _myCommand;
#pragma warning restore CS0169
}
```

### Dependency Injection

Always prefer testable dependency injection instead of static classes.

## Critical Rules (NEVER Violate)

- **NEVER use ListView** - obsolete, will be deleted. Use CollectionView
- **NEVER use TableView** - obsolete. Use Grid/VerticalStackLayout layouts
- **NEVER use AndExpand** layout options - obsolete
- **NEVER use BackgroundColor** - always use `Background` property
- **NEVER place ScrollView/CollectionView inside StackLayout** - breaks scrolling/virtualization
- **NEVER reference images as SVG** - always use PNG (SVG only for generation)
- **NEVER mix Shell with NavigationPage/TabbedPage/FlyoutPage**
- **NEVER use renderers** - use handlers instead
- **NEVER use `SemanticScreenReader` directly** (except inside `MauiScreenReaderService`) - always announce via `IScreenReaderService`

## Control Reference

### Status Indicators

| Control | Purpose | Key Properties |
|---------|---------|----------------|
| ActivityIndicator | Indeterminate busy state | `IsRunning`, `Color` |
| ProgressBar | Known progress (0.0-1.0) | `Progress`, `ProgressColor` |

### Layout Controls

| Control | Purpose | Notes |
|---------|---------|-------|
| **Border** | Container with border | **Prefer over Frame** |
| ContentView | Reusable custom controls | Encapsulates UI components |
| ScrollView | Scrollable content | Single child; **never in StackLayout** |
| Frame | Legacy container | Only for shadows |

### Shapes

BoxView, Ellipse, Line, Path, Polygon, Polyline, Rectangle, RoundRectangle - all support `Fill`, `Stroke`, `StrokeThickness`.

### Input Controls

| Control | Purpose |
|---------|---------|
| Button/ImageButton | Clickable actions |
| CheckBox/Switch | Boolean selection |
| RadioButton | Mutually exclusive options |
| Entry | Single-line text |
| Editor | Multi-line text (`AutoSize="TextChanges"`) |
| Picker | Drop-down selection |
| DatePicker/TimePicker | Date/time selection |
| Slider/Stepper | Numeric value selection |
| SearchBar | Search input with icon |

### FontImageSource

**CRITICAL**: When using `FontImageSource` for icons, use `Size` property, NOT `FontSize`:

```xml
<!-- CORRECT -->
<Button.ImageSource>
    <FontImageSource Glyph="↻" Size="20" Color="White" />
</Button.ImageSource>

<!-- WRONG - Will cause XamlParseException -->
<Button.ImageSource>
    <FontImageSource Glyph="↻" FontSize="20" Color="White" />
</Button.ImageSource>
```

**Why**: `FontImageSource` uses `Size` property while text controls like `Label` and `Button` use `FontSize`. This is a common naming inconsistency in .NET MAUI.

### List & Data Display

| Control | When to Use |
|---------|-------------|
| **CollectionView** | Lists >20 items (virtualized); **never in StackLayout** |
| BindableLayout | Small lists ≤20 items (no virtualization) |
| CarouselView + IndicatorView | Galleries, onboarding, image sliders |

### Interactive Controls

- **RefreshView**: Pull-to-refresh wrapper
- **SwipeView**: Swipe gestures for contextual actions

### Display Controls

- **Image**: Use PNG references (even for SVG sources)
- **Label**: Text with formatting, spans, hyperlinks
- **WebView**: Web content/HTML
- **GraphicsView**: Custom drawing via ICanvas
- **Map**: Interactive maps with pins

## Best Practices

### Layouts

```xml
<!-- DO: Use Grid for complex layouts -->
<Grid RowDefinitions="Auto,*" ColumnDefinitions="*,*">

<!-- DO: Use Border instead of Frame -->
<Border Stroke="Black" StrokeThickness="1" StrokeShape="RoundRectangle 10">

<!-- DO: Use specific stack layouts -->
<VerticalStackLayout> <!-- Not <StackLayout Orientation="Vertical"> -->
```

### Compiled Bindings (Critical for Performance)

```xml
<!-- Always use x:DataType for 8-20x performance improvement -->
<ContentPage x:DataType="vm:MainViewModel">
    <Label Text="{Binding Name}" />
</ContentPage>
```

```csharp
// DO: Expression-based bindings (type-safe, compiled)
label.SetBinding(Label.TextProperty, static (PersonViewModel vm) => vm.FullName?.FirstName);

// DON'T: String-based bindings (runtime errors, no IntelliSense)
label.SetBinding(Label.TextProperty, "FullName.FirstName");
```

### Binding Modes

- `OneTime` - data won't change
- `OneWay` - default, read-only
- `TwoWay` - only when needed (editable)
- Don't bind static values - set directly

### Handler Customization

```csharp
// In MauiProgram.cs ConfigureMauiHandlers
Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("Custom", (handler, view) =>
{
#if ANDROID
    handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.HotPink);
#elif IOS
    handler.PlatformView.BackgroundColor = UIKit.UIColor.SystemPink;
#endif
});
```

### Shell Navigation (Recommended)

```csharp
Routing.RegisterRoute("details", typeof(DetailPage));
await Shell.Current.GoToAsync("details?id=123");
```

- Set `MainPage` once at startup
- Don't nest tabs

### Platform Code

**Prefer partial classes and methods** over `#if` preprocessor directives for platform-specific implementations. This keeps platform code isolated and improves readability.

#### Partial Classes Pattern (Preferred)

Define a `partial class` with a `partial method` declaration in the shared code (e.g., `Services/`):

```csharp
namespace MyApp.Services;

public partial class MyService
{
    // Shared code here...

    // Platform-specific method declaration (no body)
    private static partial Task<string?> GetPlatformValueAsync();
}
```

Implement the partial method in each platform folder (`Platforms/Windows/`, `Platforms/Android/`, etc.):

```csharp
// Platforms/Windows/MyService.cs
namespace MyApp.Services;

public partial class MyService
{
    private static partial Task<string?> GetPlatformValueAsync()
    {
        // Windows-specific implementation
        return Task.FromResult<string?>("Windows");
    }
}
```

The build system automatically includes only the correct platform implementation based on the target framework.

#### Conditional Compilation (Use Sparingly)

Only use `#if` directives for small, inline platform differences (e.g., in handler customization):

```csharp
#if ANDROID
#elif IOS
#elif WINDOWS
#elif MACCATALYST
#endif
```

- Prefer `BindableObject.Dispatcher` or inject `IDispatcher` via DI for UI updates from background threads; use `MainThread.BeginInvokeOnMainThread()` as a fallback

### Performance

1. Use compiled bindings (`x:DataType`)
2. Use Grid > StackLayout, CollectionView > ListView, Border > Frame

### Security

```csharp
await SecureStorage.SetAsync("oauth_token", token);
string token = await SecureStorage.GetAsync("oauth_token");
```

- Never commit secrets
- Validate inputs
- Use HTTPS

### Resources

- `Resources/Images/` - images (PNG, JPG, SVG→PNG)
- `Resources/Fonts/` - custom fonts
- `Resources/Raw/` - raw assets
- Reference images as PNG: `<Image Source="logo.png" />` (not .svg)
- Use appropriate sizes to avoid memory bloat

## Common Pitfalls

1. Mixing Shell with NavigationPage/TabbedPage/FlyoutPage
2. Changing MainPage frequently
3. Nesting tabs
4. Gesture recognizers on parent and child (use `InputTransparent = true`)
5. Using renderers instead of handlers
6. Memory leaks from unsubscribed events
7. Deeply nested layouts (flatten hierarchy)
8. Testing only on emulators - test on actual devices
9. Some Xamarin.Forms APIs not yet in MAUI - check GitHub issues

## Reference Documentation

- [Controls](https://learn.microsoft.com/dotnet/maui/user-interface/controls/)
- [XAML](https://learn.microsoft.com/dotnet/maui/xaml/)
- [Data Binding](https://learn.microsoft.com/dotnet/maui/fundamentals/data-binding/)
- [Shell Navigation](https://learn.microsoft.com/dotnet/maui/fundamentals/shell/)
- [Handlers](https://learn.microsoft.com/dotnet/maui/user-interface/handlers/)
- [Performance](https://learn.microsoft.com/dotnet/maui/deployment/performance)

## Your Role

1. **Recommend best practices** - proper control selection
2. **Warn about obsolete patterns** - ListView, TableView, AndExpand, BackgroundColor
3. **Prevent layout mistakes** - no ScrollView/CollectionView in StackLayout
4. **Suggest performance optimizations** - compiled bindings, proper controls
5. **Provide working XAML examples** with modern patterns
6. **Consider cross-platform implications**

## General Practices

Avoid allocations at all costs! Code this like it is a fighter jet (no allocations, no exceptions) within reason. Allocations should happen at app start.

Always use compiled bindings in xaml

## iOS Simulator Automation (Appium)

Use Appium for UI testing and validation on iOS simulators. This is useful for verifying gesture fixes and testing user flows.

### Setup with MCP Tools

1. **Select platform and device**:

   ```text
   mcp_appium-mcp_select_platform (platform: "ios", iosDeviceType: "simulator")
   mcp_appium-mcp_select_device (platform: "ios", deviceUdid: "<UDID>")
   ```

2. **Boot simulator** (if not already running):

   ```text
   mcp_appium-mcp_boot_simulator (udid: "<UDID>")
   ```

3. **Setup and install WebDriverAgent** (first time only):

   ```text
   mcp_appium-mcp_setup_wda (platform: "ios")
   mcp_appium-mcp_install_wda (simulatorUdid: "<UDID>")
   ```

### Build and Install App

```bash
# Build for iOS simulator (use the build task or command)
dotnet build apps/twenty-forty-eight/src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-ios -c Debug

# Get bundle ID from built app
defaults read "apps/twenty-forty-eight/src/TwentyFortyEight.Maui/bin/Debug/net10.0-ios/iossimulator-arm64/TwentyFortyEight.Maui.app/Info.plist" CFBundleIdentifier

# Install on simulator
xcrun simctl install <UDID> "apps/twenty-forty-eight/src/TwentyFortyEight.Maui/bin/Debug/net10.0-ios/iossimulator-arm64/TwentyFortyEight.Maui.app"

# Launch app
xcrun simctl launch <UDID> com.dappermagna.twentyfortyeight
```

### Running Appium Server (Recommended Approach)

The most reliable method is to start a standalone Appium server and use curl for API calls:

```bash
# Start Appium server in background
nohup npx -y appium@latest --port 4723 --address 127.0.0.1 --relaxed-security > /tmp/appium.log 2>&1 &

# Verify server is running
curl -s http://127.0.0.1:4723/status
# Expected: {"value":{"ready":true,"message":"The server is ready to accept new connections"...}}
```

### Creating an Appium Session

```bash
# Create session (returns sessionId)
curl -s -X POST http://127.0.0.1:4723/session \
  -H "Content-Type: application/json" \
  -d '{"capabilities":{"alwaysMatch":{"platformName":"iOS","appium:automationName":"XCUITest","appium:udid":"<UDID>","appium:bundleId":"com.dappermagna.twentyfortyeight","appium:noReset":true}}}'

# Extract session ID from response for subsequent calls
SESSION_ID="<from response>"
```

### Appium API Commands

```bash
# Get page source (UI hierarchy as XML)
curl -s "http://127.0.0.1:4723/session/${SESSION_ID}/source"

# Take screenshot (returns base64 PNG)
curl -s "http://127.0.0.1:4723/session/${SESSION_ID}/screenshot"

# Find element by accessibility ID
curl -s -X POST "http://127.0.0.1:4723/session/${SESSION_ID}/element" \
  -H "Content-Type: application/json" \
  -d '{"using":"accessibility id","value":"ToolbarNewGameButton"}'

# Click element
curl -s -X POST "http://127.0.0.1:4723/session/${SESSION_ID}/element/${ELEMENT_ID}/click" \
  -H "Content-Type: application/json" -d '{}'

# Delete session when done
curl -s -X DELETE "http://127.0.0.1:4723/session/${SESSION_ID}"
```

### W3C Actions for Gestures

Use W3C Actions API for swipe gestures. This is critical for testing gesture recognition:

```bash
# Fast swipe DOWN (start at y=350, end at y=800)
curl -s -X POST "http://127.0.0.1:4723/session/${SESSION_ID}/actions" \
  -H "Content-Type: application/json" \
  -d '{"actions":[{"type":"pointer","id":"finger1","parameters":{"pointerType":"touch"},"actions":[{"type":"pointerMove","duration":0,"x":200,"y":350},{"type":"pointerDown","button":0},{"type":"pointerMove","duration":100,"x":200,"y":800},{"type":"pointerUp","button":0}]}]}'

# Fast swipe UP (start at y=550, end at y=100)
curl -s -X POST "http://127.0.0.1:4723/session/${SESSION_ID}/actions" \
  -H "Content-Type: application/json" \
  -d '{"actions":[{"type":"pointer","id":"finger1","parameters":{"pointerType":"touch"},"actions":[{"type":"pointerMove","duration":0,"x":200,"y":550},{"type":"pointerDown","button":0},{"type":"pointerMove","duration":100,"x":200,"y":100},{"type":"pointerUp","button":0}]}]}'

# Fast swipe LEFT (start at x=350, end at x=-50 - exits view bounds)
curl -s -X POST "http://127.0.0.1:4723/session/${SESSION_ID}/actions" \
  -H "Content-Type: application/json" \
  -d '{"actions":[{"type":"pointer","id":"finger1","parameters":{"pointerType":"touch"},"actions":[{"type":"pointerMove","duration":0,"x":350,"y":400},{"type":"pointerDown","button":0},{"type":"pointerMove","duration":80,"x":-50,"y":400},{"type":"pointerUp","button":0}]}]}'

# Fast swipe RIGHT (start at x=50, end at x=450 - exits view bounds)
curl -s -X POST "http://127.0.0.1:4723/session/${SESSION_ID}/actions" \
  -H "Content-Type: application/json" \
  -d '{"actions":[{"type":"pointer","id":"finger1","parameters":{"pointerType":"touch"},"actions":[{"type":"pointerMove","duration":0,"x":50,"y":450},{"type":"pointerDown","button":0},{"type":"pointerMove","duration":80,"x":450,"y":450},{"type":"pointerUp","button":0}]}]}'
```

**Key parameters:**

- `duration`: Swipe speed in ms (lower = faster). Use 80-100ms for fast swipes
- Coordinates can exceed view bounds to test edge cases (e.g., x=-50 or y=900)

### Verifying Board State

The game board exposes its state via accessibility description:

```bash
# Parse board state from page source
curl -s "http://127.0.0.1:4723/session/${SESSION_ID}/source" | \
  python3 -c "import sys,re; m=re.search(r'Game board[^\"]*', sys.stdin.read()); print(m.group(0) if m else 'Not found')"
```

Example output: `Game board. Row 1:4, 2, empty, empty. Row 2:empty, empty, empty, empty...`

### Screenshots with simctl

```bash
# Take screenshot directly via simctl
xcrun simctl io <UDID> screenshot /tmp/screenshot.png

# Open screenshot
open /tmp/screenshot.png
```

### Common Element Accessibility IDs

| Element            | Accessibility ID             |
| ------------------ | ---------------------------- |
| New Game button    | `ToolbarNewGameButton`       |
| Mode button        | `ToolbarModeButton`          |
| More menu          | `SecondaryToolbarMenuButton` |
| Undo button        | `Undo last move`             |
| Game board         | `Game board. Row 1:...`      |
| Start New (dialog) | `Start New`                  |
| Cancel (dialog)    | `Cancel`                     |

### Screen Dimensions (iPhone 17 Pro Simulator)

- Screen: 402 x 874 points
- Navigation bar: y=62 to y=116
- Game board: x=16, y=299, width=370, height=372
- Bottom controls: y=768+

### Troubleshooting

1. **"No driver found" from MCP tools**: Use curl with standalone Appium server instead
2. **Server not responding**: Check if Appium is running with `ps aux | grep appium`
3. **Session creation fails**: Ensure WDA is installed and app is already running
4. **Swipes not detected**: Verify coordinates are within/near the game board area
