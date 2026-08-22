// Manually maintained strongly typed accessors for AppStrings.resx.
#nullable enable

namespace GoodMovies.Maui.Resources.Strings;

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;

[GeneratedCode(
    "System.Resources.Tools.StronglyTypedResourceBuilder",
    "10.0.0.0"
)]
[DebuggerNonUserCode]
internal static class AppStrings
{
    private static ResourceManager? resourceMan;
    private static CultureInfo? resourceCulture;

    internal static ResourceManager ResourceManager
    {
        get
        {
            resourceMan ??= new ResourceManager(
                "GoodMovies.Maui.Resources.Strings.AppStrings",
                typeof(AppStrings).Assembly
            );
            return resourceMan;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo? Culture
    {
        get => resourceCulture;
        set => resourceCulture = value;
    }

    internal static string AppTitle => Get(nameof(AppTitle));
    internal static string PopcornIcon => Get(nameof(PopcornIcon));
    internal static string MovieIcon => Get(nameof(MovieIcon));
    internal static string HeartIcon => Get(nameof(HeartIcon));
    internal static string SearchIcon => Get(nameof(SearchIcon));
    internal static string CalendarIcon => Get(nameof(CalendarIcon));
    internal static string ClearGlyph => Get(nameof(ClearGlyph));
    internal static string FilledHeartGlyph => Get(nameof(FilledHeartGlyph));
    internal static string OutlineHeartGlyph => Get(nameof(OutlineHeartGlyph));
    internal static string RefreshGlyph => Get(nameof(RefreshGlyph));
    internal static string QuestionGlyph => Get(nameof(QuestionGlyph));
    internal static string ComingSoon => Get(nameof(ComingSoon));
    internal static string MyFavorites => Get(nameof(MyFavorites));
    internal static string FindAMovie => Get(nameof(FindAMovie));
    internal static string NavComingLabel => Get(nameof(NavComingLabel));
    internal static string NavFavoritesLabel => Get(nameof(NavFavoritesLabel));
    internal static string NavSearchLabel => Get(nameof(NavSearchLabel));
    internal static string NavSearchSubtext => Get(nameof(NavSearchSubtext));
    internal static string NavComingAccessibility => Get(nameof(NavComingAccessibility));
    internal static string NavFavoritesAccessibility => Get(nameof(NavFavoritesAccessibility));
    internal static string NavSearchAccessibility => Get(nameof(NavSearchAccessibility));
    internal static string NavTileAccessibilityFormat => Get(nameof(NavTileAccessibilityFormat));
    internal static string SelectedState => Get(nameof(SelectedState));
    internal static string SelectedAccessibilityFormat => Get(nameof(SelectedAccessibilityFormat));
    internal static string DateFormat => Get(nameof(DateFormat));
    internal static string CompactDateFormat => Get(nameof(CompactDateFormat));
    internal static string MovieCountOne => Get(nameof(MovieCountOne));
    internal static string MovieCountMany => Get(nameof(MovieCountMany));
    internal static string RatingComingSoon => Get(nameof(RatingComingSoon));
    internal static string RatingFilterHeading => Get(nameof(RatingFilterHeading));
    internal static string RatingFilterAll => Get(nameof(RatingFilterAll));
    internal static string RatingFilterG => Get(nameof(RatingFilterG));
    internal static string RatingFilterPG => Get(nameof(RatingFilterPG));
    internal static string RatingFilterHint => Get(nameof(RatingFilterHint));
    internal static string SelectedRatingFilterFormat => Get(nameof(SelectedRatingFilterFormat));
    internal static string MovieKindFallback => Get(nameof(MovieKindFallback));
    internal static string SavedCountOne => Get(nameof(SavedCountOne));
    internal static string SavedCountMany => Get(nameof(SavedCountMany));
    internal static string OneMovieInGroup => Get(nameof(OneMovieInGroup));
    internal static string ManyMoviesInGroup => Get(nameof(ManyMoviesInGroup));
    internal static string InTheatersNow => Get(nameof(InTheatersNow));
    internal static string InTheatersToday => Get(nameof(InTheatersToday));
    internal static string OneSleepStatus => Get(nameof(OneSleepStatus));
    internal static string ManySleepsStatus => Get(nameof(ManySleepsStatus));
    internal static string MovieCardAccessibility => Get(nameof(MovieCardAccessibility));
    internal static string OpenMovieHint => Get(nameof(OpenMovieHint));
    internal static string AddFavoriteHint => Get(nameof(AddFavoriteHint));
    internal static string RemoveFavoriteHint => Get(nameof(RemoveFavoriteHint));
    internal static string AddFavorite => Get(nameof(AddFavorite));
    internal static string RemoveFavorite => Get(nameof(RemoveFavorite));
    internal static string SavedToFavorites => Get(nameof(SavedToFavorites));
    internal static string TapHeartToSave => Get(nameof(TapHeartToSave));
    internal static string SearchPlaceholder => Get(nameof(SearchPlaceholder));
    internal static string ClearSearch => Get(nameof(ClearSearch));
    internal static string SearchAccessibility => Get(nameof(SearchAccessibility));
    internal static string GroupAccessibility => Get(nameof(GroupAccessibility));
    internal static string LoadingTitle => Get(nameof(LoadingTitle));
    internal static string LoadingMessage => Get(nameof(LoadingMessage));
    internal static string LoadingAnnouncement => Get(nameof(LoadingAnnouncement));
    internal static string SearchPromptTitle => Get(nameof(SearchPromptTitle));
    internal static string SearchPromptMessage => Get(nameof(SearchPromptMessage));
    internal static string NoSearchResultsTitle => Get(nameof(NoSearchResultsTitle));
    internal static string NoSearchResultsMessage => Get(nameof(NoSearchResultsMessage));
    internal static string NoFavoritesTitle => Get(nameof(NoFavoritesTitle));
    internal static string NoFavoritesMessage => Get(nameof(NoFavoritesMessage));
    internal static string NoMoviesTitle => Get(nameof(NoMoviesTitle));
    internal static string NoMoviesMessage => Get(nameof(NoMoviesMessage));
    internal static string MissingTokenTitle => Get(nameof(MissingTokenTitle));
    internal static string MissingTokenMessage => Get(nameof(MissingTokenMessage));
    internal static string RefreshErrorTitle => Get(nameof(RefreshErrorTitle));
    internal static string RefreshErrorMessage => Get(nameof(RefreshErrorMessage));
    internal static string RefreshWarningTitle => Get(nameof(RefreshWarningTitle));
    internal static string RefreshWarningMessage => Get(nameof(RefreshWarningMessage));
    internal static string OfflineWarningTitle => Get(nameof(OfflineWarningTitle));
    internal static string OfflineWarningMessage => Get(nameof(OfflineWarningMessage));
    internal static string OfflineErrorTitle => Get(nameof(OfflineErrorTitle));
    internal static string OfflineErrorMessage => Get(nameof(OfflineErrorMessage));
    internal static string FavoritesErrorTitle => Get(nameof(FavoritesErrorTitle));
    internal static string FavoritesErrorMessage => Get(nameof(FavoritesErrorMessage));
    internal static string FavoriteSaveFailedTitle => Get(nameof(FavoriteSaveFailedTitle));
    internal static string FavoriteSaveFailedMessage => Get(nameof(FavoriteSaveFailedMessage));
    internal static string FavoriteNotAllowedTitle => Get(nameof(FavoriteNotAllowedTitle));
    internal static string FavoriteNotAllowedMessage => Get(nameof(FavoriteNotAllowedMessage));
    internal static string SpeechFailedTitle => Get(nameof(SpeechFailedTitle));
    internal static string SpeechFailedMessage => Get(nameof(SpeechFailedMessage));
    internal static string Retry => Get(nameof(Retry));
    internal static string Refreshing => Get(nameof(Refreshing));
    internal static string PosterFallbackAccessibility => Get(nameof(PosterFallbackAccessibility));
    internal static string ReadAloud => Get(nameof(ReadAloud));
    internal static string StopReading => Get(nameof(StopReading));
    internal static string GoBack => Get(nameof(GoBack));
    internal static string BackLabel => Get(nameof(BackLabel));
    internal static string TapWordHint => Get(nameof(TapWordHint));
    internal static string Synopsis => Get(nameof(Synopsis));
    internal static string PlayTrailer => Get(nameof(PlayTrailer));
    internal static string OpeningTrailer => Get(nameof(OpeningTrailer));
    internal static string TrailerUnavailableButton => Get(nameof(TrailerUnavailableButton));
    internal static string TrailerLoading => Get(nameof(TrailerLoading));
    internal static string TrailerNotFound => Get(nameof(TrailerNotFound));
    internal static string TrailerMissingConfiguration => Get(nameof(TrailerMissingConfiguration));
    internal static string TrailerLaunchFailed => Get(nameof(TrailerLaunchFailed));
    internal static string TrailerOpened => Get(nameof(TrailerOpened));
    internal static string NoSynopsis => Get(nameof(NoSynopsis));
    internal static string DetailAccessibility => Get(nameof(DetailAccessibility));
    internal static string TodayStatus => Get(nameof(TodayStatus));

    private static string Get(string name) =>
        ResourceManager.GetString(name, resourceCulture) ?? string.Empty;
}
