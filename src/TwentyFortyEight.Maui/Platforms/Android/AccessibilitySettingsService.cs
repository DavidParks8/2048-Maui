using Android.Content;
using Android.Provider;
using Android.Views.Accessibility;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

public sealed class AccessibilitySettingsService : IAccessibilitySettingsService, IDisposable
{
    private readonly AccessibilityManager? _accessibilityManager;
    private readonly AccessibilityStateChangeListener? _stateChangeListener;

    public event EventHandler? AccessibilitySettingsChanged;

    public AccessibilitySettingsService()
    {
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        if (context != null)
        {
            _accessibilityManager =
                context.GetSystemService(Context.AccessibilityService) as AccessibilityManager;

            if (_accessibilityManager != null)
            {
                _stateChangeListener = new AccessibilityStateChangeListener(this);
                _accessibilityManager.AddAccessibilityStateChangeListener(_stateChangeListener);
                _accessibilityManager.AddTouchExplorationStateChangeListener(_stateChangeListener);
            }
        }
    }

    public bool ShouldReduceMotion()
    {
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        if (context?.ContentResolver == null)
            return false;

        try
        {
            float scale = Settings.Global.GetFloat(
                context.ContentResolver,
                Settings.Global.AnimatorDurationScale,
                1f
            );
            return scale == 0f;
        }
        catch
        {
            return false;
        }
    }

    public bool IsVoiceControlEnabled()
    {
        // On Android, treat TalkBack (screen reader) as equivalent to voice control
        // since it provides hands-free accessibility features.
        try
        {
            if (_accessibilityManager == null)
                return false;

            return _accessibilityManager.IsEnabled
                && _accessibilityManager.IsTouchExplorationEnabled;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_accessibilityManager != null && _stateChangeListener != null)
        {
            _accessibilityManager.RemoveAccessibilityStateChangeListener(_stateChangeListener);
            _accessibilityManager.RemoveTouchExplorationStateChangeListener(_stateChangeListener);
        }
    }

    private sealed class AccessibilityStateChangeListener(AccessibilitySettingsService service)
        : Java.Lang.Object,
            AccessibilityManager.IAccessibilityStateChangeListener,
            AccessibilityManager.ITouchExplorationStateChangeListener
    {
        public void OnAccessibilityStateChanged(bool enabled)
        {
            service.AccessibilitySettingsChanged?.Invoke(service, EventArgs.Empty);
        }

        public void OnTouchExplorationStateChanged(bool enabled)
        {
            service.AccessibilitySettingsChanged?.Invoke(service, EventArgs.Empty);
        }
    }
}
