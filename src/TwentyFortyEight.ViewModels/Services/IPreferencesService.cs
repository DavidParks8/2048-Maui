namespace TwentyFortyEight.ViewModels.Services;

/// <summary>
/// Abstraction for platform preferences/settings storage.
/// </summary>
public interface IPreferencesService
{
    /// <summary>
    /// Gets a string value from preferences.
    /// </summary>
    string GetString(string key, string defaultValue = "");

    /// <summary>
    /// Sets a string value in preferences.
    /// </summary>
    void SetString(string key, string value);

    /// <summary>
    /// Gets an integer value from preferences.
    /// </summary>
    int GetInt(string key, int defaultValue = 0);

    /// <summary>
    /// Sets an integer value in preferences.
    /// </summary>
    void SetInt(string key, int value);

    /// <summary>
    /// Gets a boolean value from preferences.
    /// </summary>
    bool GetBool(string key, bool defaultValue = false);

    /// <summary>
    /// Sets a boolean value in preferences.
    /// </summary>
    void SetBool(string key, bool value);

    /// <summary>
    /// Gets a double value from preferences.
    /// </summary>
    double GetDouble(string key, double defaultValue = 0.0);

    /// <summary>
    /// Sets a double value in preferences.
    /// </summary>
    void SetDouble(string key, double value);
}
