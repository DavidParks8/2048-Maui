using TwentyFortyEight.ViewModels.Services;
using Windows.UI.ViewManagement;

namespace TwentyFortyEight.Maui.Services;

public sealed class AccessibilitySettingsService : IAccessibilitySettingsService, IDisposable
{
    private readonly UISettings _uiSettings;

    public event EventHandler? AccessibilitySettingsChanged;

    public AccessibilitySettingsService()
    {
        _uiSettings = new UISettings();

        // Subscribe to accessibility setting changes
        _uiSettings.AnimationsEnabledChanged += OnAccessibilitySettingChanged;
        _uiSettings.AccessibilitySettingsChanged += OnAccessibilitySettingChanged;
    }

    private void OnAccessibilitySettingChanged(UISettings sender, object args)
    {
        AccessibilitySettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool ShouldReduceMotion()
    {
        try
        {
            return !_uiSettings.AnimationsEnabled;
        }
        catch
        {
            return false;
        }
    }

    public bool IsVoiceControlEnabled()
    {
        try
        {
            // On Windows, treat the Narrator screen reader as equivalent to voice control
            // since it provides hands-free accessibility features.
            return _uiSettings.ScreenReaderEnabled;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _uiSettings.AnimationsEnabledChanged -= OnAccessibilitySettingChanged;
        _uiSettings.AccessibilitySettingsChanged -= OnAccessibilitySettingChanged;
    }
}
