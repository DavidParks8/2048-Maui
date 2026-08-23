using GoodMovies.Maui.Services;

namespace GoodMovies.Maui;

public partial class AppShell : Shell
{
    public AppShell(MainPage mainPage, IMovieDetailPageFactory detailPageFactory)
    {
        InitializeComponent();
        MainShellContent.Content = mainPage ?? throw new ArgumentNullException(nameof(mainPage));
        Routing.RegisterRoute(
            GoodMoviesRoutes.MovieDetail,
            new MauiMovieDetailRouteFactory(detailPageFactory)
        );
    }
}
