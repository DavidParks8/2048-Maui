using System.Globalization;
using GoodMovies.Maui;
using GoodMovies.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui.Services;

public static class GoodMoviesRoutes
{
    public const string MovieDetail = "movie-detail";
}

/// <summary>
/// UI-owned seam for creating a detail page. The Shell route factory uses this
/// explicit factory rather than resolving pages from a ViewModel.
/// </summary>
public interface IMovieDetailPageFactory
{
    MovieDetailPage Create();
}

public sealed class MauiMovieDetailPageFactory : IMovieDetailPageFactory
{
    private readonly MovieDetailPage _page;

    public MauiMovieDetailPageFactory(
        CatalogViewModel catalogViewModel,
        MauiExternalTrailerLauncher trailerLauncher
    )
    {
        ArgumentNullException.ThrowIfNull(catalogViewModel);
        ArgumentNullException.ThrowIfNull(trailerLauncher);
        _page = new MovieDetailPage(catalogViewModel, trailerLauncher);
    }

    public MovieDetailPage Create() => _page;
}

public sealed class MauiMovieDetailRouteFactory : RouteFactory
{
    private readonly IMovieDetailPageFactory _pageFactory;

    public MauiMovieDetailRouteFactory(IMovieDetailPageFactory pageFactory)
    {
        _pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
    }

    public override Element GetOrCreate() => _pageFactory.Create();

    public override Element GetOrCreate(IServiceProvider services) => _pageFactory.Create();
}

public interface IMovieDetailNavigationHost
{
    Task NavigateToMovieDetailAsync(int movieId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Navigates through Shell so the native iOS back button and edge-swipe remain
/// available. CatalogViewModel has already selected the detail before this call.
/// </summary>
public sealed class MauiMovieDetailNavigationHost : IMovieDetailNavigationHost
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
        ShellNavigationQueryParameters parameters = new()
        {
            ["movieId"] = movieId.ToString(CultureInfo.InvariantCulture),
        };
        Task navigation = MainThread.InvokeOnMainThreadAsync(() =>
            shell.GoToAsync(GoodMoviesRoutes.MovieDetail, animate: false, parameters)
        );
        return navigation.WaitAsync(cancellationToken);
    }
}

public sealed class MauiNavigationService : INavigationService, IMovieNavigationService
{
    private readonly IMovieDetailNavigationHost _navigationHost;

    public MauiNavigationService(IMovieDetailNavigationHost navigationHost)
    {
        _navigationHost = navigationHost ?? throw new ArgumentNullException(nameof(navigationHost));
    }

    public Task NavigateToMovieDetailAsync(
        int movieId,
        CancellationToken cancellationToken = default
    ) => _navigationHost.NavigateToMovieDetailAsync(movieId, cancellationToken);

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
