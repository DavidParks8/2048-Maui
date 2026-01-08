namespace TwentyFortyEight.Core;

/// <summary>
/// Configuration for a 2048 game.
/// </summary>
public class GameConfig
{
    private string? _rulesetId;

    /// <summary>
    /// Maximum reasonable board size. Larger sizes may cause performance issues.
    /// </summary>
    public const int MaxReasonableBoardSize = 64;

    private const int DefaultBoardSize = 4;

    private const int DefaultWinTile = 2048;

    private const GameMode DefaultMode = GameMode.Classic;

    /// <summary>
    /// Size of the board (default 4x4).
    /// </summary>
    public int Size { get; init; } = DefaultBoardSize;

    /// <summary>
    /// Tile value required to win (default 2048).
    /// </summary>
    public int WinTile { get; init; } = DefaultWinTile;

    /// <summary>
    /// Game mode (default Classic).
    /// </summary>
    public GameMode Mode { get; init; } = DefaultMode;

    /// <summary>
    /// Stable identifier for persistence and scoping.
    /// </summary>
    public string RulesetId
    {
        get => _rulesetId ??= BuildRulesetId();
    }

    private string BuildRulesetId()
    {
        List<string> parts = [];

        if (Size != DefaultBoardSize)
        {
            parts.Add($"size={Size}");
        }

        if (WinTile != DefaultWinTile)
        {
            parts.Add($"win={WinTile}");
        }

        if (Mode != DefaultMode)
        {
            parts.Add($"mode={GetModeId(Mode)}");
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(';', parts);
    }

    private static string GetModeId(GameMode mode) =>
        mode switch
        {
            GameMode.Walltastrophy => "walltastrophy",
            _ => "classic",
        };
}
