using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using TwentyFortyEight.ViewModels.Messages;
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

    [ObservableProperty]
    private bool _coachEnabled;

    [ObservableProperty]
    private bool _coachNudgesEnabled;

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
        _coachEnabled = _settingsService.CoachEnabled;
        _coachNudgesEnabled = _settingsService.CoachNudgesEnabled;
    }

    partial void OnHapticsEnabledChanged(bool value)
    {
        _settingsService.HapticsEnabled = value;
    }

    partial void OnCoachEnabledChanged(bool value)
    {
        _settingsService.CoachEnabled = value;
        WeakReferenceMessenger.Default.Send(new CoachEnabledChangedMessage(value));
    }

    partial void OnCoachNudgesEnabledChanged(bool value)
    {
        _settingsService.CoachNudgesEnabled = value;
        WeakReferenceMessenger.Default.Send(new CoachNudgesEnabledChangedMessage(value));
    }
}
