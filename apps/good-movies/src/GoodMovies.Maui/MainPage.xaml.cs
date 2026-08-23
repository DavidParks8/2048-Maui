using System.ComponentModel;
using System.Globalization;
using CoreGraphics;
using GoodMovies.Maui.Components;
using GoodMovies.Maui.Converters;
using GoodMovies.Maui.Resources.Strings;
using GoodMovies.Maui.Services;
using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;
using UIKit;

namespace GoodMovies.Maui;

public partial class MainPage : ContentPage
{
    private const string ShimmerAnimationName = "GoodMoviesSkeletonShimmer";

    private readonly CatalogViewModel _viewModel;
    private readonly IScreenReaderService _screenReaderService;
    private readonly GridItemsLayout _movieItemsLayout;
    private CatalogSection _lastSection;
    private bool _layoutInitialized;
    private bool _isWide;
    private bool _isAppeared;
    private bool _hasStartedInitialization;
    private bool _loadingWasAnnounced;
    private bool _refreshWasAnnounced;
    private bool _shimmerRunning;
    private int _movieNavigationPending;
    private UICollectionView? _nativeMovieCollection;
    private FeedScrollAnchor[]? _pendingFeedScrollAnchors;
    private int _feedScrollRestoreVersion;
    private bool _restoreFeedScrollAfterRefresh;

    public MainPage(
        CatalogViewModel viewModel,
        NavigationViewModel navigationViewModel,
        IScreenReaderService screenReaderService
    )
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        NavigationViewModel =
            navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
        _screenReaderService =
            screenReaderService ?? throw new ArgumentNullException(nameof(screenReaderService));
        BindingContext = _viewModel;
        InitializeComponent();

