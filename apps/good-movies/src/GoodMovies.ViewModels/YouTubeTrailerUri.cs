using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// Builds the privacy-enhanced YouTube embed URL used by the in-app player.
/// </summary>
public static class YouTubeTrailerUri
{
    public const string Scheme = "https";
    public const string Host = "www.youtube-nocookie.com";

    public const int VideoKeyLength = YouTubeVideoKey.Length;

    public static bool IsValidKey(string? key) => YouTubeVideoKey.IsValid(key);

    public static bool TryCreate(string? key, out Uri uri)
    {
        if (!IsValidKey(key))
        {
            uri = null!;
            return false;
        }

        uri = new Uri(
            $"{Scheme}://{Host}/embed/{Uri.EscapeDataString(key!)}"
                + "?autoplay=1&controls=1&playsinline=0&rel=0",
            UriKind.Absolute
        );
        return true;
    }

    public static Uri Create(string key) =>
        TryCreate(key, out Uri uri)
            ? uri
            : throw new ArgumentException("The YouTube video key is invalid.", nameof(key));

    public static Uri? Build(string? key) => TryCreate(key, out Uri uri) ? uri : null;

    public static bool IsTrustedEmbedUri(Uri? uri)
    {
        if (
            uri is null
            || !uri.IsAbsoluteUri
            || !string.Equals(uri.Scheme, Scheme, StringComparison.Ordinal)
            || !string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase)
        )
        {
            return false;
        }

        return TryGetVideoKey(uri, out _);
    }

    public static bool TryGetVideoKey(Uri? uri, out string key)
    {
        const string embedPrefix = "/embed/";
        if (
            uri is null
            || !uri.AbsolutePath.StartsWith(embedPrefix, StringComparison.Ordinal)
            || !YouTubeVideoKey.IsValid(uri.AbsolutePath[embedPrefix.Length..])
        )
        {
            key = string.Empty;
            return false;
        }

        key = uri.AbsolutePath[embedPrefix.Length..];
        return true;
    }
}

public static class YouTubeKeyValidator
{
    public static bool IsValid(string? key) => YouTubeTrailerUri.IsValidKey(key);

    public static bool IsValidKey(string? key) => YouTubeTrailerUri.IsValidKey(key);
}
