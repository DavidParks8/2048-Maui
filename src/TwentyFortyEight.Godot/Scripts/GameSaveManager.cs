using System.Text.Json;
using Godot;
using TwentyFortyEight.Core;
using GodotFileAccess = Godot.FileAccess;

namespace TwentyFortyEight.Godot;

/// <summary>
/// Handles saving and loading game state for persistence.
/// </summary>
public partial class GameSaveManager : Node
{
    private const string SaveDirectory = "user://saves";

    public static GameSaveManager? Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
        EnsureSaveDirectoryExists();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    private static void EnsureSaveDirectoryExists()
    {
        using var dir = DirAccess.Open("user://");
        if (dir != null && !dir.DirExists("saves"))
        {
            dir.MakeDir("saves");
        }
    }

    public void SaveGame(GameConfig config, GameSave save)
    {
        try
        {
            var path = GetSavePath(config);
            var json = JsonSerializer.Serialize(save, GameSaveJsonContext.Default.GameSave);

            using var file = GodotFileAccess.Open(path, GodotFileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(json);
            }
            else
            {
                GD.PrintErr($"Failed to open save file for writing: {path}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save game: {ex.Message}");
        }
    }

    public GameSave? LoadGame(GameConfig config)
    {
        try
        {
            var path = GetSavePath(config);
            if (!GodotFileAccess.FileExists(path))
                return null;

            using var file = GodotFileAccess.Open(path, GodotFileAccess.ModeFlags.Read);
            if (file == null)
                return null;

            var json = file.GetAsText();
            return JsonSerializer.Deserialize(json, GameSaveJsonContext.Default.GameSave);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load game: {ex.Message}");
            return null;
        }
    }

    public void ClearSavedGame(GameConfig config)
    {
        var path = GetSavePath(config);
        if (GodotFileAccess.FileExists(path))
        {
            using var dir = DirAccess.Open(SaveDirectory);
            dir?.Remove(GetSaveFileName(config));
        }
    }

    public int GetBestScore(GameConfig config)
    {
        var path = GetBestScorePath(config);
        if (!GodotFileAccess.FileExists(path))
            return 0;

        using var file = GodotFileAccess.Open(path, GodotFileAccess.ModeFlags.Read);
        if (file == null)
            return 0;

        return (int)file.Get64();
    }

    public void UpdateBestScoreIfHigher(GameConfig config, int score)
    {
        var current = GetBestScore(config);

        // For adversarial mode, lower is better
        bool shouldUpdate =
            config.Mode == GameMode.Adversarial
                ? (current == 0 || score < current)
                : score > current;

        if (shouldUpdate)
        {
            var path = GetBestScorePath(config);
            using var file = GodotFileAccess.Open(path, GodotFileAccess.ModeFlags.Write);
            file?.Store64((ulong)score);
        }
    }

    private static string GetSavePath(GameConfig config)
    {
        return $"{SaveDirectory}/{GetSaveFileName(config)}";
    }

    private static string GetSaveFileName(GameConfig config)
    {
        var rulesetId = config.RulesetId;
        var sanitized = string.IsNullOrEmpty(rulesetId)
            ? "default"
            : rulesetId.Replace(";", "_").Replace("=", "-");
        return $"game_{sanitized}.json";
    }

    private static string GetBestScorePath(GameConfig config)
    {
        var rulesetId = config.RulesetId;
        var sanitized = string.IsNullOrEmpty(rulesetId)
            ? "default"
            : rulesetId.Replace(";", "_").Replace("=", "-");
        return $"{SaveDirectory}/best_{sanitized}.dat";
    }
}

/// <summary>
/// JSON serialization context for AOT compatibility.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(GameSave))]
[System.Text.Json.Serialization.JsonSerializable(typeof(GameStateDto))]
[System.Text.Json.Serialization.JsonSerializable(typeof(MoveRecord))]
[System.Text.Json.Serialization.JsonSerializable(typeof(List<MoveRecord>))]
internal partial class GameSaveJsonContext
    : System.Text.Json.Serialization.JsonSerializerContext { }