        _movieItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
        {
            HorizontalItemSpacing = 14,
            VerticalItemSpacing = 14,
        };
        MovieCollection.ItemsLayout = _movieItemsLayout;
        ComingTile.Command = NavigationViewModel.SwitchSectionCommand;
        FavoritesTile.Command = NavigationViewModel.SwitchSectionCommand;
        SearchTile.Command = NavigationViewModel.SwitchSectionCommand;
        CompactComingTile.Command = NavigationViewModel.SwitchSectionCommand;
        CompactFavoritesTile.Command = NavigationViewModel.SwitchSectionCommand;
        CompactSearchTile.Command = NavigationViewModel.SwitchSectionCommand;
        _lastSection = NavigationViewModel.SelectedSection;
        _viewModel.FeedReplacementStarting += OnFeedReplacementStarting;
        _viewModel.FeedReplacementCompleted += OnFeedReplacementCompleted;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        SizeChanged += OnSizeChanged;
        UpdateNavigationState();
        UpdateRatingFilterState();
    }

    public NavigationViewModel NavigationViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isAppeared = true;
        UpdateNavigationState();
        UpdateShimmer();
        if (!_hasStartedInitialization)
        {
            _hasStartedInitialization = true;
            _ = InitializeCatalogAsync();
        }
    }

    protected override void OnDisappearing()
    {
        _isAppeared = false;
        StopShimmer();
        base.OnDisappearing();
    }

    private void UpdateShimmer()
    {
        if (_viewModel.IsLoading && _isAppeared)
        {
            StartShimmer();
        }
        else
        {
            StopShimmer();
        }
    }

    private void StartShimmer()
    {
        if (_shimmerRunning || UIAccessibility.IsReduceMotionEnabled)
        {
            return;
        }

        View[] targets = SkeletonBlocks().ToArray();
        if (targets.Length == 0)
        {
            return;
        }

        _shimmerRunning = true;
        Animation shimmer = [];
        for (int index = 0; index < targets.Length; index++)
        {
            View target = targets[index];
            double start = Math.Min(0.45, index * 0.05);
            double middle = start + 0.25;
            double end = middle + 0.25;
            shimmer.Add(
                start,
                middle,
                new Animation(value => target.Opacity = value, 1d, 0.35d, Easing.SinInOut)
            );
            shimmer.Add(
                middle,
                end,
                new Animation(value => target.Opacity = value, 0.35d, 1d, Easing.SinInOut)
            );
        }

        shimmer.Commit(this, ShimmerAnimationName, 16, 1500, null, null, () => _shimmerRunning);
    }

    private void StopShimmer()
    {
        if (!_shimmerRunning)
        {
            return;
        }

        _shimmerRunning = false;
        this.AbortAnimation(ShimmerAnimationName);
        foreach (View target in SkeletonBlocks())
        {
            target.Opacity = 1d;
        }
    }

    private IEnumerable<View> SkeletonBlocks()
    {
        foreach (
            Layout host in new Layout[] { RailSkeleton, SkeletonSurface, CompactNavigationSkeleton }
        )
        {
            foreach (Border block in FindBorders(host))
            {
                yield return block;
            }
        }
    }

    private static IEnumerable<Border> FindBorders(IView view)
    {
        switch (view)
        {
            case Border border:
                yield return border;
                break;
            case Layout layout:
                foreach (IView child in layout.Children)
                {
                    foreach (Border border in FindBorders(child))
                    {
                        yield return border;
                    }
                }

                break;
        }
    }

    private async Task InitializeCatalogAsync()
    {
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (OperationCanceledException) { }
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        bool isWide = Width >= 900 && Width > Height;
        if (_layoutInitialized && isWide == _isWide)
        {
            return;
        }

        _layoutInitialized = true;
        _isWide = isWide;

        if (Handler is IPlatformViewHandler { ViewController: { View: { } view } })
        {
            UIView.PerformWithoutAnimation(() =>
            {
                ApplyLayout(isWide);
                view.SetNeedsLayout();
                view.LayoutIfNeeded();
            });
            return;
        }

        ApplyLayout(isWide);
    }

    private void ApplyLayout(bool isWide)
    {
        if (isWide)
        {
            RootLayout.ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(262),
                new(GridLength.Star),
            };
            Grid.SetColumn(Rail, 0);
            Grid.SetColumn(MainContent, 1);
            Grid.SetColumnSpan(BottomNavigation, 2);
            Rail.IsVisible = true;
            BottomNavigation.IsVisible = false;
            MainContent.Padding = new Thickness(8, 18, 22, 0);
            MainTitle.FontSize = 44;
        }
        else
        {
            RootLayout.ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star) };
            Grid.SetColumn(MainContent, 0);
            Grid.SetColumn(BottomNavigation, 0);
            Grid.SetColumnSpan(BottomNavigation, 1);
            Rail.IsVisible = false;
            BottomNavigation.IsVisible = true;
            MainContent.Padding = new Thickness(14, 14, 14, 0);
            MainTitle.FontSize = Width >= 700 ? 44 : 32;
        }

        // Change the CollectionView layout only after its surrounding grid has
        // reached the matching shape, avoiding a stretched intermediate frame.
        _movieItemsLayout.Span = isWide ? 2 : 1;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => OnViewModelPropertyChanged(sender, e));
            return;
        }

        if (e.PropertyName == nameof(CatalogViewModel.IsLoading))
        {
            UpdateShimmer();
            if (_viewModel.IsLoading && !_loadingWasAnnounced)
            {
                _screenReaderService.Announce(AppStrings.LoadingAnnouncement);
                _loadingWasAnnounced = true;
            }
            else if (!_viewModel.IsLoading)
            {
                _loadingWasAnnounced = false;
            }
        }

        if (e.PropertyName == nameof(CatalogViewModel.IsRefreshing))
        {
            if (_viewModel.IsRefreshing && !_refreshWasAnnounced)
            {
                _screenReaderService.Announce(AppStrings.Refreshing);
                _refreshWasAnnounced = true;
            }
            else if (!_viewModel.IsRefreshing)
            {
                _refreshWasAnnounced = false;
                if (_restoreFeedScrollAfterRefresh)
                {
                    _restoreFeedScrollAfterRefresh = false;
                    ScheduleFeedScrollRestore();
                }
            }
        }

        if (
            e.PropertyName
            is nameof(CatalogViewModel.SelectedSection)
                or nameof(CatalogViewModel.CurrentSection)
                or nameof(CatalogViewModel.ComingSoonCount)
                or nameof(CatalogViewModel.FavoriteCount)
        )
        {
            if (e.PropertyName == nameof(CatalogViewModel.SelectedSection))
            {
                ClearFeedScrollSnapshot();
            }

            CatalogSection section = NavigationViewModel.SelectedSection;
            bool switchedToSearch =
                _isAppeared
                && section == CatalogSection.FindAMovie
                && _lastSection != CatalogSection.FindAMovie;
            _lastSection = section;
            UpdateNavigationState();
            if (switchedToSearch)
            {
                Dispatcher.Dispatch(() =>
                {
                    if (SearchPanel.IsVisible)
                    {
                        SearchEntry.Focus();
                    }
                });
            }
            else if (section != CatalogSection.FindAMovie)
            {
                SearchEntry.Unfocus();
            }
        }

        if (e.PropertyName == nameof(CatalogViewModel.SelectedRatingFilter))
        {
            ClearFeedScrollSnapshot();
            UpdateRatingFilterState();
        }
    }

    private void OnFeedReplacementStarting(object? sender, EventArgs e)
    {
        if (_viewModel.MovieCards.Count == 0)
        {
            return;
        }

        // Capture only at the replacement boundary. Reading native layout from
        // the Scrolled event caused a visible hitch during drag and momentum.
        _pendingFeedScrollAnchors = MainThread.IsMainThread ? CaptureVisibleFeedAnchors() : null;
    }

    private void OnFeedReplacementCompleted(object? sender, EventArgs e)
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => OnFeedReplacementCompleted(sender, e));
            return;
        }

        if (_viewModel.IsRefreshing)
        {
            _restoreFeedScrollAfterRefresh = true;
            return;
        }

        ScheduleFeedScrollRestore();
    }

    private void ClearFeedScrollSnapshot()
    {
        _pendingFeedScrollAnchors = null;
        _restoreFeedScrollAfterRefresh = false;
        _feedScrollRestoreVersion++;
    }

    /// <summary>
    /// Measures every visible card. Runs once per catalog replacement, never
    /// while scrolling, so the extra layout queries are not on a hot path.
    /// </summary>
    private FeedScrollAnchor[]? CaptureVisibleFeedAnchors()
    {
        UICollectionView? collectionView = GetNativeMovieCollection();
        if (collectionView is null)
        {
            return null;
        }

        List<FeedScrollAnchor> anchors = new(collectionView.IndexPathsForVisibleItems.Length);
        foreach (Foundation.NSIndexPath indexPath in collectionView.IndexPathsForVisibleItems)
        {
            if (ResolveAnchor(collectionView, indexPath) is { } anchor)
            {
                anchors.Add(anchor);
            }
        }

        // OffsetFromItemTop decreases as items sit further down the viewport,
        // so descending order puts the topmost visible card first.
        return anchors.Count == 0
            ? null
            : anchors.OrderByDescending(static anchor => anchor.OffsetFromItemTop).ToArray();
    }

    private FeedScrollAnchor? ResolveAnchor(
        UICollectionView collectionView,
        Foundation.NSIndexPath indexPath
    )
    {
        int groupIndex = (int)indexPath.Section;
        int itemIndex = (int)indexPath.Item;
        if (
            groupIndex < 0
            || groupIndex >= _viewModel.MovieGroups.Count
            || itemIndex < 0
            || itemIndex >= _viewModel.MovieGroups[groupIndex].Cards.Count
        )
        {
            return null;
        }

        UICollectionViewLayoutAttributes? attributes =
            collectionView.CollectionViewLayout.LayoutAttributesForItem(indexPath);
        return attributes is null
            ? null
            : new FeedScrollAnchor(
                _viewModel.MovieGroups[groupIndex].Cards[itemIndex].MovieId,
                collectionView.ContentOffset.Y - attributes.Frame.Y
            );
    }

    private UICollectionView? GetNativeMovieCollection()
    {
        if (_nativeMovieCollection?.Window is not null)
        {
            return _nativeMovieCollection;
        }

        _nativeMovieCollection = MovieCollection.Handler?.PlatformView is UIView platformView
            ? FindCollectionView(platformView)
            : null;
        return _nativeMovieCollection;
    }

    private static UICollectionView? FindCollectionView(UIView view)
    {
        if (view is UICollectionView collectionView)
        {
            return collectionView;
        }

        foreach (UIView subview in view.Subviews)
        {
            UICollectionView? match = FindCollectionView(subview);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private void ScheduleFeedScrollRestore()
    {
        FeedScrollAnchor[]? anchors = _pendingFeedScrollAnchors;
        _pendingFeedScrollAnchors = null;
        if (anchors is null)
        {
            return;
        }

        int version = ++_feedScrollRestoreVersion;
        CatalogSection section = _viewModel.SelectedSection;
        MovieRatingFilter filter = _viewModel.SelectedRatingFilter;
        Dispatcher.DispatchDelayed(
            TimeSpan.FromMilliseconds(32),
            () => RestoreFeedScrollPosition(anchors, section, filter, version, attempt: 0)
        );
    }

    private void RestoreFeedScrollPosition(
        IReadOnlyList<FeedScrollAnchor> anchors,
        CatalogSection section,
        MovieRatingFilter filter,
        int version,
        int attempt
    )
    {
        if (
            version != _feedScrollRestoreVersion
            || section != _viewModel.SelectedSection
            || filter != _viewModel.SelectedRatingFilter
            || GetNativeMovieCollection() is not { } collectionView
        )
        {
            return;
        }

        if (collectionView.Dragging || collectionView.Decelerating || collectionView.Tracking)
        {
            // The user owns the scroll position once they touch the feed, so
            // abandon restoration instead of fighting their momentum.
            _feedScrollRestoreVersion++;
            return;
        }

        collectionView.LayoutIfNeeded();
        foreach (FeedScrollAnchor anchor in anchors)
        {
            if (
                !MovieGroupViewModel.TryFindMovie(
                    _viewModel.MovieGroups,
                    anchor.MovieId,
                    out int groupIndex,
                    out int itemIndex
                )
            )
            {
                continue;
            }

            Foundation.NSIndexPath indexPath = Foundation.NSIndexPath.FromItemSection(
                itemIndex,
                groupIndex
            );
            UICollectionViewLayoutAttributes? attributes =
                collectionView.CollectionViewLayout.LayoutAttributesForItem(indexPath);
            if (attributes is not null)
            {
                double minimumOffset = -collectionView.AdjustedContentInset.Top;
                double maximumOffset = Math.Max(
                    minimumOffset,
                    collectionView.ContentSize.Height
                        - collectionView.Bounds.Height
                        + collectionView.AdjustedContentInset.Bottom
                );
                double targetOffset = Math.Clamp(
                    attributes.Frame.Y + anchor.OffsetFromItemTop,
                    minimumOffset,
                    maximumOffset
                );
                UIView.PerformWithoutAnimation(() =>
                    collectionView.SetContentOffset(
                        new CGPoint(collectionView.ContentOffset.X, targetOffset),
                        animated: false
                    )
                );

                if (attempt < 1)
                {
                    // One settling pass covers self-sizing group headers that
                    // finish measuring after the reload.
                    Dispatcher.DispatchDelayed(
                        TimeSpan.FromMilliseconds(100),
                        () =>
                            RestoreFeedScrollPosition(
                                anchors,
                                section,
                                filter,
                                version,
                                attempt + 1
                            )
                    );
                }

                return;
            }
        }

        if (attempt < 4)
        {
            Dispatcher.DispatchDelayed(
                TimeSpan.FromMilliseconds(32),
                () => RestoreFeedScrollPosition(anchors, section, filter, version, attempt + 1)
            );
        }
    }

    private void UpdateNavigationState()
    {
        CatalogSection selected = NavigationViewModel.SelectedSection;
        ComingTile.IsSelected = selected == CatalogSection.ComingSoon;
        FavoritesTile.IsSelected = selected == CatalogSection.MyFavorites;
        SearchTile.IsSelected = selected == CatalogSection.FindAMovie;
        CompactComingTile.IsSelected = selected == CatalogSection.ComingSoon;
        CompactFavoritesTile.IsSelected = selected == CatalogSection.MyFavorites;
        CompactSearchTile.IsSelected = selected == CatalogSection.FindAMovie;
        ComingTile.Subtext = GoodMoviesTextFormatter.FormatCount(
            NavigationViewModel.ComingSoonCount
        );
        FavoritesTile.Subtext = GoodMoviesTextFormatter.FormatSavedCount(
            NavigationViewModel.FavoriteCount
        );

        ComingTile.AccessibilityLabel = FormatNavigationLabel(
            AppStrings.NavComingAccessibility,
            GoodMoviesTextFormatter.FormatCount(NavigationViewModel.ComingSoonCount),
            selected == CatalogSection.ComingSoon
        );
        FavoritesTile.AccessibilityLabel = FormatNavigationLabel(
            AppStrings.NavFavoritesAccessibility,
            GoodMoviesTextFormatter.FormatSavedCount(NavigationViewModel.FavoriteCount),
            selected == CatalogSection.MyFavorites
        );
        SearchTile.AccessibilityLabel = FormatNavigationLabel(
            AppStrings.NavSearchAccessibility,
            string.Empty,
            selected == CatalogSection.FindAMovie
        );
        CompactComingTile.AccessibilityLabel = ComingTile.AccessibilityLabel;
        CompactFavoritesTile.AccessibilityLabel = FavoritesTile.AccessibilityLabel;
        CompactSearchTile.AccessibilityLabel = SearchTile.AccessibilityLabel;
    }

    private void UpdateRatingFilterState()
    {
        SetRatingFilterButtonState(
            AllRatingFilterButton,
            MovieRatingFilter.All,
            AppStrings.RatingFilterAll
        );
        SetRatingFilterButtonState(
            GRatingFilterButton,
            MovieRatingFilter.G,
            AppStrings.RatingFilterG
        );
        SetRatingFilterButtonState(
            PgRatingFilterButton,
            MovieRatingFilter.PG,
            AppStrings.RatingFilterPG
        );
        SetRatingFilterButtonState(
            RatingSoonFilterButton,
            MovieRatingFilter.RatingSoon,
            AppStrings.RatingComingSoon
        );
    }

    private void SetRatingFilterButtonState(Button button, MovieRatingFilter filter, string label)
    {
        bool isSelected = _viewModel.SelectedRatingFilter == filter;

        // Assign the resolved colors directly. SetDynamicResource cannot convert
        // a Color resource into the Background brush, so it would silently leave
        // whatever the XAML applied and strand a stale selected chip.
        Color surface = GetThemeColor(isSelected ? "Accent" : "Surface2", Colors.Purple);
        button.Background = new SolidColorBrush(surface);
        button.BorderColor = surface;
        button.TextColor = GetThemeColor(
            isSelected ? "PageBackground" : "White",
            isSelected ? Colors.Black : Colors.White
        );
        button.Text = isSelected
            ? string.Format(
                CultureInfo.CurrentCulture,
                AppStrings.SelectedRatingFilterFormat,
                label
            )
            : label;
        SemanticProperties.SetDescription(
            button,
            isSelected
                ? string.Format(
                    CultureInfo.CurrentCulture,
                    AppStrings.SelectedAccessibilityFormat,
                    label,
                    AppStrings.SelectedState
                )
                : label
        );
    }

    private static Color GetThemeColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out object? value) == true
        && value is Color color
            ? color
            : fallback;

    private static string FormatNavigationLabel(string title, string count, bool isSelected)
    {
        string label = string.IsNullOrEmpty(count)
            ? title
            : string.Format(
                CultureInfo.CurrentCulture,
                AppStrings.NavTileAccessibilityFormat,
                title,
                count
            );
        return isSelected
            ? string.Format(
                CultureInfo.CurrentCulture,
                AppStrings.SelectedAccessibilityFormat,
                label,
                AppStrings.SelectedState
            )
            : label;
    }

    private void OnClearSearchClicked(object? sender, EventArgs e)
    {
        _viewModel.Query = string.Empty;
        SearchEntry.Focus();
    }

    private void OnMovieRequested(object? sender, EventArgs e)
    {
        if (
            sender is MovieCardView { Card: MovieCardViewModel card }
            && Interlocked.Exchange(ref _movieNavigationPending, 1) == 0
        )
        {
            _ = OpenMovieAsync(card);
        }
    }

    private async Task OpenMovieAsync(MovieCardViewModel card)
    {
        try
        {
            await _viewModel.OpenDetailAsync(card);
        }
        catch (OperationCanceledException) { }
        finally
        {
            Interlocked.Exchange(ref _movieNavigationPending, 0);
        }
    }

    private readonly record struct FeedScrollAnchor(int MovieId, double OffsetFromItemTop);
}
