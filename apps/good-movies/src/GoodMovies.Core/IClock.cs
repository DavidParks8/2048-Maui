namespace GoodMovies.Core;

/// <summary>
/// Supplies the local calendar date used by catalog policies.
/// </summary>
public interface IClock
{
    DateOnly Today { get; }
}
