namespace GoodMovies.Core;

public enum FavoritesResultStatus
{
    Succeeded,
    NoFavorites,
    Corrupted,
    Failed,
    Empty = NoFavorites,
}

public sealed class FavoritesResult
{
    public FavoritesResult(
        FavoritesResultStatus status,
        IEnumerable<FavoriteEntry>? entries = null,
        Exception? error = null
    )
    {
        Status = status;
        Entries = Array.AsReadOnly((entries ?? Array.Empty<FavoriteEntry>()).ToArray());
        Error = error;
    }

    public FavoritesResultStatus Status { get; }

    public FavoritesResultStatus State => Status;

    public IReadOnlyList<FavoriteEntry> Entries { get; }

    public IReadOnlyList<FavoriteEntry> Favorites => Entries;

    public Exception? Error { get; }

    public bool Succeeded =>
        Status is FavoritesResultStatus.Succeeded or FavoritesResultStatus.NoFavorites;

    public static FavoritesResult Success(IEnumerable<FavoriteEntry> entries)
    {
        FavoriteEntry[] values = (entries ?? Array.Empty<FavoriteEntry>()).ToArray();
        return new(
            values.Length == 0
                ? FavoritesResultStatus.NoFavorites
                : FavoritesResultStatus.Succeeded,
            values
        );
    }

    public static FavoritesResult Failure(FavoritesResultStatus status, Exception error) =>
        new(status, error: error);
}

public enum FavoriteToggleStatus
{
    Added,
    Removed,
    Rejected,
    Failed,
    NotAllowed = Rejected,
}

public sealed class FavoriteToggleResult
{
    public FavoriteToggleResult(
        FavoriteToggleStatus status,
        FavoriteEntry favorite,
        IEnumerable<FavoriteEntry>? entries = null,
        Exception? error = null
    )
    {
        Status = status;
        Favorite = favorite;
        Entries = Array.AsReadOnly((entries ?? Array.Empty<FavoriteEntry>()).ToArray());
        Error = error;
    }

    public FavoriteToggleStatus Status { get; }

    public FavoriteToggleStatus State => Status;

    public FavoriteEntry Favorite { get; }

    public IReadOnlyList<FavoriteEntry> Entries { get; }

    public bool IsFavorite => Status == FavoriteToggleStatus.Added;

    public Exception? Error { get; }
}

public interface IFavoritesStore
{
    Task<FavoritesResult> GetAsync(DateOnly today, CancellationToken cancellationToken = default);

    Task<FavoriteToggleResult> ToggleAsync(
        FavoriteEntry favorite,
        DateOnly today,
        CancellationToken cancellationToken = default
    );

    Task<FavoritesResult> PruneAsync(DateOnly today, CancellationToken cancellationToken = default);

    Task<FavoritesResult> ReconcileAsync(
        IEnumerable<Movie> refreshedMovies,
        DateOnly today,
        CancellationToken cancellationToken = default
    );
}

public interface IFavoritesRepository : IFavoritesStore { }

/// <summary>
/// Alias for ViewModels that prefer a service-oriented name.
/// </summary>
public interface IFavoritesService : IFavoritesStore { }
