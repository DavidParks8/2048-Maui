using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// Display state for one catalog card. Favorite persistence remains owned by
/// <see cref="CatalogViewModel"/>.
/// </summary>
public sealed partial class MovieCardViewModel : ObservableObject
{
    private readonly IClock _clock;
    private readonly Func<
        MovieCardViewModel,
        CancellationToken,
        Task<FavoriteToggleResult>
    >? _favoriteToggle;

    public MovieCardViewModel(
        Movie movie,
        IClock? clock = null,
        bool isFavorite = false,
        Func<MovieCardViewModel, CancellationToken, Task<FavoriteToggleResult>>? favoriteToggle =
            null
    )
    {
        Movie = movie ?? throw new ArgumentNullException(nameof(movie));
        MovieGenre? primaryGenre = Movie.Genres.Count == 0 ? null : Movie.Genres[0];
        Kind = primaryGenre?.Name ?? string.Empty;
        KindIcon = MovieKindIconMapper.GetIcon(primaryGenre);
        _clock = clock ?? new SystemClock();
        _favoriteToggle = favoriteToggle;
        IsFavorite = isFavorite;

        ReleaseDate = GetDisplayReleaseDate();
        StatusInfo = ReleaseDate is DateOnly releaseDate
            ? ReleaseWindowPolicy.GetStatusInfo(releaseDate, _clock.Today)
            : default;
    }

    public Movie Movie { get; }

    public int MovieId => Movie.Id;

    public string Title => Movie.Title;

    public string Rating => Movie.Certification?.Code ?? string.Empty;

    public string Kind { get; }

    public string KindIcon { get; }

    public string? PosterSource => Movie.PosterUri?.AbsoluteUri ?? Movie.PosterPath;

    public DateOnly? ReleaseDate { get; }

    internal FavoriteEntry? FavoriteEntry =>
        ReleaseDate is DateOnly date ? new FavoriteEntry(Movie.Id, date) : null;

    private ReleaseStatusInfo StatusInfo { get; }

    public ReleaseStatus Status => StatusInfo.Status;

    public int Sleeps => StatusInfo.Sleeps;

    [ObservableProperty]
    private bool _isFavorite;

    internal void SetFavorite(bool isFavorite) => IsFavorite = isFavorite;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task ToggleFavoriteAsync(CancellationToken cancellationToken) =>
        _favoriteToggle?.Invoke(this, cancellationToken) ?? Task.CompletedTask;

    private DateOnly? GetDisplayReleaseDate() =>
        ReleaseWindowPolicy.GetVisibleRelease(Movie, _clock.Today)?.ReleaseDate
        ?? Movie.UsTheatricalReleaseDate;
}
