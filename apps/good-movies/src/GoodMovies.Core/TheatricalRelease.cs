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

    public TheatricalRelease(string countryCode, int releaseType, DateOnly releaseDate)
        : this(releaseDate, countryCode, releaseType) { }

    public TheatricalRelease(string countryCode, DateOnly releaseDate, int releaseType)
        : this(releaseDate, countryCode, releaseType) { }

    public DateOnly ReleaseDate { get; }

    public DateOnly Date => ReleaseDate;

    public string CountryCode { get; }

    public string Country => CountryCode;

    public string Iso3166CountryCode => CountryCode;

    public int ReleaseType { get; }

    public int Type => ReleaseType;

    public bool IsUsRelease => CountryCode == "US";

    public bool IsUsTheatrical =>
        IsUsRelease && ReleaseType is LimitedTheatricalType or TheatricalType;

    public static bool IsAllowedTheatricalType(int releaseType) =>
        releaseType is LimitedTheatricalType or TheatricalType;

    private static string NormalizeCountryCode(string? countryCode) =>
        countryCode?.Trim().ToUpperInvariant() ?? string.Empty;
}
