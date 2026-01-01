namespace TwentyFortyEight.Core;

/// <summary>
/// Event args raised when the player achieves victory (reaches 2048 for the first time in a game).
/// </summary>
public class VictoryEventArgs : EventArgs
{
    /// <summary>
    /// The row of the winning tile (0-indexed).
    /// </summary>
    public required int WinningTileRow { get; init; }

    /// <summary>
    /// The column of the winning tile (0-indexed).
    /// </summary>
    public required int WinningTileColumn { get; init; }
}
