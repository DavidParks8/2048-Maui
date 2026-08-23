namespace GoodMovies.Core;

/// <summary>
/// A provider video associated with a movie.
/// </summary>
public sealed record MovieTrailer
{
    public MovieTrailer(string key, string site, string type, bool isOfficial, string? languageCode)
    {
        Key = key?.Trim() ?? string.Empty;
        Site = site?.Trim() ?? string.Empty;
        Type = type?.Trim() ?? string.Empty;
        IsOfficial = isOfficial;
        LanguageCode = languageCode?.Trim();
    }

    public string Key { get; }

    public string Site { get; }

    public string Type { get; }

    public bool IsOfficial { get; }

    public string? LanguageCode { get; }

    public bool IsYouTube => TrailerSelectionPolicy.IsYouTubeSite(Site);
}
