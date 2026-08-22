using System.Collections.ObjectModel;

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
        IEnumerable<MovieTrailer>? trailers = null,
        string? overview = null,
        string? posterPath = null,
        string? posterUri = null,
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
        Releases = Copy(releases);
        Genres = Copy(genres);
        GenreNames = Copy(Genres.Select(static genre => genre.Name));
        Trailers = Copy(trailers);
        Overview = NormalizeOptional(overview);
        PosterPath = NormalizeOptional(posterPath);
        PosterUri = NormalizeOptional(posterUri);
        OriginalLanguage = NormalizeOptional(originalLanguage);
        GenreIds = CopyDistinctGenreIds(genreIds, Genres);
        UsTheatricalReleases = Copy(
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
        IEnumerable<MovieTrailer>? trailers = null,
        string? overview = null,
        string? posterPath = null,
        string? posterUri = null,
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
            trailers,
            overview,
            posterPath,
            posterUri,
            originalLanguage,
            genreIds
        ) { }

    public Movie(
        int id,
        string title,
        string? certification,
        TheatricalRelease release,
        IEnumerable<MovieGenre>? genres = null,
        IEnumerable<MovieTrailer>? trailers = null,
        string? overview = null,
        string? posterPath = null,
        string? posterUri = null,
        string? originalLanguage = null,
        IEnumerable<int>? genreIds = null
    )
        : this(
            id,
            title,
            certification,
            new[] { release },
            genres,
            trailers,
            overview,
            posterPath,
            posterUri,
            originalLanguage,
            genreIds
        ) { }

    public int Id { get; }

    public int MovieId => Id;

    public string Title { get; }

    public string Name => Title;

    public MovieCertification? Certification { get; }

    public string? CertificationCode => Certification?.Code;

    public IReadOnlyList<TheatricalRelease> Releases { get; }

    public IReadOnlyList<TheatricalRelease> TheatricalReleases => Releases;

    public IReadOnlyList<TheatricalRelease> UsTheatricalReleases { get; }

    public IReadOnlyList<MovieGenre> Genres { get; }

    public IReadOnlyList<int> GenreIds { get; }

    public IReadOnlyList<string> GenreNames { get; }

    public IReadOnlyList<MovieTrailer> Trailers { get; }

    public IReadOnlyList<MovieTrailer> Videos => Trailers;

    public string? Overview { get; }

    public string? Synopsis => Overview;

    public string? SimpleSynopsis => Overview;

    public string? SynopsisSource => Overview;

    public string? SimpleSynopsisSource => Overview;

    public string? OverviewText => Overview;

    public string? PosterPath { get; }

    public string? PosterUri { get; }

    public string? PosterUrl => PosterUri;

    public Uri? PosterUriValue =>
        Uri.TryCreate(PosterUri, UriKind.Absolute, out Uri? uri) ? uri : null;

    public Uri? PosterUrlUri => PosterUriValue;

    public string? PosterPathOrUri => PosterUri ?? PosterPath;

    public string? OriginalLanguage { get; }

    public string? OriginalLanguageCode => OriginalLanguage;

    public TheatricalRelease? UsTheatricalRelease =>
        UsTheatricalReleases.Count == 0 ? null : UsTheatricalReleases[0];

    public TheatricalRelease? Release => UsTheatricalRelease;

    public DateOnly? UsTheatricalReleaseDate => UsTheatricalRelease?.ReleaseDate;

    public DateOnly? PrimaryReleaseDate => UsTheatricalReleaseDate;

    public DateOnly? ReleaseDate => UsTheatricalReleaseDate;

    public bool HasUsTheatricalRelease => UsTheatricalReleases.Count > 0;

    public FavoriteEntry? CreateFavoriteEntry() =>
        UsTheatricalReleaseDate is DateOnly date ? new FavoriteEntry(Id, date) : null;

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T>? source)
    {
        T[] items = source?.ToArray() ?? Array.Empty<T>();
        return new ReadOnlyCollection<T>(items);
    }

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

        return new ReadOnlyCollection<int>(ids.ToArray());
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
