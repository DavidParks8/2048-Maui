using CommunityToolkit.Mvvm.ComponentModel;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.ViewModels;

/// <summary>
/// ViewModel for the settings page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IHapticService _hapticService;

    [ObservableProperty]
    private bool _animationsEnabled;

    [ObservableProperty]
    private double _animationSpeed;

    [ObservableProperty]
    private bool _hapticsEnabled;

    /// <summary>
    /// Gets a value indicating whether haptic feedback is supported on this device.
    /// </summary>
    public bool IsHapticsSupported => _hapticService.IsSupported;

    public SettingsViewModel(ISettingsService settingsService, IHapticService hapticService)
    {
        _settingsService = settingsService;
        _hapticService = hapticService;

        // Load current settings
        _animationsEnabled = _settingsService.AnimationsEnabled;
        _animationSpeed = _settingsService.AnimationSpeed;
        _hapticsEnabled = _settingsService.HapticsEnabled;
    }

    partial void OnAnimationsEnabledChanged(bool value)
    {
        _settingsService.AnimationsEnabled = value;
    }

    partial void OnAnimationSpeedChanged(double value)
    {
        _settingsService.AnimationSpeed = value;
    }

    partial void OnHapticsEnabledChanged(bool value)
    {
        _settingsService.HapticsEnabled = value;
    }
}
