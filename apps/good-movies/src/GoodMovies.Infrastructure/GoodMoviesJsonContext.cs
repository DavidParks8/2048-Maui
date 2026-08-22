using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoodMovies.Infrastructure;

public sealed class CatalogCacheDocument
{
    public DateTimeOffset RefreshedAt { get; set; }

    public List<CachedMovie> Movies { get; set; } = new();
}

public sealed class CachedMovie
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Overview { get; set; }

    public string? PosterPath { get; set; }

    public string? PosterUri { get; set; }

    public string? OriginalLanguage { get; set; }

    public string? Certification { get; set; }

    public List<CachedRelease> Releases { get; set; } = new();

    public List<CachedGenre> Genres { get; set; } = new();

    public List<int> GenreIds { get; set; } = new();

    public List<CachedTrailer> Trailers { get; set; } = new();
}

public sealed class CachedRelease
{
    public DateOnly ReleaseDate { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public int ReleaseType { get; set; }
}

public sealed class CachedGenre
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class CachedTrailer
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Site { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsOfficial { get; set; }

    public string? LanguageCode { get; set; }
}

public sealed class FavoriteFileEntry
{
    public int MovieId { get; set; }

    public DateOnly UsTheatricalReleaseDate { get; set; }

    [JsonPropertyName("releaseDate")]
    public DateOnly? ReleaseDate { get; set; }
}

public sealed class TmdbDiscoverResponse
{
    public int Page { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }

    public List<TmdbDiscoverMovie> Results { get; set; } = new();
}

public sealed class TmdbDiscoverMovie
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Overview { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("original_language")]
    public string? OriginalLanguage { get; set; }

    [JsonPropertyName("genre_ids")]
    public List<int> GenreIds { get; set; } = new();

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    public double Popularity { get; set; }
}

public sealed class TmdbGenreListResponse
{
    public List<TmdbGenre> Genres { get; set; } = new();
}

public sealed class TmdbGenre
{
    public int Id { get; set; }

    public string? Name { get; set; }
}

public sealed class TmdbReleaseDatesResponse
{
    public int Id { get; set; }

    public List<TmdbReleaseCountry> Results { get; set; } = new();
}

public sealed class TmdbReleaseCountry
{
    [JsonPropertyName("iso_3166_1")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("release_dates")]
    public List<TmdbReleaseDate> ReleaseDates { get; set; } = new();
}

public sealed class TmdbReleaseDate
{
    public string? Certification { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    public int Type { get; set; }
}

public sealed class TmdbVideosResponse
{
    public List<TmdbVideo> Results { get; set; } = new();
}

public sealed class TmdbVideo
{
    public string? Key { get; set; }

    public string? Name { get; set; }

    public string? Site { get; set; }

    public string? Type { get; set; }

    public bool Official { get; set; }

    [JsonPropertyName("iso_639_1")]
    public string? LanguageCode { get; set; }
}

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(CatalogCacheDocument))]
[JsonSerializable(typeof(List<CachedMovie>))]
[JsonSerializable(typeof(List<CachedRelease>))]
[JsonSerializable(typeof(List<CachedGenre>))]
[JsonSerializable(typeof(List<CachedTrailer>))]
[JsonSerializable(typeof(List<FavoriteFileEntry>))]
[JsonSerializable(typeof(List<TmdbDiscoverMovie>))]
[JsonSerializable(typeof(TmdbDiscoverResponse))]
[JsonSerializable(typeof(TmdbGenreListResponse))]
[JsonSerializable(typeof(TmdbReleaseDatesResponse))]
[JsonSerializable(typeof(TmdbVideosResponse))]
public partial class GoodMoviesJsonContext : JsonSerializerContext { }
