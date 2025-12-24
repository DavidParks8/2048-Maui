using CommunityToolkit.Mvvm.ComponentModel;
using TwentyFortyEight.Maui.Services;

namespace TwentyFortyEight.Maui.ViewModels;

/// <summary>
/// ViewModel for the settings page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private bool _animationsEnabled;

    [ObservableProperty]
    private double _animationSpeed;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Load current settings
        _animationsEnabled = _settingsService.AnimationsEnabled;
        _animationSpeed = _settingsService.AnimationSpeed;
    }

    partial void OnAnimationsEnabledChanged(bool value)
    {
        _settingsService.AnimationsEnabled = value;
    }

    partial void OnAnimationSpeedChanged(double value)
    {
        _settingsService.AnimationSpeed = value;
    }
}
