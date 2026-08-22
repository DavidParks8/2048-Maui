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

    private async void OnOpenTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await CardRoot.TranslateToAsync(0, 4, 60, Easing.CubicOut);
            OpenRequested?.Invoke(this, EventArgs.Empty);
            await CardRoot.TranslateToAsync(0, 0, 110, Easing.CubicIn);
        }
        catch (TaskCanceledException)
        {
            CardRoot.TranslationY = 0;
        }
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

        CardRoot.TranslationY = 0;
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
