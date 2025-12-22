namespace TwentyFortyEight.Core;

/// <summary>
/// Deterministic implementation of IRandomSource for testing.
/// </summary>
public class SeededRandomSource(int seed) : IRandomSource
{
    private readonly Random _random = new(seed);

    public int Next(int maxExclusive)
    {
        return _random.Next(maxExclusive);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }
}
