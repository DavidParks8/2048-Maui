namespace TwentyFortyEight.Core;

/// <summary>
/// Available game modes.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Original 2048 ruleset, including classic spawn rules.
    /// New tiles spawn as 2 (90%) or 4 (10%), regardless of progress.
    /// </summary>
    Classic = 0,

    /// <summary>
    /// Each successful move replaces a between-cell wall segment that blocks movement/merges across it.
    /// </summary>
    Walltastrophy = 1,

    /// <summary>
    /// The current/default ruleset.
    ///
    /// Note: This mode is called "Modern" because it does not use the original 2048 spawn rules.
    /// New tiles are spawned using the modern/adaptive spawn strategy.
    /// </summary>
    Modern = 2,
}
