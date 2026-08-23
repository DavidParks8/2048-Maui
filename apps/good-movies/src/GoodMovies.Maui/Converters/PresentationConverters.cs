using System.Collections.Concurrent;
using System.Globalization;
using GoodMovies.Core;
using GoodMovies.Maui.Resources.Strings;
using GoodMovies.ViewModels;

namespace GoodMovies.Maui.Converters;

public static class GoodMoviesTextFormatter
{
    private static readonly ConcurrentDictionary<string, Brush> Brushes = new(
        StringComparer.Ordinal
    );
    private static readonly Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);

    public static string FormatDate(DateOnly date) =>
        string.Format(
            CultureInfo.CurrentCulture,
            AppStrings.DateFormat,
            date.ToDateTime(TimeOnly.MinValue)
        );

    public static string FormatCompactDate(DateOnly date) =>
        string.Format(
            CultureInfo.CurrentCulture,
            AppStrings.CompactDateFormat,
            date.ToDateTime(TimeOnly.MinValue)
        );

    public static string FormatCount(int count)
    {
        string format = count == 1 ? AppStrings.MovieCountOne : AppStrings.MovieCountMany;
        return string.Format(CultureInfo.CurrentCulture, format, count);
    }

    public static string FormatSavedCount(int count)
    {
        string format = count == 1 ? AppStrings.SavedCountOne : AppStrings.SavedCountMany;
        return string.Format(CultureInfo.CurrentCulture, format, count);
    }

    public static string FormatStatus(ReleaseStatus status, int sleeps)
    {
        return status switch
        {
            ReleaseStatus.Future when sleeps == 1 => AppStrings.OneSleepStatus,
            ReleaseStatus.Future => string.Format(
                CultureInfo.CurrentCulture,
                AppStrings.ManySleepsStatus,
                sleeps
            ),
            ReleaseStatus.Today => AppStrings.InTheatersToday,
            ReleaseStatus.InTheatersNow => AppStrings.InTheatersNow,
            _ => string.Empty,
        };
    }

    public static string FormatGroupTitle(MovieGroupViewModel group)
    {
        if (group.IsInTheatersNow)
        {
            return AppStrings.InTheatersNow;
        }

        return group.ReleaseDate is DateOnly date ? FormatDate(date) : AppStrings.ComingSoon;
    }

    public static string FormatGroupStatus(MovieGroupViewModel group)
    {
        return group.Count == 1
            ? AppStrings.OneMovieInGroup
            : string.Format(CultureInfo.CurrentCulture, AppStrings.ManyMoviesInGroup, group.Count);
    }

    public static string FormatCardAccessibility(MovieCardViewModel card) =>
        string.Format(
            CultureInfo.CurrentCulture,
            AppStrings.MovieCardAccessibility,
            card.Title,
            FormatStatus(card.Status, card.Sleeps),
            card.Rating is { Length: > 0 } rating ? rating : AppStrings.RatingComingSoon,
            string.IsNullOrWhiteSpace(card.Kind) ? AppStrings.MovieKindFallback : card.Kind
        );

    public static string FormatFavoriteAccessibility(MovieCardViewModel card) =>
        card.IsFavorite
            ? string.Format(CultureInfo.CurrentCulture, AppStrings.RemoveFavoriteHint, card.Title)
            : string.Format(CultureInfo.CurrentCulture, AppStrings.AddFavoriteHint, card.Title);

    public static string FormatFavoriteStatus(bool isFavorite) =>
        isFavorite ? AppStrings.SavedToFavorites : AppStrings.TapHeartToSave;

    public static string FormatDetailStatus(MovieDetailViewModel detail) =>
        FormatStatus(detail.Status, detail.Sleeps);

    public static string GetMessageTitle(CatalogMessageKey key) =>
        key switch
        {
            CatalogMessageKey.Loading => AppStrings.LoadingTitle,
            CatalogMessageKey.SearchPrompt => AppStrings.SearchPromptTitle,
            CatalogMessageKey.NoSearchResults => AppStrings.NoSearchResultsTitle,
            CatalogMessageKey.NoFavorites => AppStrings.NoFavoritesTitle,
            CatalogMessageKey.NoMovies => AppStrings.NoMoviesTitle,
            CatalogMessageKey.MissingToken => AppStrings.MissingTokenTitle,
            CatalogMessageKey.RefreshError => AppStrings.RefreshErrorTitle,
            CatalogMessageKey.RefreshWarning => AppStrings.RefreshWarningTitle,
            CatalogMessageKey.OfflineWarning => AppStrings.OfflineWarningTitle,
            CatalogMessageKey.OfflineError => AppStrings.OfflineErrorTitle,
            CatalogMessageKey.FavoritesError => AppStrings.FavoritesErrorTitle,
            CatalogMessageKey.FavoriteSaveFailed => AppStrings.FavoriteSaveFailedTitle,
            CatalogMessageKey.FavoriteNotAllowed => AppStrings.FavoriteNotAllowedTitle,
            CatalogMessageKey.SpeechFailed => AppStrings.SpeechFailedTitle,
            _ => string.Empty,
        };

    internal static Brush GetBrush(string key)
    {
        object? resource = null;
        _ = Application.Current?.Resources.TryGetValue(key, out resource);
        return resource switch
        {
            Brush brush => brush,
            Color color => Brushes.GetOrAdd(key, _ => new SolidColorBrush(color)),
            _ => TransparentBrush,
        };
    }

    internal static Color GetColor(string key, Color fallback)
    {
        object? resource = null;
        _ = Application.Current?.Resources.TryGetValue(key, out resource);
        return resource switch
        {
            Color color => color,
            SolidColorBrush brush => brush.Color,
            _ => fallback,
        };
    }

    public static string GetMessageBody(CatalogMessageKey key) =>
        key switch
        {
            CatalogMessageKey.Loading => AppStrings.LoadingMessage,
            CatalogMessageKey.SearchPrompt => AppStrings.SearchPromptMessage,
            CatalogMessageKey.NoSearchResults => AppStrings.NoSearchResultsMessage,
            CatalogMessageKey.NoFavorites => AppStrings.NoFavoritesMessage,
            CatalogMessageKey.NoMovies => AppStrings.NoMoviesMessage,
            CatalogMessageKey.MissingToken => AppStrings.MissingTokenMessage,
            CatalogMessageKey.RefreshError => AppStrings.RefreshErrorMessage,
            CatalogMessageKey.RefreshWarning => AppStrings.RefreshWarningMessage,
            CatalogMessageKey.OfflineWarning => AppStrings.OfflineWarningMessage,
            CatalogMessageKey.OfflineError => AppStrings.OfflineErrorMessage,
            CatalogMessageKey.FavoritesError => AppStrings.FavoritesErrorMessage,
            CatalogMessageKey.FavoriteSaveFailed => AppStrings.FavoriteSaveFailedMessage,
            CatalogMessageKey.FavoriteNotAllowed => AppStrings.FavoriteNotAllowedMessage,
            CatalogMessageKey.SpeechFailed => AppStrings.SpeechFailedMessage,
            _ => string.Empty,
        };
}

