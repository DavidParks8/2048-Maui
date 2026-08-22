using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// Immutable display data for one movie. CatalogViewModel owns favorite
/// persistence; this type only reflects the persisted result.
/// </summary>
public partial class MovieCardViewModel : ObservableObject
{
    private readonly INavigationService? _navigationService;
    private readonly Func<
        MovieCardViewModel,
        CancellationToken,
        Task<FavoriteToggleResult>
    >? _favoriteToggle;
    private readonly DateOnly? _releaseDate;

    public MovieCardViewModel(
        Movie movie,
        IClock? clock = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        INavigationService? navigationService = null,
        Func<MovieCardViewModel, CancellationToken, Task<FavoriteToggleResult>>? favoriteToggle =
            null
    )
    {
        Movie = movie ?? throw new ArgumentNullException(nameof(movie));
        Clock = clock ?? new SystemClock();
        ReleaseWindowPolicy = releaseWindowPolicy ?? GoodMovies.Core.ReleaseWindowPolicy.Default;
        _navigationService = navigationService;
        _favoriteToggle = favoriteToggle;
        _releaseDate = GetDisplayReleaseDate();
        StatusInfo = _releaseDate is DateOnly releaseDate
            ? ReleaseWindowPolicy.GetStatusInfo(releaseDate, Clock.Today)
            : default;
    }

    public MovieCardViewModel(Movie movie, INavigationService navigationService)
        : this(movie, null, null, navigationService) { }

    public MovieCardViewModel(Movie movie, IClock clock, INavigationService? navigationService)
        : this(movie, clock, null, navigationService) { }

    public MovieCardViewModel(
        Movie movie,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        INavigationService? navigationService = null
    )
        : this(movie, new FixedClock(today), releaseWindowPolicy, navigationService) { }

    public MovieCardViewModel(
        Movie movie,
        bool isFavorite,
        IClock? clock = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        INavigationService? navigationService = null,
        Func<MovieCardViewModel, CancellationToken, Task<FavoriteToggleResult>>? favoriteToggle =
            null
    )
        : this(movie, clock, releaseWindowPolicy, navigationService, favoriteToggle)
    {
        IsFavorite = isFavorite;
    }

    public MovieCardViewModel(
        Movie movie,
        IClock clock,
        bool isFavorite,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        INavigationService? navigationService = null
    )
        : this(movie, isFavorite, clock, releaseWindowPolicy, navigationService) { }

    public MovieCardViewModel(
        Movie movie,
        IClock clock,
        ReleaseWindowPolicy releaseWindowPolicy,
        bool isFavorite,
        INavigationService? navigationService = null
    )
        : this(movie, isFavorite, clock, releaseWindowPolicy, navigationService) { }

    public Movie Movie { get; }

    public Movie Item => Movie;

    public IClock Clock { get; }

    public ReleaseWindowPolicy ReleaseWindowPolicy { get; }

    public int Id => Movie.Id;

    public int MovieId => Movie.Id;

    public string Title => Movie.Title;

    public string Name => Movie.Title;

    public string Rating => Movie.CertificationCode ?? string.Empty;

    public string Certification => Rating;

    /// <summary>
    /// True when the MPAA has not rated this release yet. The movie is still
    /// safe to show because only animation and family titles reach the catalog
    /// without a rating.
    /// </summary>
    public bool IsNotYetRated => Movie.IsNotYetRated;

    public MovieCertification? MovieCertification => Movie.Certification;

    /// <summary>
    /// Design E uses one kind chip. Core provides ordered genre data, so the
    /// first provider genre is used without inventing policy in the ViewModel.
    /// </summary>
    public string Kind => Movie.Genres.FirstOrDefault()?.Name ?? string.Empty;

    public string Genre => Kind;

    public MovieGenre? PrimaryGenre => Movie.Genres.FirstOrDefault();

    public string KindIcon => MovieKindIconMapper.GetIcon(PrimaryGenre);

