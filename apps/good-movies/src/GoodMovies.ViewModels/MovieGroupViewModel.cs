using System.Collections;
using System.Collections.ObjectModel;
using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// A semantic group of cards. The UI localizes the heading and formats the
/// exact date rather than receiving display prose from this type.
/// </summary>
public sealed class MovieGroupViewModel : IEnumerable<MovieCardViewModel>
{
    public MovieGroupViewModel(
        IEnumerable<MovieCardViewModel> cards,
        MovieGroupKind kind,
        DateOnly? releaseDate = null
    )
        : this(kind, releaseDate, cards) { }

    public MovieGroupViewModel(
        MovieGroupKind kind,
        DateOnly? releaseDate,
        IEnumerable<MovieCardViewModel> cards
    )
    {
        if (kind == MovieGroupKind.InTheatersNow)
        {
            releaseDate = null;
        }

        GroupKind = kind;
        ReleaseDate = releaseDate;
        Cards = new ReadOnlyCollection<MovieCardViewModel>(
            (cards ?? Array.Empty<MovieCardViewModel>())
                .Where(static card => card is not null)
                .OrderBy(static card => card.ReleaseDate ?? DateOnly.MaxValue)
                .ThenBy(static card => card.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static card => card.Title, StringComparer.Ordinal)
                .ThenBy(static card => card.MovieId)
                .ToArray()
        );
    }

    public MovieGroupKind GroupKind { get; }

    public MovieGroupKind Kind => GroupKind;

    public MovieGroupKind Group => GroupKind;

    public string GroupKey => HeaderKey.ToString();

    public MovieGroupHeaderKey HeaderKey =>
        GroupKind == MovieGroupKind.InTheatersNow
            ? MovieGroupHeaderKey.InTheatersNow
            : MovieGroupHeaderKey.ReleaseDate;

    public MovieGroupHeaderKey HeaderKind => HeaderKey;

    public MovieGroupHeaderKey Header => HeaderKey;

    public DateOnly? ReleaseDate { get; }

    public DateOnly? Date => ReleaseDate;

    public DateOnly? GroupDate => ReleaseDate;

    public DateOnly? DateKey => ReleaseDate;

    public IReadOnlyList<MovieCardViewModel> Cards { get; }

    public IReadOnlyList<MovieCardViewModel> MovieCards => Cards;

    public IReadOnlyList<MovieCardViewModel> Items => Cards;

    public IReadOnlyList<MovieCardViewModel> Movies => Cards;

    public int Count => Cards.Count;

    public bool IsInTheatersNow => GroupKind == MovieGroupKind.InTheatersNow;

    public bool IsInTheaters => IsInTheatersNow;

    public bool IsFutureDate => GroupKind == MovieGroupKind.ReleaseDate;

    /// <summary>
    /// CollectionView grouping requires each group to expose an enumerable
    /// sequence. Cards remains the read-only presentation surface used by
    /// callers and tests.
    /// </summary>
    public IEnumerator<MovieCardViewModel> GetEnumerator() => Cards.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static bool TryFindMovie(
        IReadOnlyList<MovieGroupViewModel>? groups,
        int movieId,
        out int groupIndex,
        out int itemIndex
    )
    {
        if (groups is not null)
        {
            for (int currentGroupIndex = 0; currentGroupIndex < groups.Count; currentGroupIndex++)
            {
                IReadOnlyList<MovieCardViewModel> cards = groups[currentGroupIndex].Cards;
                for (int currentItemIndex = 0; currentItemIndex < cards.Count; currentItemIndex++)
                {
                    if (cards[currentItemIndex].MovieId == movieId)
                    {
                        groupIndex = currentGroupIndex;
                        itemIndex = currentItemIndex;
                        return true;
                    }
                }
            }
        }

        groupIndex = -1;
        itemIndex = -1;
        return false;
    }