public abstract class OneWayValueConverter : IValueConverter
{
    public abstract object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    );

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class SectionTitleConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is CatalogSection section
            ? section switch
            {
                CatalogSection.MyFavorites => AppStrings.MyFavorites,
                CatalogSection.FindAMovie => AppStrings.FindAMovie,
                _ => AppStrings.ComingSoon,
            }
            : AppStrings.ComingSoon;
}

public sealed class SearchSectionVisibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is CatalogSection.FindAMovie;
}

public sealed class RefreshBannerVisibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is CatalogViewModel viewModel && (viewModel.IsWarning || viewModel.IsStale);
}

public sealed class RefreshBannerMessageConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is not CatalogViewModel viewModel)
        {
            return string.Empty;
        }

        CatalogMessageKey key = viewModel.WarningKey;
        if (key == CatalogMessageKey.None && viewModel.IsStale)
        {
            key = CatalogMessageKey.RefreshWarning;
        }

        return GoodMoviesTextFormatter.GetMessageBody(key);
    }
}

public sealed class MovieCountConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is int count ? GoodMoviesTextFormatter.FormatCount(count) : string.Empty;
}

public sealed class KindLabelConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is string text && !string.IsNullOrWhiteSpace(text)
            ? text
            : AppStrings.MovieKindFallback;
}

public sealed class RatingLabelConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is string text && !string.IsNullOrWhiteSpace(text)
            ? text
            : AppStrings.RatingComingSoon;
}

public sealed class CardStatusConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is MovieCardViewModel card
            ? GoodMoviesTextFormatter.FormatStatus(card.Status, card.Sleeps)
            : string.Empty;
}

