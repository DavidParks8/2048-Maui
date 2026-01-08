using Foundation;
using TwentyFortyEight.ViewModels.Services;
using UIKit;

namespace TwentyFortyEight.Maui.Services;

public sealed class AccessibilitySettingsService : IAccessibilitySettingsService, IDisposable
{
    private readonly NSObject? _reduceMotionObserver;
    private readonly NSObject? _voiceControlObserver;
    private readonly NSObject? _appDidBecomeActiveObserver;

    public event EventHandler? AccessibilitySettingsChanged;

    public AccessibilitySettingsService()
    {
        // Notification constants are not consistently exposed across bindings.
        // Use best-effort string notification names so this compiles across SDK variations.
        _reduceMotionObserver = AddObserverBestEffort(
            "UIAccessibilityReduceMotionStatusDidChangeNotification"
        );

        _voiceControlObserver = AddObserverBestEffort(
            "UIAccessibilityVoiceOverStatusDidChangeNotification"
        );

        // Re-check accessibility settings when app becomes active (returns to foreground)
        // This provides a more reliable fallback if the specific accessibility notifications don't fire
        _appDidBecomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.DidBecomeActiveNotification,
            _ => AccessibilitySettingsChanged?.Invoke(this, EventArgs.Empty)
        );
    }

    public bool ShouldReduceMotion() => UIAccessibility.IsReduceMotionEnabled;

    public bool IsVoiceControlEnabled() =>
        UIAccessibility.IsVoiceOverRunning || UIAccessibility.IsSpeakScreenEnabled;

    private NSObject? AddObserverBestEffort(string notificationName)
    {
        try
        {
            return NSNotificationCenter.DefaultCenter.AddObserver(
                new NSString(notificationName),
                _ => AccessibilitySettingsChanged?.Invoke(this, EventArgs.Empty)
            );
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_reduceMotionObserver is not null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_reduceMotionObserver);
        }

        if (_voiceControlObserver is not null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_voiceControlObserver);
        }

        if (_appDidBecomeActiveObserver is not null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_appDidBecomeActiveObserver);
        }
    }
}
