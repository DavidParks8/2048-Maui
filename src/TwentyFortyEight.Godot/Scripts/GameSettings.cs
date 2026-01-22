using Godot;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Persistent game settings and preferences.
/// Uses Godot's ConfigFile for storage.
/// </summary>
public partial class GameSettings : Node
{
    private const string SettingsPath = "user://settings.cfg";
    private const string SettingsSection = "settings";
    private const string GameSection = "game";

    private readonly ConfigFile _config = new();

    public static GameSettings? Instance { get; private set; }

    // Settings properties
    public bool HapticsEnabled
    {
        get => (bool)_config.GetValue(SettingsSection, "haptics_enabled", true);
        set
        {
            _config.SetValue(SettingsSection, "haptics_enabled", value);
            Save();
        }
    }

    public bool CoachEnabled
    {
        get => (bool)_config.GetValue(SettingsSection, "coach_enabled", false);
        set
        {
            _config.SetValue(SettingsSection, "coach_enabled", value);
            Save();
        }
    }

    public bool CoachNudgesEnabled
    {
        get => (bool)_config.GetValue(SettingsSection, "coach_nudges_enabled", true);
        set
        {
            _config.SetValue(SettingsSection, "coach_nudges_enabled", value);
            Save();
        }
    }

    public bool UndoButtonVisible
    {
        get => (bool)_config.GetValue(SettingsSection, "undo_button_visible", true);
        set
        {
            _config.SetValue(SettingsSection, "undo_button_visible", value);
            Save();
        }
    }

    public bool IsDarkTheme
    {
        get => (bool)_config.GetValue(SettingsSection, "dark_theme", false);
        set
        {
            _config.SetValue(SettingsSection, "dark_theme", value);
            Save();
            ThemeChanged?.Invoke();
        }
    }

    // Last active game configuration
    public int LastBoardSize
    {
        get => (int)_config.GetValue(GameSection, "last_board_size", 4);
        set
        {
            _config.SetValue(GameSection, "last_board_size", value);
            Save();
        }
    }

    public GameMode LastGameMode
    {
        get =>
            (GameMode)(int)_config.GetValue(GameSection, "last_game_mode", (int)GameMode.Classic);
        set
        {
            _config.SetValue(GameSection, "last_game_mode", (int)value);
            Save();
        }
    }

    public GameConfig GetLastActiveGameConfig()
    {
        return new GameConfig
        {
            Size = LastBoardSize,
            Mode = LastGameMode,
            WinTile = 2048,
        };
    }

    public void SetLastActiveGameConfig(GameConfig config)
    {
        LastBoardSize = config.Size;
        LastGameMode = config.Mode;
    }

    public event Action? ThemeChanged;

    public override void _EnterTree()
    {
        Instance = this;
        Load();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Load()
    {
        var error = _config.Load(SettingsPath);
        if (error != Error.Ok && error != Error.FileNotFound)
        {
            GD.PrintErr($"Failed to load settings: {error}");
        }
    }

    private void Save()
    {
        var error = _config.Save(SettingsPath);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to save settings: {error}");
        }
    }
}
