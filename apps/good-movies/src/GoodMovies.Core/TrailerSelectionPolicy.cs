namespace GoodMovies.Core;

/// <summary>
/// Selects the best eligible English YouTube video in product-defined order.
/// </summary>
public sealed class TrailerSelectionPolicy
{
    public MovieTrailer? Select(IEnumerable<MovieTrailer>? trailers) => SelectCore(trailers);

    public static MovieTrailer? Select(
        IEnumerable<MovieTrailer>? trailers,
        bool useSelectionPolicy = true
    ) => SelectCore(trailers);

    public MovieTrailer? Choose(IEnumerable<MovieTrailer>? trailers) => SelectCore(trailers);

    public MovieTrailer? SelectTrailer(IEnumerable<MovieTrailer>? trailers) => SelectCore(trailers);

    public MovieTrailer? SelectBest(IEnumerable<MovieTrailer>? trailers) => SelectCore(trailers);

    public static MovieTrailer? SelectBest(
        IEnumerable<MovieTrailer>? trailers,
        bool useSelectionPolicy = true
    ) => SelectCore(trailers);

    public static bool IsYouTubeSite(string? site) =>
        string.Equals(site?.Trim(), "YouTube", StringComparison.OrdinalIgnoreCase);

    public static bool IsEnglishLanguage(string? languageCode) =>
        !string.IsNullOrWhiteSpace(languageCode)
        && languageCode.Trim().StartsWith("en", StringComparison.OrdinalIgnoreCase);

    private static MovieTrailer? SelectCore(IEnumerable<MovieTrailer>? trailers)
    {
        if (trailers is null)
        {
            return null;
        }

        MovieTrailer[] eligible = trailers
            .Where(static trailer =>
                trailer is not null && trailer.IsOfficial && IsEligibleYouTube(trailer)
            )
            .ToArray();

        return eligible.FirstOrDefault(IsOfficialTrailer)
            ?? eligible.FirstOrDefault(IsOfficialTeaser);
    }

    private static bool IsEligibleYouTube(MovieTrailer trailer) =>
        IsYouTubeSite(trailer.Site)
        && IsEnglishLanguage(trailer.LanguageCode)
        && YouTubeVideoKey.IsValid(trailer.Key);

    private static bool IsTrailer(MovieTrailer trailer) =>
        string.Equals(trailer.Type, "Trailer", StringComparison.OrdinalIgnoreCase);

    private static bool IsTeaser(MovieTrailer trailer) =>
        string.Equals(trailer.Type, "Teaser", StringComparison.OrdinalIgnoreCase);

    private static bool IsOfficialTrailer(MovieTrailer trailer) =>
        trailer.IsOfficial && IsTrailer(trailer);

    private static bool IsOfficialTeaser(MovieTrailer trailer) =>
        trailer.IsOfficial && IsTeaser(trailer);
}
