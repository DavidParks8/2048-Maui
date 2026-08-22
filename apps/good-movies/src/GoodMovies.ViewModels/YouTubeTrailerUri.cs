using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// Builds the native iOS YouTube app link used for trailer playback.
/// </summary>
public static class YouTubeTrailerUri
{
    public const string Scheme = "youtube";

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
            $"{Scheme}://www.youtube.com/watch?v={Uri.EscapeDataString(key!)}",
            UriKind.Absolute
        );
        return true;
    }

    public static Uri Create(string key) =>
        TryCreate(key, out Uri uri)
            ? uri
            : throw new ArgumentException("The YouTube video key is invalid.", nameof(key));

    public static Uri? Build(string? key) => TryCreate(key, out Uri uri) ? uri : null;
}

public static class YouTubeKeyValidator
{
    public static bool IsValid(string? key) => YouTubeTrailerUri.IsValidKey(key);

    public static bool IsValidKey(string? key) => YouTubeTrailerUri.IsValidKey(key);
}
