namespace GoodMovies.Core;

/// <summary>
/// The minimal data needed to retain or prune a favorite while offline.
/// </summary>
public readonly record struct FavoriteEntry(int MovieId, DateOnly UsTheatricalReleaseDate);
