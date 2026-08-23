namespace GoodMovies.Core;

public enum FavoritesResultStatus
{
    Succeeded,
    Corrupted,
    Failed,
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
        Entries = CollectionSnapshot.Create(entries);
        Error = error;
    }

    public FavoritesResultStatus Status { get; }

    public IReadOnlyList<FavoriteEntry> Entries { get; }

    public Exception? Error { get; }

    public bool Succeeded => Status == FavoritesResultStatus.Succeeded;

    public static FavoritesResult Success(IEnumerable<FavoriteEntry> entries) =>
        new(FavoritesResultStatus.Succeeded, entries);

    public static FavoritesResult Failure(FavoritesResultStatus status, Exception error) =>
        new(status, error: error);
}

public enum FavoriteToggleStatus
{
    Added,
    Removed,
    Rejected,
    Failed,
}

public sealed class FavoriteToggleResult
{
    public FavoriteToggleResult(FavoriteToggleStatus status, Exception? error = null)
    {
        Status = status;
        Error = error;
    }

    public FavoriteToggleStatus Status { get; }

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

    Task<FavoritesResult> ReconcileAsync(
        IEnumerable<Movie> refreshedMovies,
        DateOnly today,
        CancellationToken cancellationToken = default
    );
}
