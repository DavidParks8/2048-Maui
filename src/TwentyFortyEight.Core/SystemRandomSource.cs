namespace TwentyFortyEight.Core;

/// <summary>
/// Production implementation of IRandomSource using System.Random.
/// </summary>
public class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    public SystemRandomSource()
    {
        _random = new Random();
    }

    public SystemRandomSource(int seed)
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
