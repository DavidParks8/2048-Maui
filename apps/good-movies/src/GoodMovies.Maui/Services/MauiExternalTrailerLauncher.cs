using GoodMovies.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace GoodMovies.Maui.Services;

public interface ITrailerPlaybackController
{
    void Stop();
}

public interface ITrailerPlayerPageFactory
{
    TrailerPlayerPage Create();
}

public sealed class MauiTrailerPlayerRouteFactory(ITrailerPlayerPageFactory pageFactory)
    : RouteFactory
{
    private readonly ITrailerPlayerPageFactory _pageFactory =
        pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));

    public override Element GetOrCreate() => _pageFactory.Create();

    public override Element GetOrCreate(IServiceProvider services) => _pageFactory.Create();
}

public class MauiExternalTrailerLauncher
    : IExternalTrailerLauncher,
        ITrailerLauncher,
        IYouTubeTrailerLauncher,
        IExternalLinkLauncher,
        IExternalLauncher,
        IExternalTrailerService,
        ITrailerPlaybackController,
        ITrailerPlayerPageFactory
{
    private readonly ILogger<MauiExternalTrailerLauncher> _logger;
    private readonly IScreenReaderService _screenReaderService;
    private readonly object _sync = new();
    private TrailerPlayerPage? _activePage;

    public MauiExternalTrailerLauncher(
        ILogger<MauiExternalTrailerLauncher> logger,
        IScreenReaderService screenReaderService
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _screenReaderService =
            screenReaderService ?? throw new ArgumentNullException(nameof(screenReaderService));
    }

    public async Task<bool> LaunchAsync(
        string youtubeKey,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!YouTubeTrailerUri.TryCreate(youtubeKey, out _))
        {
            return false;
        }

        Task<bool> presentation = MainThread.InvokeOnMainThreadAsync(() =>
            PresentAsync(youtubeKey, cancellationToken)
        );
        return await presentation.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Stop()
    {
        TrailerPlayerPage? page;
        lock (_sync)
        {
            page = _activePage;
        }

        if (page is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => page.CloseForLifecycle());
    }

    public TrailerPlayerPage Create()
    {
        TrailerPlayerPage page = new(_logger, _screenReaderService);
        page.Closed += OnPlayerClosed;
        lock (_sync)
        {
            _activePage = page;
        }

        return page;
    }

    private async Task<bool> PresentAsync(string youtubeKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (_activePage is not null)
            {
                return true;
            }
        }

        Shell shell =
            Shell.Current
            ?? throw new InvalidOperationException("The Good Movies Shell is not available.");
        ShellNavigationQueryParameters parameters = new()
        {
            [TrailerPlayerPage.YouTubeKeyQueryParameter] = youtubeKey,
        };
        await shell.GoToAsync(GoodMoviesRoutes.TrailerPlayer, parameters);
        return true;
    }

    private void OnPlayerClosed(object? sender, EventArgs e)
    {
        if (sender is not TrailerPlayerPage page)
        {
            return;
        }

        page.Closed -= OnPlayerClosed;
        lock (_sync)
        {
            if (ReferenceEquals(_activePage, page))
            {
                _activePage = null;
            }
        }
    }
}
