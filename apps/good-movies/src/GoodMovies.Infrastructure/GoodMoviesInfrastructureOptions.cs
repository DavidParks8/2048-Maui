using System.Net;

namespace GoodMovies.Infrastructure;

/// <summary>
/// Configuration for the platform-neutral Good Movies services. The MAUI
/// application can bind or populate this type without making Infrastructure
/// depend on MAUI.
/// </summary>
public class GoodMoviesInfrastructureOptions
{
    public static Uri DefaultApiBaseAddress { get; } = new("https://api.themoviedb.org/");

    public static Uri DefaultImageBaseAddress { get; } = new("https://image.tmdb.org/t/p/w500");

    public Uri ApiBaseAddress { get; set; } = DefaultApiBaseAddress;

    public Uri BaseAddress
    {
        get => ApiBaseAddress;
        set => ApiBaseAddress = value;
    }

    public Uri ImageBaseAddress { get; set; } = DefaultImageBaseAddress;

    /// <summary>
    /// The TMDB bearer token. It is consumed only by the token provider and is
    /// never included in logs or exception messages.
    /// </summary>
    public string? Token { get; set; }

    public string? AccessToken
    {
        get => Token;
        set => Token = value;
    }

    public int MaxPages { get; set; } = 20;

    /// <summary>
    /// Minimum TMDB popularity a movie must have before it is accepted without a
    /// published US certification. Real theatrical family releases score well
    /// above this even a year out, while festival shorts sit far below it, so
    /// this keeps the twelve month window full without filling it with noise.
    /// Movies that already carry a G or PG certification ignore this entirely.
    /// </summary>
    public double MinimumUnratedPopularity { get; set; } = 0.75;

    public int PageLimit
    {
        get => MaxPages;
        set => MaxPages = value;
    }

    public int PageCap
    {
        get => MaxPages;
        set => MaxPages = value;
    }

    public int MaxPageCount
    {
        get => MaxPages;
        set => MaxPages = value;
    }

    public int MaxConcurrentRequests { get; set; } = 4;

    public int ConcurrencyCap
    {
        get => MaxConcurrentRequests;
        set => MaxConcurrentRequests = value;
    }

    public int MaxConcurrency
    {
        get => MaxConcurrentRequests;
        set => MaxConcurrentRequests = value;
    }

    public TimeSpan CacheLifetime { get; set; } = TimeSpan.FromHours(6);

    public TimeSpan CacheStalenessThreshold
    {
        get => CacheLifetime;
        set => CacheLifetime = value;
    }

    public string CatalogCacheFileName { get; set; } = "good-movies-catalog.json";

    public string CatalogFileName
    {
        get => CatalogCacheFileName;
        set => CatalogCacheFileName = value;
    }

    public string CacheFileName
    {
        get => CatalogCacheFileName;
        set => CatalogCacheFileName = value;
    }

    public string FavoritesFileName { get; set; } = "good-movies-favorites.json";

    public string FavoritesFilePathName
    {
        get => FavoritesFileName;
        set => FavoritesFileName = value;
    }

    public string PosterSize { get; set; } = "w500";

    /// <summary>
    /// Optional application data directory. A platform can leave this null and
    /// provide its own path provider instead.
    /// </summary>
    public string? StorageDirectory { get; set; }

    public string? DataDirectory
    {
        get => StorageDirectory;
        set => StorageDirectory = value;
    }

    public Uri ApiBaseUri
    {
        get => ApiBaseAddress;
        set => ApiBaseAddress = value;
    }

    public string ApiBaseUrl
    {
        get => ApiBaseAddress.ToString();
        set =>
            ApiBaseAddress = Uri.TryCreate(value, UriKind.Absolute, out Uri? address)
                ? address
                : throw new GoodMoviesConfigurationException(
                    "The TMDB API base address is invalid."
                );
    }

    public Uri ImageBaseUri
    {
        get => ImageBaseAddress;
        set => ImageBaseAddress = value;
    }

    public string ImageBaseUrl
    {
        get => ImageBaseAddress.ToString();
        set =>
            ImageBaseAddress = Uri.TryCreate(value, UriKind.Absolute, out Uri? address)
                ? address
                : throw new GoodMoviesConfigurationException("The image base address is invalid.");
    }

