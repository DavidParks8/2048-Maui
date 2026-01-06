namespace TwentyFortyEight.Core;

/// <summary>
/// Configuration for a 2048 game.
/// </summary>
public class GameConfig
{
    private const string RulesetIdVersionPrefix = "v1";
    private const string DefaultSpawnModeId = "default";
    private const string DefaultUndoModeId = "on";

    /// <summary>
    /// Maximum reasonable board size. Larger sizes may cause performance issues.
    /// </summary>
    public const int MaxReasonableBoardSize = 64;

    /// <summary>
    /// Size of the board (default 4x4).
    /// </summary>
    public int Size { get; init; } = 4;

    /// <summary>
    /// Tile value required to win (default 2048).
    /// </summary>
    public int WinTile { get; init; } = 2048;

    /// <summary>
    /// Stable identifier for persistence and scoping.
    /// </summary>
    public string RulesetId =>
        $"{RulesetIdVersionPrefix}:size={Size};win={WinTile};spawn={DefaultSpawnModeId};undo={DefaultUndoModeId}";
}
