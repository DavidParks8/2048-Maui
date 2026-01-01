namespace TwentyFortyEight.ViewModels;

/// <summary>
/// Event args for starting the victory animation.
/// </summary>
public sealed class VictoryAnimationStartEventArgs : EventArgs
{
    /// <summary>
    /// Row of the winning tile.
    /// </summary>
    public required int WinningTileRow { get; init; }

    /// <summary>
    /// Column of the winning tile.
    /// </summary>
    public required int WinningTileColumn { get; init; }

    /// <summary>
    /// Score at the time of victory.
    /// </summary>
    public required int Score { get; init; }
}