    public void Validate()
    {
        if (
            ApiBaseAddress is null
            || !ApiBaseAddress.IsAbsoluteUri
            || !string.Equals(
                ApiBaseAddress.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new GoodMoviesConfigurationException(
                "The TMDB API base address must be an absolute HTTPS URI."
            );
        }

        if (
            ImageBaseAddress is null
            || !ImageBaseAddress.IsAbsoluteUri
            || !string.Equals(
                ImageBaseAddress.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new GoodMoviesConfigurationException(
                "The image base address must be an absolute HTTPS URI."
            );
        }

        if (MaxPages < 1)
        {
            throw new GoodMoviesConfigurationException("The maximum page count must be positive.");
        }

        if (MinimumUnratedPopularity < 0)
        {
            throw new GoodMoviesConfigurationException(
                "The minimum unrated popularity cannot be negative."
            );
        }

        if (MaxConcurrentRequests < 1)
        {
            throw new GoodMoviesConfigurationException(
                "The maximum concurrent request count must be positive."
            );
        }

        if (CacheLifetime < TimeSpan.Zero)
        {
            throw new GoodMoviesConfigurationException("The cache lifetime cannot be negative.");
        }

        if (
            string.IsNullOrWhiteSpace(PosterSize)
            || PosterSize.Contains('/', StringComparison.Ordinal)
            || PosterSize.Contains('\\', StringComparison.Ordinal)
        )
        {
            throw new GoodMoviesConfigurationException("The poster size is invalid.");
        }

        ValidateFileName(CatalogCacheFileName, nameof(CatalogCacheFileName));
        ValidateFileName(FavoritesFileName, nameof(FavoritesFileName));
    }

    private static void ValidateFileName(string? fileName, string parameterName)
    {
        if (
            string.IsNullOrWhiteSpace(fileName)
            || fileName.Contains('/', StringComparison.Ordinal)
            || fileName.Contains('\\', StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal)
        )
        {
            throw new GoodMoviesConfigurationException(
                $"The {parameterName} must be a file name, not a path."
            );
        }
    }
}

public class GoodMoviesOptions : GoodMoviesInfrastructureOptions { }

public class TmdbOptions : GoodMoviesInfrastructureOptions { }

public interface IFileSystemPathProvider
{
    string GetPath(string fileName);

    string GetFilePath(string fileName) => GetPath(fileName);
}

public interface IFilePathProvider : IFileSystemPathProvider { }

public interface IGoodMoviesFilePathProvider : IFileSystemPathProvider { }

public class GoodMoviesFilePathProvider : IGoodMoviesFilePathProvider
{
    public GoodMoviesFilePathProvider(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("A root directory is required.", nameof(rootDirectory));
        }

        RootDirectory = Path.GetFullPath(rootDirectory);
    }

    public string RootDirectory { get; }

    public string GetPath(string fileName)
    {
        if (
            string.IsNullOrWhiteSpace(fileName)
            || fileName.Contains('/', StringComparison.Ordinal)
            || fileName.Contains('\\', StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal)
        )
        {
            throw new ArgumentException("Only a file name is allowed.", nameof(fileName));
        }

        return Path.Combine(RootDirectory, fileName);
    }

    public string GetFilePath(string fileName) => GetPath(fileName);
}

public class FileSystemPathProvider : GoodMoviesFilePathProvider, IFilePathProvider
{
    public FileSystemPathProvider(string rootDirectory)
        : base(rootDirectory) { }
}

public sealed class DefaultGoodMoviesFilePathProvider : GoodMoviesFilePathProvider
{
    public DefaultGoodMoviesFilePathProvider()
        : base(GetDefaultRoot()) { }

    private static string GetDefaultRoot()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(root)
            ? Path.Combine(AppContext.BaseDirectory, "GoodMovies")
            : Path.Combine(root, "GoodMovies");
    }
}

public interface IGoodMoviesTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemGoodMoviesTimeProvider : IGoodMoviesTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public interface IGoodMoviesTokenProvider
{
    ValueTask<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}

public interface ITmdbTokenProvider : IGoodMoviesTokenProvider { }

public sealed class OptionsGoodMoviesTokenProvider : ITmdbTokenProvider
{
    private readonly GoodMoviesInfrastructureOptions _options;

    public OptionsGoodMoviesTokenProvider(GoodMoviesInfrastructureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ValueTask<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_options.Token);
    }
}

public sealed class StaticGoodMoviesTokenProvider : ITmdbTokenProvider
{
    private readonly string? _token;

    public StaticGoodMoviesTokenProvider(string? token)
    {
        _token = token;
    }

    public ValueTask<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_token);
    }
}

public sealed class GoodMoviesConfigurationException : InvalidOperationException
{
    public GoodMoviesConfigurationException(string message)
        : base(message) { }

    public GoodMoviesConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class TmdbRequestException : HttpRequestException
{
    public TmdbRequestException(string message, HttpStatusCode statusCode)
        : base(message, null, statusCode) { }

    public TmdbRequestException(string message, Exception innerException)
        : base(message, innerException) { }
}

public sealed class TmdbProtocolException : Exception
{
    public TmdbProtocolException(string message)
        : base(message) { }

    public TmdbProtocolException(string message, Exception innerException)
        : base(message, innerException) { }
}

public sealed class CatalogRefreshException : InvalidOperationException
{
    public CatalogRefreshException(string message, Exception innerException)
        : base(message, innerException) { }
}
