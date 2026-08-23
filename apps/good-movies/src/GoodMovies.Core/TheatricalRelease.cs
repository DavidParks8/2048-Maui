namespace GoodMovies.Core;

/// <summary>
/// A release event reported by the movie provider.
/// </summary>
public sealed record TheatricalRelease
{
    public const int LimitedTheatricalType = 2;
    public const int TheatricalType = 3;

    public TheatricalRelease(DateOnly releaseDate, string countryCode, int releaseType)
    {
        ReleaseDate = releaseDate;
        CountryCode = NormalizeCountryCode(countryCode);
        ReleaseType = releaseType;
    }

    public DateOnly ReleaseDate { get; }

    public string CountryCode { get; }

    public int ReleaseType { get; }

    public bool IsUsTheatrical =>
        CountryCode == "US" && ReleaseType is LimitedTheatricalType or TheatricalType;

    public static bool IsAllowedTheatricalType(int releaseType) =>
        releaseType is LimitedTheatricalType or TheatricalType;

    private static string NormalizeCountryCode(string? countryCode) =>
        countryCode?.Trim().ToUpperInvariant() ?? string.Empty;
}
