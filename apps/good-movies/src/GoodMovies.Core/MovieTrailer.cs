namespace GoodMovies.Core;

/// <summary>
/// A provider video associated with a movie.
/// </summary>
public sealed record MovieTrailer
{
    public MovieTrailer(
        string key,
        string name,
        string site,
        string type,
        bool isOfficial,
        string? languageCode
    )
    {
        Key = key?.Trim() ?? string.Empty;
        Name = name?.Trim() ?? string.Empty;
        Site = site?.Trim() ?? string.Empty;
        Type = type?.Trim() ?? string.Empty;
        IsOfficial = isOfficial;
        LanguageCode = languageCode?.Trim();
    }

    public MovieTrailer(string key, string site, string type, bool isOfficial, string? languageCode)
        : this(key, string.Empty, site, type, isOfficial, languageCode) { }

    public MovieTrailer(string site, string type, bool isOfficial, string? languageCode)
        : this(string.Empty, string.Empty, site, type, isOfficial, languageCode) { }

    public MovieTrailer(
        string key,
        string name,
        string site,
        string type,
        string? languageCode,
        bool isOfficial
    )
        : this(key, name, site, type, isOfficial, languageCode) { }

    public string Key { get; }

    public string Name { get; }

    public string Site { get; }

    public string Type { get; }

    public bool IsOfficial { get; }

    public bool Official => IsOfficial;

    public string? LanguageCode { get; }

    public string? Iso6391 => LanguageCode;

    public string? Language => LanguageCode;

    public bool IsYouTube => TrailerSelectionPolicy.IsYouTubeSite(Site);

    public bool IsEnglish => TrailerSelectionPolicy.IsEnglishLanguage(LanguageCode);
}
