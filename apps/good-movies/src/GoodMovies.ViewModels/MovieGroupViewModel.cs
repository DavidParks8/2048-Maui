using System.Collections;
using System.Collections.ObjectModel;
using GoodMovies.Core;

namespace GoodMovies.ViewModels;

/// <summary>
/// A semantic group of cards. The UI localizes its heading and formats its date.
/// </summary>
public sealed class MovieGroupViewModel : IEnumerable<MovieCardViewModel>
{
    private MovieGroupViewModel(
        MovieGroupKind kind,
        DateOnly? releaseDate,
        List<MovieCardViewModel> cards
    )
    {
        GroupKind = kind;
        ReleaseDate = kind == MovieGroupKind.InTheatersNow ? null : releaseDate;
        cards.Sort(CompareCards);
        Cards = new ReadOnlyCollection<MovieCardViewModel>(cards);
    }

    public MovieGroupKind GroupKind { get; }

    public DateOnly? ReleaseDate { get; }

    public IReadOnlyList<MovieCardViewModel> Cards { get; }

    public int Count => Cards.Count;

    public bool IsInTheatersNow => GroupKind == MovieGroupKind.InTheatersNow;

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

        return groups.Count == 0
            ? Array.Empty<MovieGroupViewModel>()
            : new ReadOnlyCollection<MovieGroupViewModel>(groups);
    }

    private static int CompareCards(MovieCardViewModel left, MovieCardViewModel right)
    {
        int result = (left.ReleaseDate ?? DateOnly.MaxValue).CompareTo(
            right.ReleaseDate ?? DateOnly.MaxValue
        );
        if (result == 0)
        {
            result = StringComparer.OrdinalIgnoreCase.Compare(left.Title, right.Title);
        }

        if (result == 0)
        {
            result = StringComparer.Ordinal.Compare(left.Title, right.Title);
        }

        return result == 0 ? left.MovieId.CompareTo(right.MovieId) : result;
    }
}
