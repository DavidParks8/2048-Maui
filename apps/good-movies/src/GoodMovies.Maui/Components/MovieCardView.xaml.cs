using GoodMovies.Maui.Converters;
using GoodMovies.Maui.Resources.Strings;
using GoodMovies.ViewModels;
using Maui.BindableProperty.Generator.Core;

namespace GoodMovies.Maui.Components;

public partial class MovieCardView : ContentView
{
#pragma warning disable CS0169
    [AutoBindable(OnChanged = nameof(OnCardChanged))]
    private readonly MovieCardViewModel? _card;
#pragma warning restore CS0169

    public event EventHandler? OpenRequested;

    public MovieCardView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    public string FavoriteAccessibilityLabel =>
        Card is MovieCardViewModel card
            ? GoodMoviesTextFormatter.FormatFavoriteAccessibility(card)
            : string.Empty;

    public string FavoriteAccessibilityHint =>
        Card is MovieCardViewModel card
            ? card.IsFavorite
                ? AppStrings.RemoveFavorite
                : AppStrings.AddFavorite
            : AppStrings.AddFavorite;

    private void OnOpenClicked(object? sender, EventArgs e) =>
        OpenRequested?.Invoke(this, EventArgs.Empty);

    private void OnOpenPressed(object? sender, EventArgs e) => CardBorder.TranslationY = 4;

    private void OnOpenReleased(object? sender, EventArgs e) => CardBorder.TranslationY = 0;

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        double posterWidth;
        double posterHeight;
        double spacing;

        if (Width > 0 && Width < 430)
        {
            posterWidth = 88;
            posterHeight = 132;
            spacing = 10;
        }
        else if (Width >= 650)
        {
            posterWidth = 130;
            posterHeight = 195;
            spacing = 16;
        }
        else
        {
            posterWidth = 104;
            posterHeight = 156;
            spacing = 14;
        }

        CardGrid.ColumnDefinitions[0].Width = posterWidth;
        CardGrid.ColumnSpacing = spacing;
        PosterBorder.WidthRequest = posterWidth;
        PosterBorder.HeightRequest = posterHeight;
    }

    private void OnCardChanged(MovieCardViewModel? oldCard, MovieCardViewModel? newCard)
    {
        if (oldCard is not null)
        {
            oldCard.PropertyChanged -= OnCardPropertyChanged;
        }

        if (newCard is not null)
        {
            newCard.PropertyChanged += OnCardPropertyChanged;
        }

        OnPropertyChanged(nameof(FavoriteAccessibilityLabel));
        OnPropertyChanged(nameof(FavoriteAccessibilityHint));
    }

    private void OnCardPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        if (
            e.PropertyName
            is nameof(MovieCardViewModel.IsFavorite)
                or nameof(MovieCardViewModel.Favorite)
        )
        {
            OnPropertyChanged(nameof(FavoriteAccessibilityLabel));
            OnPropertyChanged(nameof(FavoriteAccessibilityHint));
        }
    }
}
