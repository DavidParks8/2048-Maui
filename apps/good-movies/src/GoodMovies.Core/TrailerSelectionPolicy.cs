namespace GoodMovies.Core;

/// <summary>
/// Selects the best eligible English YouTube video in product-defined order.
/// </summary>
public static class TrailerSelectionPolicy
{
    public static MovieTrailer? Select(IEnumerable<MovieTrailer>? trailers)
    {
        MovieTrailer? firstTeaser = null;
        foreach (MovieTrailer? trailer in trailers ?? Array.Empty<MovieTrailer>())
        {
            if (trailer is null || !trailer.IsOfficial || !IsEligibleYouTube(trailer))
            {
                continue;
            }

            if (IsTrailer(trailer))
            {
                return trailer;
            }

            if (firstTeaser is null && IsTeaser(trailer))
            {
                firstTeaser = trailer;
            }
        }

        return firstTeaser;
    }

    internal static bool IsYouTubeSite(string? site) =>
        string.Equals(site?.Trim(), "YouTube", StringComparison.OrdinalIgnoreCase);

    private static bool IsEnglishLanguage(string? languageCode) =>
        !string.IsNullOrWhiteSpace(languageCode)
        && languageCode.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase);

    private static bool IsEligibleYouTube(MovieTrailer trailer) =>
        IsYouTubeSite(trailer.Site)
        && IsEnglishLanguage(trailer.LanguageCode)
        && YouTubeVideoKey.IsValid(trailer.Key);

    private static bool IsTrailer(MovieTrailer trailer) =>
        string.Equals(trailer.Type, "Trailer", StringComparison.OrdinalIgnoreCase);

    private static bool IsTeaser(MovieTrailer trailer) =>
        string.Equals(trailer.Type, "Teaser", StringComparison.OrdinalIgnoreCase);
}
