using System.Diagnostics.CodeAnalysis;
using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// Builds and validates the privacy-enhanced YouTube embed URL used by the in-app player.
/// </summary>
public static class YouTubeTrailerUri
{
    private const string Scheme = "https";
    public const string Host = "www.youtube-nocookie.com";
    private const string EmbedPrefix = "/embed/";

    public static bool TryCreate(string? key, [NotNullWhen(true)] out Uri? uri)
    {
        if (!YouTubeVideoKey.IsValid(key))
        {
            uri = null;
            return false;
        }

        uri = new Uri(
            $"{Scheme}://{Host}{EmbedPrefix}{Uri.EscapeDataString(key!)}"
                + "?autoplay=1&controls=1&playsinline=0&rel=0",
            UriKind.Absolute
        );
        return true;
    }

    public static bool TryGetTrustedVideoKey(Uri? uri, out string key)
    {
        if (
            uri is null
            || !uri.IsAbsoluteUri
            || !uri.IsDefaultPort
            || uri.UserInfo.Length > 0
            || !string.Equals(uri.Scheme, Scheme, StringComparison.Ordinal)
            || !string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase)
            || !uri.AbsolutePath.StartsWith(EmbedPrefix, StringComparison.Ordinal)
        )
        {
            key = string.Empty;
            return false;
        }

        key = uri.AbsolutePath[EmbedPrefix.Length..];
        if (YouTubeVideoKey.IsValid(key))
        {
            return true;
        }

        key = string.Empty;
        return false;
    }
}
