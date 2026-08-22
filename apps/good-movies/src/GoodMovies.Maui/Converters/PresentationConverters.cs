using System.Globalization;
using GoodMovies.Core;
using GoodMovies.Maui.Resources.Strings;
using GoodMovies.ViewModels;

namespace GoodMovies.Maui.Converters;

public static class GoodMoviesTextFormatter
{
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
}

public sealed class SectionTitleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CatalogSection section
            ? section switch
            {
                CatalogSection.MyFavorites => AppStrings.MyFavorites,
                CatalogSection.FindAMovie => AppStrings.FindAMovie,
                _ => AppStrings.ComingSoon,
            }
            : AppStrings.ComingSoon;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class SearchSectionVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CatalogSection.FindAMovie;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class RefreshBannerVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CatalogViewModel viewModel && (viewModel.IsWarning || viewModel.IsStale);

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class RefreshBannerMessageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

        return MessageBodyConverter.GetMessage(key);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class MovieCountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count ? GoodMoviesTextFormatter.FormatCount(count) : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class SavedCountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count ? GoodMoviesTextFormatter.FormatSavedCount(count) : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class KindLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text && !string.IsNullOrWhiteSpace(text)
            ? text
            : AppStrings.MovieKindFallback;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class RatingLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text && !string.IsNullOrWhiteSpace(text)
            ? text
            : AppStrings.RatingComingSoon;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class CardStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieCardViewModel card
            ? GoodMoviesTextFormatter.FormatStatus(card.Status, card.Sleeps)
            : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class DetailStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieDetailViewModel detail
            ? GoodMoviesTextFormatter.FormatDetailStatus(detail)
            : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class GroupTitleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieGroupViewModel group
            ? GoodMoviesTextFormatter.FormatGroupTitle(group)
            : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class GroupStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieGroupViewModel group
            ? GoodMoviesTextFormatter.FormatGroupStatus(group)
            : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class GroupAccessibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class DateOnlyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateOnly date ? GoodMoviesTextFormatter.FormatDate(date) : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class MessageTitleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CatalogMessageKey key ? GetTitle(key) : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();

    private static string GetTitle(CatalogMessageKey key) =>
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
            _ => AppStrings.LoadingTitle,
        };
}

public sealed class MessageBodyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CatalogMessageKey key ? GetMessage(key) : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();

    internal static string GetMessage(CatalogMessageKey key) =>
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

public sealed class MessageAccessibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CatalogMessageKey key || key == CatalogMessageKey.None)
        {
            return string.Empty;
        }

        return string.Concat(GetTitle(key), ". ", GetBody(key));
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();

    private static string GetTitle(CatalogMessageKey key) =>
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
            _ => AppStrings.LoadingTitle,
        };

    private static string GetBody(CatalogMessageKey key) =>
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

