namespace TwentyFortyEight.Core;

/// <summary>
/// Configuration for a 2048 game.
/// </summary>
public class GameConfig
{
    /// <summary>
    /// Size of the board (default 4x4).
    /// </summary>
    public int Size { get; init; } = 4;

    /// <summary>
    /// Tile value required to win (default 2048).
    /// </summary>
    public int WinTile { get; init; } = 2048;

    /// <summary>
    /// Whether the player can continue playing after winning (default true).
    /// </summary>
    public bool AllowContinueAfterWin { get; init; } = true;
}
