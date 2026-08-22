using System.Collections.ObjectModel;

namespace GoodMovies.Core;

/// <summary>
/// The minimal data needed to retain or prune a favorite while offline.
/// </summary>
public readonly record struct FavoriteEntry
{
    public FavoriteEntry(int movieId, DateOnly usTheatricalReleaseDate)
    {
        MovieId = movieId;
        UsTheatricalReleaseDate = usTheatricalReleaseDate;
    }

    public int MovieId { get; }

    public int Id => MovieId;

    public DateOnly UsTheatricalReleaseDate { get; }

    public DateOnly ReleaseDate => UsTheatricalReleaseDate;

    public bool IsVisible(DateOnly today, ReleaseWindowPolicy? releaseWindowPolicy = null) =>
        (releaseWindowPolicy ?? ReleaseWindowPolicy.Default).IsVisible(this, today);

    public bool IsExpired(DateOnly today, ReleaseWindowPolicy? releaseWindowPolicy = null) =>
        (releaseWindowPolicy ?? ReleaseWindowPolicy.Default).IsExpired(this, today);

    public static IReadOnlyList<FavoriteEntry> FilterVisible(
        IEnumerable<FavoriteEntry> entries,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null
    ) =>
        new ReadOnlyCollection<FavoriteEntry>(
            (entries ?? Array.Empty<FavoriteEntry>())
                .Where(entry =>
                    (releaseWindowPolicy ?? ReleaseWindowPolicy.Default).IsVisible(entry, today)
                )
                .ToArray()
        );

    public static IReadOnlyList<FavoriteEntry> PruneExpired(
        IEnumerable<FavoriteEntry> entries,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null
    ) => FilterVisible(entries, today, releaseWindowPolicy);
}
