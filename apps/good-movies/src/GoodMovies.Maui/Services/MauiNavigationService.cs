using GoodMovies.Maui;
using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui.Services;

internal static class GoodMoviesRoutes
{
    public const string MovieDetail = "movie-detail";
}

internal sealed class MauiMovieDetailRouteFactory : RouteFactory
{
    private readonly MovieDetailPage _page;

    public MauiMovieDetailRouteFactory(MovieDetailPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
    }

    public override Element GetOrCreate() => _page;

    public override Element GetOrCreate(IServiceProvider services) => _page;
}

/// <summary>
/// Navigates through Shell so the native iOS back button and edge-swipe remain
/// available. CatalogViewModel has already selected the detail before this call.
/// </summary>
public sealed class MauiNavigationService : INavigationService
{
    public Task NavigateToMovieDetailAsync(
        int movieId,
        CancellationToken cancellationToken = default
    )
    {
        if (movieId <= 0)
        {
            return Task.FromException(
                new ArgumentOutOfRangeException(nameof(movieId), "A movie ID must be positive.")
            );
        }

        cancellationToken.ThrowIfCancellationRequested();
        Shell shell =
            Shell.Current
            ?? throw new InvalidOperationException("The Good Movies Shell is not available.");
        ShellNavigationQueryParameters parameters = new() { ["movieId"] = movieId };
        Task navigation = MainThread.InvokeOnMainThreadAsync(() =>
            shell.GoToAsync(GoodMoviesRoutes.MovieDetail, animate: false, parameters)
        );
        return navigation.WaitAsync(cancellationToken);
    }

    public Task NavigateBackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Shell shell =
            Shell.Current
            ?? throw new InvalidOperationException("The Good Movies Shell is not available.");
        Task navigation = MainThread.InvokeOnMainThreadAsync(() => shell.GoToAsync(".."));
        return navigation.WaitAsync(cancellationToken);
    }
}