    public static IReadOnlyList<MovieGroupViewModel> CreateGroups(
        IEnumerable<MovieCardViewModel>? cards
    )
    {
        Dictionary<DateOnly, List<MovieCardViewModel>> future = new();
        List<MovieCardViewModel> inTheaters = new();

        foreach (MovieCardViewModel card in cards ?? Array.Empty<MovieCardViewModel>())
        {
            if (card is null || card.ReleaseDate is not DateOnly date)
            {
                continue;
            }

            switch (card.Status)
            {
                case ReleaseStatus.Today:
                case ReleaseStatus.InTheatersNow:
                    inTheaters.Add(card);
                    break;
                case ReleaseStatus.Future:
                    if (!future.TryGetValue(date, out List<MovieCardViewModel>? group))
                    {
                        group = new List<MovieCardViewModel>();
                        future.Add(date, group);
                    }

                    group.Add(card);
                    break;
            }
        }

        List<MovieGroupViewModel> groups = new();
        if (inTheaters.Count > 0)
        {
            groups.Add(new MovieGroupViewModel(MovieGroupKind.InTheatersNow, null, inTheaters));
        }

        foreach (
            KeyValuePair<DateOnly, List<MovieCardViewModel>> pair in future.OrderBy(static pair =>
                pair.Key
            )
        )
        {
            groups.Add(new MovieGroupViewModel(MovieGroupKind.ReleaseDate, pair.Key, pair.Value));
        }

        return new ReadOnlyCollection<MovieGroupViewModel>(groups);
    }

    public static IReadOnlyList<MovieGroupViewModel> GroupByDate(
        IEnumerable<MovieCardViewModel>? cards
    ) => CreateGroups(cards);

    public static IReadOnlyList<MovieGroupViewModel> GroupByReleaseDate(
        IEnumerable<MovieCardViewModel>? cards
    ) => CreateGroups(cards);

    public static IReadOnlyList<MovieGroupViewModel> GroupByDate(
        IEnumerable<Movie>? movies,
        IClock? clock = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        INavigationService? navigationService = null
    ) => CreateGroups(movies, clock, releaseWindowPolicy, movieSafetyPolicy, navigationService);

    public static IReadOnlyList<MovieGroupViewModel> GroupByReleaseDate(
        IEnumerable<Movie>? movies,
        IClock? clock = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        INavigationService? navigationService = null
    ) => CreateGroups(movies, clock, releaseWindowPolicy, movieSafetyPolicy, navigationService);

    public static IReadOnlyList<MovieGroupViewModel> CreateGroups(
        IEnumerable<Movie>? movies,
        DateOnly today,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        INavigationService? navigationService = null
    ) =>
        CreateGroups(
            movies,
            new FixedClock(today),
            releaseWindowPolicy,
            movieSafetyPolicy,
            navigationService
        );

    public static IReadOnlyList<MovieGroupViewModel> CreateGroups(
        IEnumerable<Movie>? movies,
        IClock? clock = null,
        ReleaseWindowPolicy? releaseWindowPolicy = null,
        MovieSafetyPolicy? movieSafetyPolicy = null,
        INavigationService? navigationService = null
    )
    {
        DateOnly today = (clock ?? new SystemClock()).Today;
        IReadOnlyList<Movie> safeMovies = MovieCatalogSnapshot
            .Create(
                movies ?? Array.Empty<Movie>(),
                today,
                releaseWindowPolicy ?? GoodMovies.Core.ReleaseWindowPolicy.Default,
                movieSafetyPolicy ?? new MovieSafetyPolicy()
            )
            .Movies;
        MovieCardViewModel[] cards = safeMovies
            .Select(movie => new MovieCardViewModel(
                movie,
                clock,
                releaseWindowPolicy,
                navigationService
            ))
            .ToArray();
        return CreateGroups(cards);
    }

    public static IReadOnlyList<MovieGroupViewModel> CreateGroups(
        MovieCatalogSnapshot snapshot,
        IClock? clock = null,
        INavigationService? navigationService = null
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        DateOnly today = snapshot.AsOfDate ?? (clock ?? new SystemClock()).Today;
        return CreateGroups(
            snapshot.Movies,
            clock ?? new FixedClock(today),
            snapshot.ReleaseWindowPolicy,
            snapshot.MovieSafetyPolicy,
            navigationService
        );
    }

    private sealed class FixedClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; } = today;
    }
}
