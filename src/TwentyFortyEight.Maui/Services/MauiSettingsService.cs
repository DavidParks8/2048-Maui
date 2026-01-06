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
    private const string LastActiveBoardSizeKey = "LastActiveBoardSize";
    private const string LastActiveGameConfigKey = "LastActiveGameConfig";

    /// <inheritdoc />
    public bool HapticsEnabled
    {
        get => Preferences.Get(HapticsEnabledKey, true);
        set => Preferences.Set(HapticsEnabledKey, value);
    }

    /// <inheritdoc />
    public int LastActiveBoardSize
    {
        get => Preferences.Get(LastActiveBoardSizeKey, 4);
        set => Preferences.Set(LastActiveBoardSizeKey, value);
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
                        return config;
                    }
                }
                catch
                {
                    // Fall back to defaults.
                }
            }

            // Migration: if we have a legacy board-size-only setting, upgrade it.
            var size = Preferences.Get(LastActiveBoardSizeKey, 4);
            var migrated = new GameConfig { Size = size };
            LastActiveGameConfig = migrated;
            return migrated;
        }
        set
        {
            var config = value ?? new GameConfig();
            var json = JsonSerializer.Serialize(
                config,
                GameSerializationContext.Default.GameConfig
            );
            Preferences.Set(LastActiveGameConfigKey, json);

            // Keep legacy key in sync for smooth downgrades.
            Preferences.Set(LastActiveBoardSizeKey, config.Size);
        }
    }
}
