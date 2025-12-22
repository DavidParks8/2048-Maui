# Agent Instructions

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

### Theme Support

Both light and dark mode must be supported:

- Use `{AppThemeBinding Light=..., Dark=...}` for colors in XAML
- Define theme-aware colors in `Resources/Styles/Colors.xaml`
- Test UI in both light and dark modes

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
