using System.ComponentModel;
using System.Globalization;
using GoodMovies.Maui.Components;
using GoodMovies.Maui.Converters;
using GoodMovies.Maui.Resources.Strings;
using GoodMovies.Maui.Services;
using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui;

public partial class MainPage : ContentPage
{
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
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        SizeChanged += OnSizeChanged;
        UpdateNavigationState();
    }

    public NavigationViewModel NavigationViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isAppeared = true;
        UpdateNavigationState();
        if (!_hasStartedInitialization)
        {
            _hasStartedInitialization = true;
            _ = InitializeCatalogAsync();
        }
    }

    protected override void OnDisappearing()
    {
        _isAppeared = false;
        base.OnDisappearing();
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
        _movieItemsLayout.Span = isWide ? 2 : 1;
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
        if (sender is MovieCardView { Card: MovieCardViewModel card })
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
    }
}
