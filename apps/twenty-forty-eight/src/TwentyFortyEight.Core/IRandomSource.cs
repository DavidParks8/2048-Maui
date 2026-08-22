namespace TwentyFortyEight.Core;

/// <summary>
/// Abstraction for random number generation to enable deterministic testing.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Returns a non-negative random integer that is less than the specified maximum.
    /// </summary>
    int Next(int maxExclusive);

    /// <summary>
    /// Returns a random floating-point number between 0.0 and 1.0.
    /// </summary>
    double NextDouble();
}
