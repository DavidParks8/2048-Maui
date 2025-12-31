using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of ISettingsService using Preferences.
/// </summary>
public class MauiSettingsService : ISettingsService
{
    private const string HapticsEnabledKey = "HapticsEnabled";

    /// <inheritdoc />
    public bool HapticsEnabled
    {
        get => Preferences.Get(HapticsEnabledKey, true);
        set => Preferences.Set(HapticsEnabledKey, value);
    }
}
