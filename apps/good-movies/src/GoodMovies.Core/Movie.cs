namespace GoodMovies.Core;

/// <summary>
/// Immutable movie data needed by the catalog and UI.
/// </summary>
public sealed record Movie
{
    public Movie(
        int id,
        string title,
        string? certification,
        IEnumerable<TheatricalRelease>? releases = null,
        IEnumerable<MovieGenre>? genres = null,
        string? overview = null,
        string? posterPath = null,
        Uri? posterUri = null,
        string? originalLanguage = null,
        IEnumerable<int>? genreIds = null
    )
    {
        Id = id;
        Title = title?.Trim() ?? string.Empty;
        Certification = MovieCertification.TryCreate(
            certification,
            out MovieCertification? normalized
        )
            ? normalized
            : null;
        IsNotYetRated = string.IsNullOrWhiteSpace(certification);
        Releases = CollectionSnapshot.Create(releases);
        Genres = CollectionSnapshot.Create(genres);
        Overview = NormalizeOptional(overview);
        PosterPath = NormalizeOptional(posterPath);
        PosterUri = posterUri;
        OriginalLanguage = NormalizeOptional(originalLanguage);
        GenreIds = CopyDistinctGenreIds(genreIds, Genres);
        IsFamilyAudience = GenreIds.Any(MovieGenre.IsFamilyAudienceGenre);
        UsTheatricalReleases = CollectionSnapshot.Create(
            Releases
                .Where(static release => release is not null && release.IsUsTheatrical)
                .OrderBy(static release => release.ReleaseDate)
                .ThenBy(static release => release.ReleaseType)
        );
    }

    public Movie(
        int id,
        string title,
        string? certification,
        DateOnly usTheatricalReleaseDate,
        IEnumerable<MovieGenre>? genres = null,
        string? overview = null,
        string? posterPath = null,
        Uri? posterUri = null,
        string? originalLanguage = null,
        IEnumerable<int>? genreIds = null
    )
        : this(
            id,
            title,
            certification,
            new[]
            {
                new TheatricalRelease(
                    usTheatricalReleaseDate,
                    "US",
                    TheatricalRelease.TheatricalType
                ),
            },
            genres,
            overview,
            posterPath,
            posterUri,
            originalLanguage,
            genreIds
        ) { }

    public int Id { get; }

    public string Title { get; }

    public MovieCertification? Certification { get; }

    /// <summary>
    /// True when the provider has not published a US certification yet, which is
    /// normal for releases that are still many months away. This is different
    /// from a movie that carries a certification we do not allow.
    /// </summary>
    public bool IsNotYetRated { get; }

    /// <summary>
    /// True when the provider classifies the movie as animation or family.
    /// </summary>
    public bool IsFamilyAudience { get; }

    public IReadOnlyList<TheatricalRelease> Releases { get; }

    public IReadOnlyList<TheatricalRelease> UsTheatricalReleases { get; }

    public IReadOnlyList<MovieGenre> Genres { get; }

    public IReadOnlyList<int> GenreIds { get; }

    public string? Overview { get; }

    public string? PosterPath { get; }

    public Uri? PosterUri { get; }

    public string? OriginalLanguage { get; }

    public DateOnly? UsTheatricalReleaseDate =>
        UsTheatricalReleases.Count == 0 ? null : UsTheatricalReleases[0].ReleaseDate;

    private static IReadOnlyList<int> CopyDistinctGenreIds(
        IEnumerable<int>? genreIds,
        IReadOnlyList<MovieGenre> genres
    )
    {
        HashSet<int> seen = new();
        List<int> ids = new();

        if (genreIds is not null)
        {
            foreach (int id in genreIds)
            {
                if (id > 0 && seen.Add(id))
                {
                    ids.Add(id);
                }
            }
        }

        foreach (MovieGenre genre in genres)
        {
            if (genre.Id > 0 && seen.Add(genre.Id))
            {
                ids.Add(genre.Id);
            }
        }

        return CollectionSnapshot.Create(ids);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
