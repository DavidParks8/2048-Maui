using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of IPreferencesService using MAUI Preferences API.
/// </summary>
public class MauiPreferencesService : IPreferencesService
{
    /// <inheritdoc />
    public string GetString(string key, string defaultValue = "") =>
        Preferences.Get(key, defaultValue);

    /// <inheritdoc />
    public void SetString(string key, string value) => Preferences.Set(key, value);

    /// <inheritdoc />
    public int GetInt(string key, int defaultValue = 0) => Preferences.Get(key, defaultValue);

    /// <inheritdoc />
    public void SetInt(string key, int value) => Preferences.Set(key, value);

    /// <inheritdoc />
    public bool GetBool(string key, bool defaultValue = false) =>
        Preferences.Get(key, defaultValue);

    /// <inheritdoc />
    public void SetBool(string key, bool value) => Preferences.Set(key, value);

    /// <inheritdoc />
    public double GetDouble(string key, double defaultValue = 0.0) =>
        Preferences.Get(key, defaultValue);

    /// <inheritdoc />
    public void SetDouble(string key, double value) => Preferences.Set(key, value);
}
