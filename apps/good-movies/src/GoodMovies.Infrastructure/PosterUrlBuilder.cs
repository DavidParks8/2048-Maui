namespace GoodMovies.Infrastructure;

/// <summary>
/// Centralizes TMDB poster URL construction so cached and freshly fetched
/// movies use the same image size and null behavior.
/// </summary>
internal sealed class PosterUrlBuilder
{
    private readonly Uri _imageBaseAddress;

    public PosterUrlBuilder(GoodMoviesInfrastructureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _imageBaseAddress = AddSizeSegment(options.ImageBaseAddress, options.PosterSize);
    }

    public Uri? Build(string? posterPath)
    {
        if (string.IsNullOrWhiteSpace(posterPath))
        {
            return null;
        }

        string value = posterPath.Trim();
        if (!IsSafeRelativePosterPath(value))
        {
            return null;
        }

        string combined = $"{_imageBaseAddress.ToString().TrimEnd('/')}/{value.TrimStart('/')}";
        return Uri.TryCreate(combined, UriKind.Absolute, out Uri? result) ? result : null;
    }

    private static bool IsSafeRelativePosterPath(string value) =>
        (value.StartsWith('/') || !Uri.TryCreate(value, UriKind.Absolute, out _))
        && !value.Contains('\\', StringComparison.Ordinal)
        && !value.Contains("..", StringComparison.Ordinal)
        && !value.Contains('?', StringComparison.Ordinal)
        && !value.Contains('#', StringComparison.Ordinal);

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
