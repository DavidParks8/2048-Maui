namespace TwentyFortyEight.Core;

/// <summary>
/// Deterministic implementation of IRandomSource for testing.
/// </summary>
public class SeededRandomSource : IRandomSource
{
    private readonly Random _random;

    public SeededRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    public int Next(int maxExclusive)
    {
        return _random.Next(maxExclusive);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }
}