public sealed class DetailStatusConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is MovieDetailViewModel detail
            ? GoodMoviesTextFormatter.FormatDetailStatus(detail)
            : string.Empty;
}

public sealed class GroupStatusConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is MovieGroupViewModel group
            ? GoodMoviesTextFormatter.FormatGroupStatus(group)
            : string.Empty;
}

public sealed class GroupAccessibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is not MovieGroupViewModel group)
        {
            return string.Empty;
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            AppStrings.GroupAccessibility,
            GoodMoviesTextFormatter.FormatGroupTitle(group),
            group.IsInTheatersNow
                ? GoodMoviesTextFormatter.FormatCount(group.Count)
                : GoodMoviesTextFormatter.FormatGroupStatus(group)
        );
    }
}

public sealed class DateOnlyConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is DateOnly date ? GoodMoviesTextFormatter.FormatDate(date) : string.Empty;
}

public sealed class MessageTitleConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is CatalogMessageKey key
            ? GoodMoviesTextFormatter.GetMessageTitle(key)
            : string.Empty;
}

public sealed class MessageBodyConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is CatalogMessageKey key ? GoodMoviesTextFormatter.GetMessageBody(key) : string.Empty;
}

public sealed class MessageAccessibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is not CatalogMessageKey key || key == CatalogMessageKey.None)
        {
            return string.Empty;
        }

        return string.Concat(
            GoodMoviesTextFormatter.GetMessageTitle(key),
            ". ",
            GoodMoviesTextFormatter.GetMessageBody(key)
        );
    }
}

public sealed class MessageIconConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is CatalogMessageKey key
            ? key switch
            {
                CatalogMessageKey.NoFavorites => AppStrings.HeartIcon,
                CatalogMessageKey.NoMovies => AppStrings.MovieIcon,
                CatalogMessageKey.SearchPrompt or CatalogMessageKey.NoSearchResults =>
                    AppStrings.SearchIcon,
                CatalogMessageKey.MissingToken => AppStrings.QuestionGlyph,
                CatalogMessageKey.RefreshError
                or CatalogMessageKey.RefreshWarning
                or CatalogMessageKey.OfflineError
                or CatalogMessageKey.OfflineWarning => AppStrings.RefreshGlyph,
                _ => AppStrings.MovieIcon,
            }
            : AppStrings.MovieIcon;
}

public sealed class StatePanelVisibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is CatalogViewState state
        && state
            is CatalogViewState.Error
                or CatalogViewState.MissingToken
                or CatalogViewState.Empty
                or CatalogViewState.SearchPrompt
                or CatalogViewState.NoResults;
}

public sealed class RetryVisibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is CatalogViewState state
        && state is CatalogViewState.Error or CatalogViewState.MissingToken;
}

public sealed class HeartGlyphConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is true ? AppStrings.FilledHeartGlyph : AppStrings.OutlineHeartGlyph;
}

public sealed class FavoriteTextConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is bool isFavorite
            ? GoodMoviesTextFormatter.FormatFavoriteStatus(isFavorite)
            : string.Empty;
}

public sealed class FavoriteButtonDescriptionConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is true ? AppStrings.RemoveFavorite : AppStrings.AddFavorite;
}

public sealed class CardAccessibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is MovieCardViewModel card
            ? GoodMoviesTextFormatter.FormatCardAccessibility(card)
            : string.Empty;
}

public sealed class MessageKeyVisibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is CatalogMessageKey key && key != CatalogMessageKey.None;
}

public sealed class TrailerButtonVisibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is TrailerPlaybackState state
        && state
            is TrailerPlaybackState.Idle
                or TrailerPlaybackState.Loading
                or TrailerPlaybackState.Ready
                or TrailerPlaybackState.Launched
                or TrailerPlaybackState.LaunchFailed;
}

public sealed class ReadAloudButtonTextConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is true ? AppStrings.StopReading : AppStrings.ReadAloud;
}

public sealed class PosterImageSourceConverter : OneWayValueConverter
{
    private static readonly ConcurrentDictionary<string, UriImageSource> RemoteSources = new(
        StringComparer.Ordinal
    );
    private static readonly ConcurrentDictionary<string, ImageSource> LocalSources = new(
        StringComparer.Ordinal
    );

