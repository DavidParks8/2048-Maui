using GoodMovies.Core;

namespace GoodMovies.ViewModels;

internal static class MovieKindIconMapper
{
    public static string GetIcon(MovieGenre? genre)
    {
        if (genre is null)
        {
            return "🎬";
        }

        return genre.Id switch
        {
            12 => "🧭",
            14 => "✨",
            16 => "🎨",
            35 => "😄",
            878 => "🚀",
            9648 => "🔎",
            10402 => "🎵",
            10751 => "🏠",
            _ => GetIconByName(genre.Name),
        };
    }

    private static string GetIconByName(string name) =>
        name.Trim().ToUpperInvariant() switch
        {
            "ADVENTURE" => "🧭",
            "ANIMATION" => "🎨",
            "COMEDY" => "😄",
            "FAMILY" => "🏠",
            "FANTASY" => "✨",
            "MUSIC" => "🎵",
            "MYSTERY" => "🔎",
            "SCIENCE FICTION" or "SCI-FI" => "🚀",
            _ => "🎬",
        };
}
