using GoodMovies.Maui.Services;
using GoodMovies.ViewModels;
using UIKit;

namespace GoodMovies.Maui;

public partial class MovieDetailPage : ContentPage, IQueryAttributable, ITrailerPlaybackHost
{
    private readonly CatalogViewModel _catalogViewModel;
    private readonly MauiExternalTrailerLauncher _trailerLauncher;
    private MovieDetailViewModel? _boundDetail;
    private TaskCompletionSource<bool>? _trailerLoadCompletion;
    private int? _requestedMovieId;
    private bool _isTrailerActive;
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
        _trailerLauncher.AttachHost(this);
        BindSelectedDetail();
        _boundDetail?.Activate();
    }

    protected override void OnDisappearing()
    {
        _boundDetail?.Deactivate();
        base.OnDisappearing();
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _boundDetail?.Deactivate();
        if (!Navigation.NavigationStack.Contains(this))
        {
            _trailerLauncher.DetachHost(this);
            _catalogViewModel.CloseDetail();
            _boundDetail = null;
            BindingContext = null;
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
        BindingContext = detail;
        if (detail is not null)
        {
            _ = detail.PrepareTrailerAsync();
        }
    }

    private void OnBackClicked(object? sender, EventArgs e) => _ = GoBackAsync();

    public async Task<bool> PlayAsync(string youtubeKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!YouTubeTrailerUri.TryCreate(youtubeKey, out Uri source))
        {
            return false;
        }

        if (_isTrailerActive)
        {
            return true;
        }

        TaskCompletionSource<bool> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _trailerLoadCompletion?.TrySetResult(false);
        _trailerLoadCompletion = completion;

        if (Equals(TrailerWebView.Source, source))
        {
            TrailerWebView.Reload();
        }
        else
        {
            TrailerWebView.Source = source;
        }

        try
        {
            return await completion.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (ReferenceEquals(_trailerLoadCompletion, completion))
            {
                _trailerLoadCompletion = null;
                Stop();
            }

            throw;
        }
    }

    public void Stop()
    {
        _isTrailerActive = false;
        _boundDetail?.SetTrailerPlaybackActive(false);
        _trailerLoadCompletion?.TrySetResult(false);
        _trailerLoadCompletion = null;
        TrailerWebView.StopPlayback();
    }

    private void OnTrailerLoadStarted(object? sender, EventArgs e) { }

    private void OnTrailerLoadSucceeded(object? sender, EventArgs e)
    {
        _isTrailerActive = true;
        _boundDetail?.SetTrailerPlaybackActive(true);
        _trailerLoadCompletion?.TrySetResult(true);
        _trailerLoadCompletion = null;
    }

    private void OnTrailerLoadFailed(object? sender, EventArgs e)
    {
        _isTrailerActive = false;
        _boundDetail?.SetTrailerPlaybackActive(false);
        _trailerLoadCompletion?.TrySetResult(false);
        _trailerLoadCompletion = null;
        TrailerWebView.StopPlayback();
    }

    private void OnTrailerPresentationEnded(object? sender, EventArgs e) => Stop();

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

        _layoutInitialized = true;
        _isWide = isWide;
        _compactPosterWidth = compactPosterWidth;

        if (Handler is IPlatformViewHandler { ViewController: { View: { } view } })
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
