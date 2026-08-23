using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoodMovies.Infrastructure;

internal sealed class CatalogCacheDocument
{
    public DateTimeOffset RefreshedAt { get; set; }

    public List<CachedMovie> Movies { get; set; } = new();
}

internal sealed class CachedMovie
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Overview { get; set; }

    public string? PosterPath { get; set; }

    public string? OriginalLanguage { get; set; }

    public string? Certification { get; set; }

    [JsonPropertyName("usTheatricalReleaseDate")]
    public DateOnly? LegacyUsTheatricalReleaseDate { get; set; }

    public List<CachedRelease> Releases { get; set; } = new();

    public List<CachedGenre> Genres { get; set; } = new();

    public List<int> GenreIds { get; set; } = new();
}

internal sealed class CachedRelease
{
    public DateOnly ReleaseDate { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public int ReleaseType { get; set; }
}

internal sealed class CachedGenre
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

internal sealed class FavoriteFileEntry
{
    public int MovieId { get; set; }

    public DateOnly UsTheatricalReleaseDate { get; set; }

    [JsonPropertyName("releaseDate")]
    public DateOnly? ReleaseDate { get; set; }
}

internal sealed class TmdbDiscoverResponse
{
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    public List<TmdbDiscoverMovie> Results { get; set; } = new();
}

internal sealed class TmdbDiscoverMovie
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

    public double Popularity { get; set; }
}

internal sealed class TmdbGenreListResponse
{
    public List<TmdbGenre> Genres { get; set; } = new();
}

internal sealed class TmdbGenre
{
    public int Id { get; set; }

    public string? Name { get; set; }
}

internal sealed class TmdbReleaseDatesResponse
{
    public List<TmdbReleaseCountry> Results { get; set; } = new();
}

internal sealed class TmdbReleaseCountry
{
    [JsonPropertyName("iso_3166_1")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("release_dates")]
    public List<TmdbReleaseDate> ReleaseDates { get; set; } = new();
}

internal sealed class TmdbReleaseDate
{
    public string? Certification { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    public int Type { get; set; }
}

internal sealed class TmdbVideosResponse
{
    public List<TmdbVideo> Results { get; set; } = new();
}

internal sealed class TmdbVideo
{
    public string? Key { get; set; }

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
[JsonSerializable(typeof(List<FavoriteFileEntry>))]
[JsonSerializable(typeof(TmdbDiscoverResponse))]
[JsonSerializable(typeof(TmdbGenreListResponse))]
[JsonSerializable(typeof(TmdbReleaseDatesResponse))]
[JsonSerializable(typeof(TmdbVideosResponse))]
internal partial class GoodMoviesJsonContext : JsonSerializerContext { }
