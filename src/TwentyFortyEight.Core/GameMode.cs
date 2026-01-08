namespace TwentyFortyEight.Core;

/// <summary>
/// Available game modes.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Standard 2048 rules.
    /// </summary>
    Classic = 0,

    /// <summary>
    /// Each successful move replaces a between-cell wall segment that blocks movement/merges across it.
    /// </summary>
    Walltastrophy = 1,
}
