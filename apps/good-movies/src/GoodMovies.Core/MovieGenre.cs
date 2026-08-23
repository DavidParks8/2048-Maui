namespace GoodMovies.Core;

/// <summary>
/// A movie genre from the provider.
/// </summary>
public sealed record MovieGenre
{
    public MovieGenre(int id, string name)
    {
        Id = id;
        Name = name?.Trim() ?? string.Empty;
    }

    public int Id { get; }

    public string Name { get; }

    /// <summary>TMDB genre id for animation.</summary>
    public const int AnimationId = 16;

    /// <summary>TMDB genre id for family.</summary>
    public const int FamilyId = 10751;

    public static bool IsFamilyAudienceGenre(int genreId) => genreId is AnimationId or FamilyId;
}
