namespace GoodMovies.Core;

/// <summary>
/// A saved movie, verified US release date, and last known theater history.
/// Null history identifies legacy entries awaiting catalog reconciliation.
/// </summary>
public readonly record struct FavoriteEntry(
    int MovieId,
    DateOnly UsTheatricalReleaseDate,
    bool? HasBeenInTheaters = null,
    bool IsInTheaters = false
);
