using System.Text.Json;
using TwentyFortyEight.Core;
using TwentyFortyEight.Maui.Serialization;
using TwentyFortyEight.ViewModels.Services;

namespace TwentyFortyEight.Maui.Services;

/// <summary>
/// MAUI-specific implementation of ISettingsService using Preferences.
/// </summary>
public class MauiSettingsService : ISettingsService
{
    private const string HapticsEnabledKey = "HapticsEnabled";
    private const string LastActiveGameConfigKey = "LastActiveGameConfig";

    /// <inheritdoc />
    public bool HapticsEnabled
    {
        get => Preferences.Get(HapticsEnabledKey, true);
        set => Preferences.Set(HapticsEnabledKey, value);
    }

    /// <inheritdoc />
    public GameConfig LastActiveGameConfig
    {
        get
        {
            var json = Preferences.Get(LastActiveGameConfigKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var config = JsonSerializer.Deserialize(
                        json,
                        GameSerializationContext.Default.GameConfig
                    );
                    if (config != null)
                    {
                        if (config.Size <= 0 || config.Size > GameConfig.MaxReasonableBoardSize)
                        {
                            return new();
                        }
                        return config;
                    }
                }
                catch
                {
                    // Fall back to defaults.
                }
            }

            return new();
        }
        set
        {
            GameConfig config = value ?? new();
            var json = JsonSerializer.Serialize(
                config,
                GameSerializationContext.Default.GameConfig
            );
            Preferences.Set(LastActiveGameConfigKey, json);
        }
    }
}
