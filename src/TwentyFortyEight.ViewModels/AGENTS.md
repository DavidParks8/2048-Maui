# TwentyFortyEight.ViewModels

This project contains ViewModels and MVVM abstractions that can be unit tested independently of MAUI platform dependencies.

## Architecture

This is a .NET MAUI Class Library that targets both:
- `net10.0` - for unit testing with MSTest
- `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows` - for MAUI applications

## Unit Testing

The ViewModels in this library use dependency injection for all platform-specific services, making them fully testable:

### Service Abstractions

| Interface | Purpose | MAUI Implementation |
|-----------|---------|---------------------|
| `IPreferencesService` | Key-value storage | `MauiPreferencesService` |
| `IAlertService` | Dialogs and confirmations | `MauiAlertService` |
| `INavigationService` | Page navigation | `MauiNavigationService` |
| `ISettingsService` | App settings | `MauiSettingsService` |
| `ILocalizationService` | Localized strings | `MauiLocalizationService` |

### Writing Tests

Use Moq to mock the service interfaces:

```csharp
[TestMethod]
public void Constructor_InitializesTilesCollection()
{
    // Arrange - mock all dependencies
    var loggerMock = new Mock<ILogger<GameViewModel>>();
    var preferencesServiceMock = new Mock<IPreferencesService>();
    var alertServiceMock = new Mock<IAlertService>();
    // ... setup mocks

    // Act
    var viewModel = new GameViewModel(
        loggerMock.Object,
        moveAnalyzerMock.Object,
        // ... other mocks
    );

    // Assert
    Assert.HasCount(16, viewModel.Tiles);
}
```

## Conditional Compilation

The `TileViewModel` uses conditional compilation to include MAUI-specific `Color` properties only when targeting MAUI platforms:

```csharp
#if ANDROID || IOS || MACCATALYST || WINDOWS
public Color BackgroundColor => GetTileBackgroundColor(Value);
#endif
```

When targeting `net10.0` for testing, these properties are excluded, allowing the model to be tested without MAUI dependencies.

## Guidelines

- **ViewModels**: Use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm
- **Dependencies**: Inject all platform-specific functionality through interfaces
- **Models**: Use platform-agnostic properties for testability, conditional compilation for MAUI-specific features
