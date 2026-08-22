using GoodMovies.ViewModels;

namespace GoodMovies.Maui;

public partial class MovieDetailPage : ContentPage, IQueryAttributable
{
    private readonly CatalogViewModel _catalogViewModel;
    private MovieDetailViewModel? _boundDetail;
    private int? _requestedMovieId;
    private bool _isWide;
    private bool _layoutInitialized;

    public MovieDetailPage(CatalogViewModel catalogViewModel)
    {
        _catalogViewModel =
            catalogViewModel ?? throw new ArgumentNullException(nameof(catalogViewModel));
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
        _catalogViewModel.CloseDetail();
        _boundDetail = null;
        BindingContext = null;
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

    private void OnWordClicked(object? sender, EventArgs e)
    {
        if (
            sender is Button { BindingContext: WordTokenViewModel token }
            && BindingContext is MovieDetailViewModel detail
        )
        {
            _ = detail.SpeakWordAsync(token);
        }
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
            PosterColumn.WidthRequest = 176;
            PosterColumn.HorizontalOptions = LayoutOptions.Center;
            DetailPosterBorder.WidthRequest = 150;
            DetailPosterBorder.HeightRequest = 225;
        }
    }
}
