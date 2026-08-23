using GoodMovies.Maui.Services;
using GoodMovies.ViewModels;
using UIKit;

namespace GoodMovies.Maui;

public partial class MovieDetailPage : ContentPage, IQueryAttributable
{
    private readonly CatalogViewModel _catalogViewModel;
    private readonly MauiExternalTrailerLauncher _trailerLauncher;
    private MovieDetailViewModel? _boundDetail;
    private int? _requestedMovieId;
    private long _ignoreWordTapsUntilMilliseconds;
    private bool _isAppeared;
    private bool _isPlaybackSubscribed;
    private bool _isWide;
    private bool _layoutInitialized;
    private double _compactPosterWidth;

    public MovieDetailPage(
        CatalogViewModel catalogViewModel,
        MauiExternalTrailerLauncher trailerLauncher
    )
    {
        _catalogViewModel =
            catalogViewModel ?? throw new ArgumentNullException(nameof(catalogViewModel));
        _trailerLauncher =
            trailerLauncher ?? throw new ArgumentNullException(nameof(trailerLauncher));
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("movieId", out object? value))
        {
            _requestedMovieId = value switch
            {
                int movieId => movieId,
                string text when int.TryParse(text, out int movieId) => movieId,
                _ => null,
            };
        }

        BindSelectedDetail();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_isPlaybackSubscribed)
        {
            _trailerLauncher.PlaybackChanged += OnTrailerPlaybackChanged;
            _isPlaybackSubscribed = true;
        }

        _isAppeared = true;
        _ignoreWordTapsUntilMilliseconds = Environment.TickCount64 + 500;
        BindSelectedDetail();
        _boundDetail?.Activate();
        ScheduleSynopsisWords(_boundDetail);
    }

    protected override void OnDisappearing()
    {
        _isAppeared = false;
        _boundDetail?.Deactivate();
        base.OnDisappearing();
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _boundDetail?.Deactivate();
        if (!Navigation.NavigationStack.Contains(this))
        {
            if (_isPlaybackSubscribed)
            {
                _trailerLauncher.PlaybackChanged -= OnTrailerPlaybackChanged;
                _isPlaybackSubscribed = false;
            }
            _catalogViewModel.CloseDetail();
            _boundDetail = null;
            BindingContext = null;
            BindableLayout.SetItemsSource(SynopsisWords, null);
        }

        base.OnNavigatedFrom(args);
    }

    private void BindSelectedDetail()
    {
        MovieDetailViewModel? detail = _catalogViewModel.SelectedMovieDetail;
        if (_requestedMovieId is int requested && detail?.MovieId != requested)
        {
            return;
        }

        if (ReferenceEquals(_boundDetail, detail))
        {
            return;
        }

        _boundDetail?.Deactivate();
        _boundDetail = detail;
        BindableLayout.SetItemsSource(SynopsisWords, null);
        BindingContext = detail;
        if (detail is not null)
        {
            _ = PrepareTrailerAndSyncPlaybackAsync(detail);
            if (_isAppeared)
            {
                ScheduleSynopsisWords(detail);
            }
        }
    }

    private void OnBackClicked(object? sender, EventArgs e) => _ = GoBackAsync();

    private async Task PrepareTrailerAndSyncPlaybackAsync(MovieDetailViewModel detail)
    {
        SyncTrailerPlayback(detail, _trailerLauncher.ActiveYoutubeKey);
        await detail.PrepareTrailerAsync();
        if (ReferenceEquals(_boundDetail, detail))
        {
            SyncTrailerPlayback(detail, _trailerLauncher.ActiveYoutubeKey);
        }
    }

    private void ScheduleSynopsisWords(MovieDetailViewModel? detail)
    {
        if (detail is null)
        {
            return;
        }

        Dispatcher.DispatchDelayed(
            TimeSpan.FromMilliseconds(16),
            () =>
            {
                if (_isAppeared && ReferenceEquals(_boundDetail, detail))
                {
                    BindableLayout.SetItemsSource(SynopsisWords, detail.WordTokens);
                }
            }
        );
    }

    private void OnTrailerPlaybackChanged(object? sender, TrailerPlaybackChangedEventArgs args)
    {
        if (_boundDetail is not { } detail)
        {
            return;
        }

        SyncTrailerPlayback(detail, args.IsPlaying ? args.YouTubeKey : null);
    }

    private static void SyncTrailerPlayback(MovieDetailViewModel detail, string? activeYoutubeKey)
    {
        bool isCurrent =
            activeYoutubeKey is not null
            && string.Equals(detail.SelectedTrailerKey, activeYoutubeKey, StringComparison.Ordinal);
        detail.SetTrailerPlaybackContext(
            isCurrentTrailer: isCurrent,
            isAnotherTrailerPlaying: activeYoutubeKey is not null && !isCurrent
        );
    }

    private async Task GoBackAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception)
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
        }
    }

    private void OnWordTapped(object? sender, TappedEventArgs e)
    {
        if (Environment.TickCount64 < _ignoreWordTapsUntilMilliseconds)
        {
            return;
        }

        if (
            sender is Border { BindingContext: WordTokenViewModel token }
            && BindingContext is MovieDetailViewModel detail
        )
        {
            _ = detail.SpeakWordAsync(token);
        }
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        bool isWide = Width >= 900 && Width > Height;
        double compactPosterWidth = Width >= 600 ? 230 : 150;
        if (_layoutInitialized && isWide == _isWide && compactPosterWidth == _compactPosterWidth)
        {
            return;
        }

        bool isInitialLayout = !_layoutInitialized;
        _layoutInitialized = true;
        _isWide = isWide;
        _compactPosterWidth = compactPosterWidth;

        if (
            !isInitialLayout
            && Handler is IPlatformViewHandler { ViewController: { View: { } view } }
        )
        {
            UIView.PerformWithoutAnimation(() =>
            {
                ApplyLayout(isWide, compactPosterWidth);
                view.SetNeedsLayout();
                view.LayoutIfNeeded();
            });
            return;
        }

        ApplyLayout(isWide, compactPosterWidth);
    }

    private void ApplyLayout(bool isWide, double compactPosterWidth)
    {
        if (isWide)
        {
            DetailContent.ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(250),
                new(GridLength.Star),
            };
            DetailContent.RowDefinitions = new RowDefinitionCollection { new(GridLength.Auto) };
            Grid.SetColumn(PosterColumn, 0);
            Grid.SetRow(PosterColumn, 0);
            Grid.SetColumn(DetailInfo, 1);
            Grid.SetRow(DetailInfo, 0);
            PosterColumn.WidthRequest = 250;
            PosterColumn.HorizontalOptions = LayoutOptions.Start;
            DetailPosterBorder.WidthRequest = 250;
            DetailPosterBorder.HeightRequest = 375;
        }
        else
        {
            DetailContent.ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star),
            };
            DetailContent.RowDefinitions = new RowDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Auto),
            };
            Grid.SetColumn(PosterColumn, 0);
            Grid.SetRow(PosterColumn, 0);
            Grid.SetColumn(DetailInfo, 0);
            Grid.SetRow(DetailInfo, 1);
            PosterColumn.WidthRequest = compactPosterWidth + 90;
            PosterColumn.HorizontalOptions = LayoutOptions.Center;
            DetailPosterBorder.WidthRequest = compactPosterWidth;
            DetailPosterBorder.HeightRequest = compactPosterWidth * 1.5;
        }
    }
}
