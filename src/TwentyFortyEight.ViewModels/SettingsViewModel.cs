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
        _hapticsEnabled = _settingsService.HapticsEnabled;
    }

    partial void OnHapticsEnabledChanged(bool value)
    {
        _settingsService.HapticsEnabled = value;
    }
}
