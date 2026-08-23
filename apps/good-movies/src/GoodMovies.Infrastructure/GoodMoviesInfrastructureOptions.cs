namespace GoodMovies.Infrastructure;

/// <summary>
/// Configuration for the platform-neutral Good Movies services.
/// </summary>
public sealed class GoodMoviesInfrastructureOptions
{
    public const int MinimumPageCount = 1;

    /// <summary>
    /// Bounds each of the two discovery passes to 20 pages, limiting both page
    /// requests and the candidates that can trigger release-verification calls.
    /// </summary>
    public const int MaximumPageCount = 20;

    public const int MinimumConcurrentRequestCount = 1;

    /// <summary>
    /// Caps release-verification fan-out at a modest multiple of the default.
    /// </summary>
    public const int MaximumConcurrentRequestCount = 8;

    public Uri ApiBaseAddress { get; set; } = new("https://api.themoviedb.org/");

    public Uri ImageBaseAddress { get; set; } = new("https://image.tmdb.org/t/p");

    /// <summary>
    /// The optional TMDB bearer token. It is never included in logs or exception messages.
    /// </summary>
    public string? Token { get; set; }

    public int MaxPages { get; set; } = MaximumPageCount;

    /// <summary>
    /// Minimum popularity accepted for a not-yet-rated family movie. Movies with
    /// a G or PG certification do not use this threshold.
    /// </summary>
    public double MinimumUnratedPopularity { get; set; } = 0.75;

    public int MaxConcurrentRequests { get; set; } = 4;

    public TimeSpan CacheLifetime { get; set; } = TimeSpan.FromHours(6);

    public string CatalogCacheFileName { get; set; } = "good-movies-catalog.json";

    public string FavoritesFileName { get; set; } = "good-movies-favorites.json";

    public string PosterSize { get; set; } = "w500";

    /// <summary>
    /// Optional application data directory.
    /// </summary>
    public string? StorageDirectory { get; set; }

    public void Validate()
    {
        ValidateHttpsAddress(ApiBaseAddress, "The TMDB API base address");
        ValidateHttpsAddress(ImageBaseAddress, "The image base address");

        if (MaxPages is < MinimumPageCount or > MaximumPageCount)
        {
            throw new GoodMoviesConfigurationException(
                $"The maximum page count must be between {MinimumPageCount} and {MaximumPageCount}."
            );
        }

        if (!double.IsFinite(MinimumUnratedPopularity) || MinimumUnratedPopularity < 0)
        {
            throw new GoodMoviesConfigurationException(
                "The minimum unrated popularity must be a finite, nonnegative number."
            );
        }

        if (
            MaxConcurrentRequests
            is < MinimumConcurrentRequestCount
                or > MaximumConcurrentRequestCount
        )
        {
            throw new GoodMoviesConfigurationException(
                $"The maximum concurrent request count must be between {MinimumConcurrentRequestCount} and {MaximumConcurrentRequestCount}."
            );
        }

        if (CacheLifetime < TimeSpan.Zero)
        {
            throw new GoodMoviesConfigurationException("The cache lifetime cannot be negative.");
        }

        if (
            string.IsNullOrWhiteSpace(PosterSize)
            || PosterSize.Contains("/", StringComparison.Ordinal)
            || PosterSize.Contains("\\", StringComparison.Ordinal)
        )
        {
            throw new GoodMoviesConfigurationException("The poster size is invalid.");
        }

        ValidateFileName(CatalogCacheFileName, nameof(CatalogCacheFileName));
        ValidateFileName(FavoritesFileName, nameof(FavoritesFileName));
    }

    private static void ValidateHttpsAddress(Uri? address, string description)
    {
        if (
            address is null
            || !address.IsAbsoluteUri
            || !string.Equals(
                address.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new GoodMoviesConfigurationException(
                $"{description} must be an absolute HTTPS URI."
            );
        }
    }

    private static void ValidateFileName(string? fileName, string parameterName)
    {
        if (!FileSystemPathProvider.IsFileName(fileName))
        {
            throw new GoodMoviesConfigurationException(
                $"The {parameterName} must be a file name, not a path."
            );
        }
    }
}
