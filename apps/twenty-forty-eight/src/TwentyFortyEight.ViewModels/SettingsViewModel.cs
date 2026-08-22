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
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private bool _hapticsEnabled;

    [ObservableProperty]
    private bool _coachEnabled;

    [ObservableProperty]
    private bool _coachNudgesEnabled;

    [ObservableProperty]
    private bool _undoButtonVisible;

    /// <summary>
    /// Gets a value indicating whether haptic feedback is supported on this device.
    /// </summary>
    public bool IsHapticsSupported => _hapticService.IsSupported;

    public SettingsViewModel(
        ISettingsService settingsService,
        IHapticService hapticService,
        IMessenger messenger
    )
    {
        _settingsService = settingsService;
        _hapticService = hapticService;
        _messenger = messenger;

        // Load current settings
        _hapticsEnabled = _settingsService.HapticsEnabled;
        _coachEnabled = _settingsService.CoachEnabled;
        _coachNudgesEnabled = _settingsService.CoachNudgesEnabled;
        _undoButtonVisible = _settingsService.UndoButtonVisible;
    }

    partial void OnHapticsEnabledChanged(bool value)
    {
        _settingsService.HapticsEnabled = value;
    }

    partial void OnCoachEnabledChanged(bool value)
    {
        _settingsService.CoachEnabled = value;
        _messenger.Send(new CoachEnabledChangedMessage(value));
    }

    partial void OnCoachNudgesEnabledChanged(bool value)
    {
        _settingsService.CoachNudgesEnabled = value;
        _messenger.Send(new CoachNudgesEnabledChangedMessage(value));
    }

    partial void OnUndoButtonVisibleChanged(bool value)
    {
        _settingsService.UndoButtonVisible = value;
        _messenger.Send(new UndoButtonVisibilityChangedMessage(value));
    }
}