public sealed class MessageIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
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

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class StatePanelVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CatalogViewState state
        && state
            is CatalogViewState.Error
                or CatalogViewState.MissingToken
                or CatalogViewState.Empty
                or CatalogViewState.SearchPrompt
                or CatalogViewState.NoResults;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class RetryVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CatalogViewState state
        && state is CatalogViewState.Error or CatalogViewState.MissingToken;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class HeartGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? AppStrings.FilledHeartGlyph : AppStrings.OutlineHeartGlyph;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class FavoriteTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool isFavorite
            ? GoodMoviesTextFormatter.FormatFavoriteStatus(isFavorite)
            : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class FavoriteAccessibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieCardViewModel card
            ? GoodMoviesTextFormatter.FormatFavoriteAccessibility(card)
            : AppStrings.AddFavorite;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class FavoriteHintConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieCardViewModel card
            ? card.IsFavorite
                ? AppStrings.RemoveFavorite
                : AppStrings.AddFavorite
            : AppStrings.AddFavorite;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class FavoriteButtonDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? AppStrings.RemoveFavorite : AppStrings.AddFavorite;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class CardAccessibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieCardViewModel card
            ? GoodMoviesTextFormatter.FormatCardAccessibility(card)
            : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class MessageKeyVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CatalogMessageKey key && key != CatalogMessageKey.None;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class TrailerMessageVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TrailerPlaybackState state
        && state
            is TrailerPlaybackState.NotFound
                or TrailerPlaybackState.MissingConfiguration
                or TrailerPlaybackState.Failed
                or TrailerPlaybackState.LaunchFailed
                or TrailerPlaybackState.Launched;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class TrailerButtonVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TrailerPlaybackState state
        && state
            is TrailerPlaybackState.Idle
                or TrailerPlaybackState.Loading
                or TrailerPlaybackState.Ready
                or TrailerPlaybackState.Launched
                or TrailerPlaybackState.LaunchFailed;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class ReadAloudButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? AppStrings.StopReading : AppStrings.ReadAloud;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class PosterImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Uri uri && IsTrustedRemoteUri(uri))
        {
            return CreateRemoteSource(uri);
        }

        if (value is string text && !string.IsNullOrWhiteSpace(text))
        {
            if (Uri.TryCreate(text, UriKind.Absolute, out Uri? absoluteUri))
            {
                return IsTrustedRemoteUri(absoluteUri) ? CreateRemoteSource(absoluteUri) : null;
            }

            return IsSafeLocalAssetName(text) ? ImageSource.FromFile(text) : null;
        }

        return null;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();

    private static UriImageSource CreateRemoteSource(Uri uri) =>
        new()
        {
            Uri = uri,
            CachingEnabled = true,
            CacheValidity = TimeSpan.FromDays(5),
        };

    private static bool IsTrustedRemoteUri(Uri uri) =>
        uri.IsAbsoluteUri
        && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
        && string.Equals(uri.IdnHost, "image.tmdb.org", StringComparison.OrdinalIgnoreCase);

    private static bool IsSafeLocalAssetName(string value) =>
        !value.Contains("..", StringComparison.Ordinal)
        && !value.Contains('/')
        && !value.Contains('\\')
        && string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal);
}

public sealed class CardAutomationIdConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieCardViewModel card
            ? string.Format(CultureInfo.InvariantCulture, "MovieCard-{0}", card.MovieId)
            : "MovieCard";

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class CardFavoriteAutomationIdConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is MovieCardViewModel card
            ? string.Format(CultureInfo.InvariantCulture, "Favorite-{0}", card.MovieId)
            : "Favorite";

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class StringVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text && !string.IsNullOrWhiteSpace(text);

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class InverseStringVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not string text || string.IsNullOrWhiteSpace(text);

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class SelectionBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? ResourceBrush("Accent", Colors.Transparent)
            : ResourceBrush("Surface", Colors.Transparent);

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();

    private static Brush ResourceBrush(string key, Color fallback)
    {
        object? resource = Application.Current?.Resources[key];
        return resource switch
        {
            Brush brush => brush,
            Color color => new SolidColorBrush(color),
            _ => new SolidColorBrush(fallback),
        };
    }
}

public sealed class SelectionTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? Application.Current?.Resources["PageBackground"] as Color ?? Colors.Black
            : Application.Current?.Resources["White"] as Color ?? Colors.White;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class TrailerStateMessageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TrailerPlaybackState state
            ? state switch
            {
                TrailerPlaybackState.Loading => AppStrings.TrailerLoading,
                TrailerPlaybackState.NotFound => AppStrings.TrailerNotFound,
                TrailerPlaybackState.MissingConfiguration => AppStrings.TrailerMissingConfiguration,
                TrailerPlaybackState.Failed or TrailerPlaybackState.LaunchFailed =>
                    AppStrings.TrailerLaunchFailed,
                TrailerPlaybackState.Launched => AppStrings.TrailerOpened,
                _ => string.Empty,
            }
            : string.Empty;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class TrailerButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TrailerPlaybackState state
            ? state switch
            {
                TrailerPlaybackState.Loading => AppStrings.OpeningTrailer,
                TrailerPlaybackState.NotFound => AppStrings.TrailerUnavailableButton,
                _ => AppStrings.PlayTrailer,
            }
            : AppStrings.PlayTrailer;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}

public sealed class TokenHighlightBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? ResourceBrush("Highlight", Colors.Transparent)
            : new SolidColorBrush(Colors.Transparent);

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();

    private static Brush ResourceBrush(string key, Color fallback)
    {
        object? resource = Application.Current?.Resources[key];
        return resource switch
        {
            Brush brush => brush,
            Color color => new SolidColorBrush(color),
            _ => new SolidColorBrush(fallback),
        };
    }
}

public sealed class TokenTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? Application.Current?.Resources["PageBackground"] as Color ?? Colors.Black
            : Application.Current?.Resources["White"] as Color ?? Colors.White;

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