    public override object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is Uri uri && IsTrustedRemoteUri(uri))
        {
            return CreateRemoteSource(OptimizeUri(uri, parameter));
        }

        if (value is string text && !string.IsNullOrWhiteSpace(text))
        {
            if (Uri.TryCreate(text, UriKind.Absolute, out Uri? absoluteUri))
            {
                return IsTrustedRemoteUri(absoluteUri)
                    ? CreateRemoteSource(OptimizeUri(absoluteUri, parameter))
                    : null;
            }

            return IsSafeLocalAssetName(text)
                ? LocalSources.GetOrAdd(text, static file => ImageSource.FromFile(file))
                : null;
        }

        return null;
    }

    private static UriImageSource CreateRemoteSource(Uri uri) =>
        RemoteSources.GetOrAdd(
            uri.AbsoluteUri,
            static absoluteUri => new UriImageSource
            {
                Uri = new Uri(absoluteUri, UriKind.Absolute),
                CachingEnabled = true,
                CacheValidity = TimeSpan.FromDays(5),
            }
        );

    private static Uri OptimizeUri(Uri uri, object? parameter)
    {
        if (
            parameter is not string { Length: > 0 } role
            || !string.Equals(role, "Card", StringComparison.Ordinal)
        )
        {
            return uri;
        }

        const string originalSize = "/t/p/w500/";
        int sizeIndex = uri.AbsoluteUri.IndexOf(originalSize, StringComparison.Ordinal);
        return sizeIndex < 0
            ? uri
            : new Uri(
                string.Concat(
                    uri.AbsoluteUri.AsSpan(0, sizeIndex),
                    "/t/p/w342/",
                    uri.AbsoluteUri.AsSpan(sizeIndex + originalSize.Length)
                ),
                UriKind.Absolute
            );
    }

    private static bool IsTrustedRemoteUri(Uri uri) =>
        uri.IsAbsoluteUri
        && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
        && string.Equals(uri.IdnHost, "image.tmdb.org", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeLocalAssetName(string value) =>
        !value.Contains("..", StringComparison.Ordinal)
        && !value.Contains('/', StringComparison.Ordinal)
        && !value.Contains('\\', StringComparison.Ordinal)
        && string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal);
}

public sealed class CardAutomationIdConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is MovieCardViewModel card
            ? string.Format(CultureInfo.InvariantCulture, "MovieCard-{0}", card.MovieId)
            : "MovieCard";
}

public sealed class CardFavoriteAutomationIdConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is MovieCardViewModel card
            ? string.Format(CultureInfo.InvariantCulture, "Favorite-{0}", card.MovieId)
            : "Favorite";
}

public sealed class StringVisibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is string text && !string.IsNullOrWhiteSpace(text);
}

public sealed class InverseStringVisibilityConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is not string text || string.IsNullOrWhiteSpace(text);
}

public sealed class SelectionBackgroundConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is true
            ? GoodMoviesTextFormatter.GetBrush("Accent")
            : GoodMoviesTextFormatter.GetBrush("Surface");
}

public sealed class SelectionTextColorConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is true
            ? GoodMoviesTextFormatter.GetColor("PageBackground", Colors.Black)
            : GoodMoviesTextFormatter.GetColor("White", Colors.White);
}

public sealed class TrailerStateMessageConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is TrailerPlaybackState state
            ? state switch
            {
                TrailerPlaybackState.Loading => AppStrings.TrailerLoading,
                TrailerPlaybackState.NotFound => AppStrings.TrailerNotFound,
                TrailerPlaybackState.MissingConfiguration => AppStrings.TrailerMissingConfiguration,
                TrailerPlaybackState.Failed or TrailerPlaybackState.LaunchFailed =>
                    AppStrings.TrailerLaunchFailed,
                _ => string.Empty,
            }
            : string.Empty;
}

public sealed class TrailerButtonTextConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is TrailerPlaybackState state
            ? state switch
            {
                TrailerPlaybackState.Loading => AppStrings.OpeningTrailer,
                TrailerPlaybackState.NotFound => AppStrings.TrailerUnavailableButton,
                _ => AppStrings.PlayTrailer,
            }
            : AppStrings.PlayTrailer;
}

public sealed class TrailerPlaybackActivityTextConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is true ? AppStrings.TrailerPlaying : AppStrings.PlayTrailer;
}

public sealed class TokenHighlightBackgroundConverter : OneWayValueConverter
{
    public override object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value is true
            ? GoodMoviesTextFormatter.GetBrush("Highlight")
            : GoodMoviesTextFormatter.GetBrush("Transparent");
}
