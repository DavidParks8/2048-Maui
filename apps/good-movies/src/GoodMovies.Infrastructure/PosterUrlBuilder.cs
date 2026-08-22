namespace GoodMovies.Infrastructure;

public interface IPosterUrlBuilder
{
    Uri? Build(string? posterPath);
}

/// <summary>
/// Centralizes TMDB poster URL construction so cached and freshly fetched
/// movies use the same image size and null behavior.
/// </summary>
public sealed class PosterUrlBuilder : IPosterUrlBuilder
{
    private readonly Uri _imageBaseAddress;

    public PosterUrlBuilder(GoodMoviesInfrastructureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _imageBaseAddress = AddSizeSegment(options.ImageBaseAddress, options.PosterSize);
    }

    public PosterUrlBuilder(Uri imageBaseAddress)
    {
        if (
            imageBaseAddress is null
            || !imageBaseAddress.IsAbsoluteUri
            || !string.Equals(
                imageBaseAddress.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new ArgumentException(
                "An absolute HTTPS image base address is required.",
                nameof(imageBaseAddress)
            );
        }

        _imageBaseAddress = imageBaseAddress;
    }

    public Uri? Build(string? posterPath) => BuildPosterUrl(posterPath, _imageBaseAddress);

    public string? BuildString(string? posterPath) => Build(posterPath)?.ToString();

    public static Uri? BuildPosterUrl(string? posterPath, Uri? imageBaseAddress = null)
    {
        string? value = Normalize(posterPath);
        if (value is null)
        {
            return null;
        }

        if (!IsSafeRelativePosterPath(value))
        {
            return null;
        }

        Uri baseAddress =
            imageBaseAddress ?? GoodMoviesInfrastructureOptions.DefaultImageBaseAddress;
        if (
            !baseAddress.IsAbsoluteUri
            || !string.Equals(
                baseAddress.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new ArgumentException(
                "An absolute HTTPS image base address is required.",
                nameof(imageBaseAddress)
            );
        }

        string combined = $"{baseAddress.ToString().TrimEnd('/')}/{value.TrimStart('/')}";
        return Uri.TryCreate(combined, UriKind.Absolute, out Uri? result) ? result : null;
    }

    public static string? Build(string? posterPath, Uri? imageBaseAddress = null) =>
        BuildPosterUrl(posterPath, imageBaseAddress)?.ToString();

    public static string? Build(string? posterPath, string imageBaseAddress) =>
        Uri.TryCreate(imageBaseAddress, UriKind.Absolute, out Uri? baseAddress)
            ? Build(posterPath, baseAddress)
            : throw new ArgumentException(
                "An absolute image base address is required.",
                nameof(imageBaseAddress)
            );

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static bool IsSafeRelativePosterPath(string value) =>
        (
            value.StartsWith("/", StringComparison.Ordinal)
            || !Uri.TryCreate(value, UriKind.Absolute, out _)
        )
        && !value.Contains('\\')
        && !value.Contains("..", StringComparison.Ordinal)
        && !value.Contains('?')
        && !value.Contains('#');

    private static Uri AddSizeSegment(Uri baseAddress, string size)
    {
        string normalizedSize = size.Trim().Trim('/');
        if (
            string.IsNullOrEmpty(normalizedSize)
            || baseAddress
                .AbsolutePath.TrimEnd('/')
                .EndsWith($"/{normalizedSize}", StringComparison.OrdinalIgnoreCase)
        )
        {
            return baseAddress;
        }

        string value = $"{baseAddress.ToString().TrimEnd('/')}/{normalizedSize}";
        return new Uri(value, UriKind.Absolute);
    }
}