    public IReadOnlyList<MovieGenre> Genres => Movie.Genres;

    public IReadOnlyList<string> GenreNames => Movie.GenreNames;

    public string? Overview => Movie.Overview;

    public string? Poster => Movie.PosterUri ?? Movie.PosterPath;

    public string? PosterSource => Poster;

    public string? PosterUrl => Poster;

    public string? PosterUri => Movie.PosterUri;

    public string? PosterPath => Movie.PosterPath;

    public Uri? PosterUriValue => Movie.PosterUriValue;

    public DateOnly? ReleaseDate => _releaseDate;

    public DateOnly? DisplayReleaseDate => ReleaseDate;

    public DateOnly? UsTheatricalReleaseDate => ReleaseDate;

    public FavoriteEntry? FavoriteEntry =>
        ReleaseDate is DateOnly date ? new FavoriteEntry(Movie.Id, date) : null;

    public ReleaseStatusInfo StatusInfo { get; }

    public ReleaseStatusInfo ReleaseStatusInfo => StatusInfo;

    public ReleaseStatusInfo StatusData => StatusInfo;

    public ReleaseStatusInfo ReleaseStatusData => StatusInfo;

    public ReleaseStatus Status => StatusInfo.Status;

    public ReleaseStatus ReleaseStatus => Status;

    public ReleaseStatus StatusKind => Status;

    public ReleaseStatusKey StatusKey =>
        Status switch
        {
            ReleaseStatus.Future => ReleaseStatusKey.FutureSleeps,
            ReleaseStatus.Today => ReleaseStatusKey.Today,
            ReleaseStatus.InTheatersNow => ReleaseStatusKey.InTheatersNow,
            _ => ReleaseStatusKey.None,
        };

    public int Sleeps => StatusInfo.Sleeps;

    public int DaysUntilRelease => StatusInfo.DaysUntilRelease;

    public bool IsFuture => StatusInfo.IsFuture;

    public bool IsToday => StatusInfo.IsToday;

    public bool IsInTheatersNow => StatusInfo.IsInTheatersNow;

    public bool IsVisible =>
        ReleaseDate is DateOnly date && ReleaseWindowPolicy.IsVisible(date, Clock.Today);

    [ObservableProperty]
    private bool _isFavorite;

    public bool Favorite => IsFavorite;

    public void SetFavorite(bool isFavorite)
    {
        IsFavorite = isFavorite;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteToggleFavoriteAsync(CancellationToken cancellationToken)
    {
        if (_favoriteToggle is not null)
        {
            await _favoriteToggle(this, cancellationToken);
        }
    }

    public IAsyncRelayCommand ToggleFavoriteCommand => ExecuteToggleFavoriteCommand;

    private DateOnly? GetDisplayReleaseDate()
    {
        DateOnly? visibleDate = Movie
            .UsTheatricalReleases.Where(release =>
                ReleaseWindowPolicy.IsVisible(release.ReleaseDate, Clock.Today)
            )
            .Select(static release => (DateOnly?)release.ReleaseDate)
            .OrderBy(static date => date)
            .FirstOrDefault();
        return visibleDate ?? Movie.UsTheatricalReleaseDate;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task ExecuteOpenDetailAsync()
    {
        if (_navigationService is null)
        {
            return;
        }

        Task navigation = _navigationService.NavigateToMovieDetailAsync(Movie.Id);
        if (navigation is not null)
        {
            await navigation;
        }
    }

    public IAsyncRelayCommand OpenDetailCommand => ExecuteOpenDetailCommand;

    public IAsyncRelayCommand NavigateCommand => OpenDetailCommand;

    public IAsyncRelayCommand OpenCommand => OpenDetailCommand;

    partial void OnIsFavoriteChanged(bool value)
    {
        OnPropertyChanged(nameof(Favorite));
    }

    private sealed class FixedClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; } = today;
    }
}
