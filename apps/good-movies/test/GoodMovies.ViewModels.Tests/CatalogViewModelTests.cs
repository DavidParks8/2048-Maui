using System.Collections.Concurrent;
using GoodMovies.Core;
using GoodMovies.ViewModels;
using NSubstitute;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class CatalogViewModelTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    public async Task Initialize_StaleCacheIsPublishedBeforeRefreshCompletes()
    {
        Movie cached = MovieWithRelease(1, "Cached", Today.AddDays(2));
        Movie refreshed = MovieWithRelease(2, "Fresh", Today.AddDays(4));
        TaskCompletionSource<CatalogResult> refresh = Pending<CatalogResult>();
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.StaleCache, cached)));
        service.RefreshAsync(Arg.Any<CancellationToken>()).Returns(refresh.Task);

        CatalogViewModel viewModel = CreateViewModel(service);
        Task initialization = viewModel.InitializeAsync();

        await Eventually(() => viewModel.MovieCards.Count == 1);
        Assert.AreEqual("Cached", viewModel.MovieCards[0].Title);
        Assert.IsTrue(viewModel.IsStale);
        Assert.IsTrue(viewModel.IsRefreshing);
        Assert.IsFalse(initialization.IsCompleted);

        refresh.SetResult(Refreshed(refreshed));
        await initialization;

        Assert.AreEqual(1, viewModel.MovieCards.Count);
        Assert.AreEqual("Fresh", viewModel.MovieCards[0].Title);
    }

    [TestMethod]
    public async Task Initialize_FreshCacheDoesNotRefresh_AndNoCacheDoesRefresh()
    {
        IMovieCatalogService freshService = Substitute.For<IMovieCatalogService>();
        freshService
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    Cache(CatalogResultStatus.FreshCache, MovieWithRelease(1, "Fresh", Today))
                )
            );
        CatalogViewModel fresh = CreateViewModel(freshService);

        await fresh.InitializeAsync();

        await freshService.DidNotReceive().RefreshAsync(Arg.Any<CancellationToken>());
        Assert.AreEqual("Fresh", fresh.MovieCards[0].Title);

        IMovieCatalogService emptyService = Substitute.For<IMovieCatalogService>();
        emptyService
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CatalogResult(CatalogResultStatus.NoCache)));
        emptyService
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Refreshed(MovieWithRelease(2, "Remote", Today))));
        CatalogViewModel empty = CreateViewModel(emptyService);

        await empty.InitializeAsync();

        await emptyService.Received(1).RefreshAsync(Arg.Any<CancellationToken>());
        Assert.AreEqual("Remote", empty.MovieCards[0].Title);
    }

    [TestMethod]
    public async Task Initialize_ReconcilesFavoritesAgainstTheCachedCatalog()
    {
        Movie cached = MovieWithRelease(1, "Cached", Today.AddDays(1));
        FavoriteEntry present = cached.CreateFavoriteEntry()!.Value;
        FavoriteEntry absent = new(99, Today.AddDays(2));
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        favorites
            .GetAsync(Today, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(FavoritesResult.Success(new[] { present, absent })));
        favorites
            .PruneAsync(Today, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(FavoritesResult.Success(new[] { present, absent })));
        favorites
            .ReconcileAsync(Arg.Any<IEnumerable<Movie>>(), Today, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(FavoritesResult.Success(new[] { present })));
        CatalogViewModel viewModel = CreateViewModel(FreshService(cached), favorites);

        await viewModel.InitializeAsync();

        Assert.AreEqual(1, viewModel.FavoriteCount);
        await favorites
            .Received(1)
            .ReconcileAsync(
                Arg.Is<IEnumerable<Movie>>(movies => movies.Single().Id == cached.Id),
                Today,
                Arg.Any<CancellationToken>()
            );
    }

    [TestMethod]
    public async Task CheckForUpdates_FreshCacheUsesServiceSemanticsWithoutForcingRefresh()
    {
        Movie movie = MovieWithRelease(1, "Fresh", Today);
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .GetCatalogAsync(false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, movie)));
        CatalogViewModel viewModel = CreateViewModel(service);

        CatalogResult result = await viewModel.CheckForUpdatesAsync();

        Assert.AreEqual(CatalogResultStatus.FreshCache, result.Status);
        Assert.AreEqual("Fresh", viewModel.MovieCards.Single().Title);
        await service.Received(1).GetCatalogAsync(false, Arg.Any<CancellationToken>());
        await service.DidNotReceive().RefreshAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CheckForUpdates_PrunesFavoritesForTheNewLocalDate()
    {
        Movie movie = MovieWithRelease(1, "Fresh", Today);
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, movie)));
        service
            .GetCatalogAsync(false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, movie)));
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        FavoriteEntry entry = movie.CreateFavoriteEntry()!.Value;
        favorites
            .GetAsync(Today, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(FavoritesResult.Success(new[] { entry })));
        favorites
            .PruneAsync(Today, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(FavoritesResult.Success(new[] { entry })),
                Task.FromResult(FavoritesResult.Success(Array.Empty<FavoriteEntry>()))
            );
        CatalogViewModel viewModel = CreateViewModel(service, favorites);
        await viewModel.InitializeAsync();
        Assert.AreEqual(1, viewModel.FavoriteCount);

        await viewModel.CheckForUpdatesAsync();

        await favorites.Received(2).PruneAsync(Today, Arg.Any<CancellationToken>());
        Assert.AreEqual(0, viewModel.FavoriteCount);
    }

    [TestMethod]
    public async Task ReapplyCurrentDatePolicies_RemovesDayFourteenMovieAndFavoriteOffline()
    {
        Movie expiring = MovieWithRelease(1, "Last day", Today.AddDays(-13));
        FavoriteEntry favorite = expiring.CreateFavoriteEntry()!.Value;
        MutableClock clock = new(Today);
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, expiring)));
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        favorites
            .GetAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
                Task.FromResult(
                    FavoritesResult.Success(
                        callInfo.Arg<DateOnly>() == Today
                            ? new[] { favorite }
                            : Array.Empty<FavoriteEntry>()
                    )
                )
            );
        favorites
            .PruneAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
                Task.FromResult(
                    FavoritesResult.Success(
                        callInfo.Arg<DateOnly>() == Today
                            ? new[] { favorite }
                            : Array.Empty<FavoriteEntry>()
                    )
                )
            );
        INavigationService navigation = Substitute.For<INavigationService>();
        navigation
            .NavigateToMovieDetailAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        navigation.NavigateBackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        CatalogViewModel viewModel = new(service, favorites, clock, navigationService: navigation);
        await viewModel.InitializeAsync();
        Assert.AreEqual(1, viewModel.MovieCards.Count);
        Assert.AreEqual(1, viewModel.FavoriteCount);
        await viewModel.OpenDetailAsync(viewModel.MovieCards.Single());
        Assert.IsNotNull(viewModel.SelectedMovieDetail);

        clock.Today = Today.AddDays(1);
        await viewModel.ReapplyCurrentDatePoliciesAsync();

        Assert.AreEqual(0, viewModel.MovieCards.Count);
        Assert.AreEqual(0, viewModel.FavoriteCount);
        await favorites.Received().PruneAsync(Today.AddDays(1), Arg.Any<CancellationToken>());
        Assert.IsNull(viewModel.SelectedMovieDetail);
        await navigation.Received(1).NavigateBackAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CheckForUpdates_UsesServiceRefreshForStaleOrMissingData()
    {
        Movie movie = MovieWithRelease(1, "Remote", Today);
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .GetCatalogAsync(false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Refreshed(movie)));
        CatalogViewModel viewModel = CreateViewModel(service);

        CatalogResult result = await viewModel.CheckForUpdatesAsync();

        Assert.AreEqual(CatalogResultStatus.Refreshed, result.Status);
        Assert.AreEqual("Remote", viewModel.MovieCards.Single().Title);
        await service.Received(1).GetCatalogAsync(false, Arg.Any<CancellationToken>());
        await service.DidNotReceive().RefreshAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CheckForUpdates_OlderResumeResultCannotOverwriteNewerRefresh()
    {
        TaskCompletionSource<CatalogResult> pendingResume = Pending<CatalogResult>();
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service.GetCatalogAsync(false, Arg.Any<CancellationToken>()).Returns(pendingResume.Task);
        service
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Refreshed(MovieWithRelease(2, "New", Today))));
        CatalogViewModel viewModel = CreateViewModel(service);

        Task<CatalogResult> resume = viewModel.CheckForUpdatesAsync();
        await viewModel.RefreshAsync();
        Assert.AreEqual("New", viewModel.MovieCards.Single().Title);

        pendingResume.SetResult(
            Cache(CatalogResultStatus.FreshCache, MovieWithRelease(1, "Old", Today))
        );
        await resume;

        Assert.AreEqual("New", viewModel.MovieCards.Single().Title);
    }

    [TestMethod]
    public async Task ResumeCheck_ReappliesTodayStatusToAnOpenDetail()
    {
        Movie movie = MovieWithRelease(1, "Tomorrow", Today.AddDays(1));
        MutableClock clock = new(Today);
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, movie)));
        service
            .GetCatalogAsync(false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, movie)));
        INavigationService navigation = Substitute.For<INavigationService>();
        navigation
            .NavigateToMovieDetailAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        IFavoritesStore? noFavorites = null;
        CatalogViewModel viewModel = new(
            service,
            noFavorites,
            clock,
            navigationService: navigation
        );
        await viewModel.InitializeAsync();
        await viewModel.OpenDetailAsync(viewModel.MovieCards.Single());
        Assert.AreEqual(ReleaseStatus.Future, viewModel.SelectedMovieDetail!.Status);

        clock.Today = Today.AddDays(1);
        await viewModel.ResumeAsync();

        Assert.AreEqual(ReleaseStatus.Today, viewModel.SelectedMovieDetail!.Status);
        clock.Today = Today.AddDays(2);
        await viewModel.OnResumeAsync();
        Assert.AreEqual(ReleaseStatus.InTheatersNow, viewModel.SelectedMovieDetail!.Status);
        await service.Received(2).GetCatalogAsync(false, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ResumeCheck_RemovesExpiredContentBeforePendingNetworkRefresh()
    {
        Movie expiring = MovieWithRelease(1, "Expired now", Today.AddDays(-13));
        MutableClock clock = new(Today);
        TaskCompletionSource<CatalogResult> refresh = Pending<CatalogResult>();
        TaskCompletionSource refreshStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, expiring)));
        service
            .GetCatalogAsync(false, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                refreshStarted.TrySetResult();
                return refresh.Task;
            });
        INavigationService navigation = Substitute.For<INavigationService>();
        navigation
            .NavigateToMovieDetailAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        navigation.NavigateBackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        IFavoritesStore? noFavorites = null;
        CatalogViewModel viewModel = new(
            service,
            noFavorites,
            clock,
            navigationService: navigation
        );
        await viewModel.InitializeAsync();
        await viewModel.OpenDetailAsync(viewModel.MovieCards.Single());

        clock.Today = Today.AddDays(1);
        Task<CatalogResult> update = viewModel.CheckForUpdatesAndReapplyDateAsync();
        await refreshStarted.Task;

        Assert.IsFalse(update.IsCompleted);
        Assert.AreEqual(0, viewModel.MovieCards.Count);
        Assert.IsNull(viewModel.SelectedMovieDetail);
        refresh.SetResult(new CatalogResult(CatalogResultStatus.FreshCache));
        await update;
    }

    [TestMethod]
    public async Task ReapplyCurrentDatePolicies_KeepsExpiredDetailUntilBackNavigationSucceeds()
    {
        Movie expiring = MovieWithRelease(1, "Last day", Today.AddDays(-13));
        MutableClock clock = new(Today);
        IMovieCatalogService service = FreshService(expiring);
        INavigationService navigation = Substitute.For<INavigationService>();
        navigation
            .NavigateToMovieDetailAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        navigation
            .NavigateBackAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("navigation failed")));
        IFavoritesStore? noFavorites = null;
        CatalogViewModel viewModel = new(
            service,
            noFavorites,
            clock,
            navigationService: navigation
        );
        await viewModel.InitializeAsync();
        await viewModel.OpenDetailAsync(viewModel.MovieCards.Single());

        clock.Today = Today.AddDays(1);
        await Assert.ThrowsAsync<IOException>(async () =>
            await viewModel.ReapplyCurrentDatePoliciesAsync()
        );

        Assert.IsNotNull(viewModel.SelectedMovieDetail);
        navigation.NavigateBackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        await viewModel.ReapplyCurrentDatePoliciesAsync();
        Assert.IsNull(viewModel.SelectedMovieDetail);
    }

    [TestMethod]
    public void YouTubeTrailerUri_ValidatesConservativeVideoKeys()
    {
        Assert.IsTrue(YouTubeTrailerUri.IsValidKey("dQw4w9WgXcQ"));
        Assert.IsFalse(YouTubeTrailerUri.IsValidKey(null));
        Assert.IsFalse(YouTubeTrailerUri.IsValidKey("short"));
        Assert.IsFalse(YouTubeTrailerUri.IsValidKey("dQw4w9WgXcQ!"));
        Assert.IsFalse(YouTubeTrailerUri.IsValidKey("dQw4w9WgXc Q"));

        Assert.IsTrue(YouTubeTrailerUri.TryCreate("dQw4w9WgXcQ", out Uri uri));
        Assert.AreEqual("youtube://www.youtube.com/watch?v=dQw4w9WgXcQ", uri.AbsoluteUri);
        Assert.AreEqual(YouTubeTrailerUri.Scheme, uri.Scheme);
        Assert.IsNull(YouTubeTrailerUri.Build("not-a-key"));
    }

    [TestMethod]
    public async Task Refresh_SuccessReplacesCatalogOrdersGroupsAndReconcilesFavorites()
    {
        Movie later = MovieWithRelease(2, "Zulu", Today.AddDays(3));
        Movie earlier = MovieWithRelease(1, "Alpha", Today.AddDays(1));
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        favorites
            .ReconcileAsync(Arg.Any<IEnumerable<Movie>>(), Today, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    FavoritesResult.Success(new[] { earlier.CreateFavoriteEntry()!.Value })
                )
            );
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CatalogResult(CatalogResultStatus.NoCache)));
        service
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Refreshed(later, earlier)));
        CatalogViewModel viewModel = CreateViewModel(service, favorites);

        await viewModel.InitializeAsync();

        Assert.AreEqual(2, viewModel.MovieCards.Count);
        Assert.AreEqual("Alpha", viewModel.MovieCards[0].Title);
        Assert.AreEqual(2, viewModel.MovieGroups.Count);
        Assert.AreEqual(Today.AddDays(1), viewModel.MovieGroups[0].ReleaseDate);
        Assert.IsTrue(viewModel.MovieCards.Single(card => card.MovieId == 1).IsFavorite);
        await favorites
            .Received(1)
            .ReconcileAsync(
                Arg.Is<IEnumerable<Movie>>(movies => movies.Count() == 2),
                Today,
                Arg.Any<CancellationToken>()
            );
    }

    [TestMethod]
    public async Task Refresh_FailureRetainsCacheAndWarns_WhileNoCacheFailureBlocks()
    {
        Movie cached = MovieWithRelease(1, "Cached", Today);
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, cached)));
        service
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new CatalogResult(
                        CatalogResultStatus.RefreshFailed,
                        new[] { MovieWithRelease(2, "Bad payload", Today) },
                        usedCache: true,
                        isStale: true,
                        error: new InvalidOperationException("offline")
                    )
                )
            );
        CatalogViewModel viewModel = CreateViewModel(service);
        await viewModel.InitializeAsync();

        await viewModel.RefreshAsync();

        Assert.AreEqual("Cached", viewModel.MovieCards.Single().Title);
        Assert.IsTrue(viewModel.IsWarning);
        Assert.IsTrue(viewModel.IsStale);
        Assert.IsFalse(viewModel.IsError);
        Assert.AreEqual(CatalogMessageKey.RefreshWarning, viewModel.MessageKey);

        IMovieCatalogService noCacheService = Substitute.For<IMovieCatalogService>();
        noCacheService
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CatalogResult(CatalogResultStatus.NoCache)));
        noCacheService
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new CatalogResult(
                        CatalogResultStatus.MissingConfiguration,
                        error: new InvalidOperationException("missing token")
                    )
                )
            );
        CatalogViewModel noCache = CreateViewModel(noCacheService);

        await noCache.InitializeAsync();

        Assert.IsTrue(noCache.IsError);
        Assert.IsTrue(noCache.IsMissingToken);
        Assert.AreEqual(CatalogViewState.MissingToken, noCache.State);
        Assert.AreEqual(CatalogMessageKey.MissingToken, noCache.MessageKey);
    }

    [TestMethod]
    public async Task StaleWarning_DoesNotHideSearchAndFavoritesEmptyStates()
    {
        Movie cached = MovieWithRelease(1, "Cached", Today);
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, cached)));
        service
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new CatalogResult(
                        CatalogResultStatus.RefreshFailed,
                        new[] { cached },
                        usedCache: true,
                        isStale: true,
                        error: new IOException("offline")
                    )
                )
            );
        CatalogViewModel viewModel = CreateViewModel(service);
        await viewModel.InitializeAsync();
        await viewModel.RefreshAsync();

        await viewModel.SwitchSectionAsync(CatalogSection.FindAMovie);

        Assert.IsTrue(viewModel.IsWarning);
        Assert.AreEqual(CatalogViewState.SearchPrompt, viewModel.State);
        Assert.AreEqual(CatalogMessageKey.SearchPrompt, viewModel.MessageKey);

        await viewModel.SwitchSectionAsync(CatalogSection.MyFavorites);

        Assert.IsTrue(viewModel.IsWarning);
        Assert.AreEqual(CatalogViewState.Empty, viewModel.State);
        Assert.AreEqual(CatalogMessageKey.NoFavorites, viewModel.MessageKey);
    }

    [TestMethod]
    public async Task RefreshFailure_UsesObservedConnectivityForOfflineMessages()
    {
        Movie cached = MovieWithRelease(1, "Cached", Today);
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, cached)));
        service
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new CatalogResult(
                        CatalogResultStatus.RefreshFailed,
                        new[] { cached },
                        usedCache: true,
                        isStale: true,
                        error: new HttpRequestException("offline")
                    )
                )
            );
        INetworkStatusService network = Substitute.For<INetworkStatusService>();
        network.IsInternetAvailable.Returns(false);
        IFavoritesStore? noFavorites = null;
        CatalogViewModel viewModel = new(
            service,
            noFavorites,
            new FixedClock(Today),
            networkStatusService: network
        );
        await viewModel.InitializeAsync();

        await viewModel.RefreshAsync();

        Assert.AreEqual(CatalogMessageKey.OfflineWarning, viewModel.WarningKey);
        network.IsInternetAvailable.Returns(true);
        network.NetworkStatusChanged += Raise.Event<EventHandler>(network, EventArgs.Empty);
        Assert.AreEqual(CatalogMessageKey.RefreshWarning, viewModel.WarningKey);
    }

    [TestMethod]
    public async Task Catalog_DefenseInDepthRemovesUnsafeAndExpiredMovies()
    {
        Movie safe = MovieWithRelease(1, "Safe", Today);
        Movie expired = MovieWithRelease(2, "Expired", Today.AddDays(-14));
        Movie unsafeMovie = new(
            3,
            "Unsafe",
            "PG-13",
            new TheatricalRelease(Today, "US", TheatricalRelease.TheatricalType)
        );
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Refreshed(safe, expired, unsafeMovie)));
        CatalogViewModel viewModel = CreateViewModel(service);

        await viewModel.InitializeAsync();

        CollectionAssert.AreEqual(
            new[] { 1 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );
    }

    [TestMethod]
    public async Task Refresh_DuplicateCallsShareOneOperation()
    {
        TaskCompletionSource<CatalogResult> pending = Pending<CatalogResult>();
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service.RefreshAsync(Arg.Any<CancellationToken>()).Returns(pending.Task);
        CatalogViewModel viewModel = CreateViewModel(service);

        Task first = viewModel.RefreshAsync();
        Task second = viewModel.RefreshAsync();

        Assert.AreSame(first, second);
        await service.Received(1).RefreshAsync(Arg.Any<CancellationToken>());
        pending.SetResult(Refreshed(MovieWithRelease(1, "One", Today)));
        await first;
    }

    [TestMethod]
    public async Task Initialize_DoesNotLetAnOlderCacheReadOverwriteANewerRefresh()
    {
        TaskCompletionSource<CatalogResult> load = Pending<CatalogResult>();
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service.LoadAsync(Arg.Any<CancellationToken>()).Returns(load.Task);
        service
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Refreshed(MovieWithRelease(2, "New", Today))));
        CatalogViewModel viewModel = CreateViewModel(service);

        Task initialization = viewModel.InitializeAsync();
        await viewModel.RefreshAsync();
        Assert.AreEqual("New", viewModel.MovieCards.Single().Title);

        load.SetResult(Cache(CatalogResultStatus.StaleCache, MovieWithRelease(1, "Old", Today)));
        await initialization;

        Assert.AreEqual("New", viewModel.MovieCards.Single().Title);
    }

    [TestMethod]
    public async Task Refresh_CancellationLeavesNoBusyState()
    {
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        using CancellationTokenSource cancellation = new();
        service
            .RefreshAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => WaitForCancellation(callInfo.Arg<CancellationToken>()));
        CatalogViewModel viewModel = CreateViewModel(service);

        Task<CatalogResult> refresh = viewModel.RefreshAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await refresh);
        Assert.IsFalse(viewModel.IsRefreshing);
    }

    [TestMethod]
    public async Task Sections_CountsAndSearchRemainLocalAndDebounced()
    {
        Movie dragon = MovieWithGenre(1, "Dragon Ride", Today.AddDays(1), "Fantasy");
        Movie space = MovieWithGenre(2, "Moon Trip", Today.AddDays(2), "Space");
        Movie comedy = MovieWithGenre(3, "Funny Day", Today.AddDays(3), "Comedy");
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, dragon, space, comedy)));
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        favorites
            .GetAsync(Today, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    FavoritesResult.Success(new[] { space.CreateFavoriteEntry()!.Value })
                )
            );
        favorites
            .PruneAsync(Today, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    FavoritesResult.Success(new[] { space.CreateFavoriteEntry()!.Value })
                )
            );
        CatalogViewModel viewModel = CreateViewModel(
            service,
            favorites,
            searchDebounce: TimeSpan.FromMilliseconds(15)
        );
        await viewModel.InitializeAsync();

        Assert.AreEqual(3, viewModel.ComingSoonCount);
        Assert.AreEqual(1, viewModel.FavoriteCount);
        viewModel.Query = "dragon";
        Assert.AreEqual(3, viewModel.ComingSoonCount);
        Assert.AreEqual(3, viewModel.MovieCards.Count);

        await viewModel.SwitchSectionAsync(CatalogSection.FindAMovie);
        await viewModel.SearchDebounceTask;
        Assert.IsTrue(viewModel.IsSearchPrompt);
        Assert.AreEqual(0, viewModel.MovieCards.Count);

        viewModel.Query = "fantasy";
        await viewModel.SearchDebounceTask;
        Assert.AreSequenceEqual(
            new[] { 1 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );

        viewModel.Query = "   ";
        await viewModel.SearchDebounceTask;
        Assert.IsTrue(viewModel.IsSearchPrompt);
        Assert.AreEqual(0, viewModel.MovieCards.Count);

        viewModel.Query = "does not exist";
        await viewModel.SearchDebounceTask;
        Assert.IsTrue(viewModel.HasNoResults);
        Assert.AreEqual(CatalogMessageKey.NoSearchResults, viewModel.MessageKey);

        await viewModel.SwitchSectionAsync(CatalogSection.MyFavorites);
        Assert.AreSequenceEqual(
            new[] { 2 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );
    }

    [TestMethod]
    public async Task RatingFilters_ShowOnlyMatchingComingSoonMovies()
    {
        Movie g = MovieWithRating(1, "Gentle Day", Today.AddDays(1), "G");
        Movie pg = MovieWithRating(2, "Big Adventure", Today.AddDays(2), "PG");
        Movie ratingSoon = MovieWithRating(3, "Future Friend", Today.AddDays(3), null);
        CatalogViewModel viewModel = CreateViewModel(FreshService(g, pg, ratingSoon));

        await viewModel.InitializeAsync();

        Assert.AreEqual(MovieRatingFilter.All, viewModel.SelectedRatingFilter);
        Assert.AreSequenceEqual(
            new[] { 1, 2, 3 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );

        viewModel.SelectRatingFilter(MovieRatingFilter.G);
        Assert.AreSequenceEqual(
            new[] { 1 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );
        Assert.AreEqual(1, viewModel.CurrentCount);
        Assert.AreEqual(1, viewModel.MovieGroups.Sum(group => group.Count));

        viewModel.SelectRatingFilter(MovieRatingFilter.PG);
        Assert.AreSequenceEqual(
            new[] { 2 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );

        viewModel.SelectRatingFilter(MovieRatingFilter.RatingSoon);
        Assert.AreSequenceEqual(
            new[] { 3 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );

        viewModel.SelectRatingFilter(MovieRatingFilter.All);
        Assert.AreEqual(3, viewModel.MovieCards.Count);
    }

    [TestMethod]
    public async Task RatingFilter_EmptyStateAndOtherSectionsUseTheirOwnMovieSets()
    {
        Movie g = MovieWithRating(1, "Gentle Day", Today.AddDays(1), "G");
        Movie pg = MovieWithRating(2, "Favorite Quest", Today.AddDays(2), "PG");
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        FavoriteEntry favorite = pg.CreateFavoriteEntry()!.Value;
        favorites
            .GetAsync(Today, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(FavoritesResult.Success(new[] { favorite })));
        favorites
            .PruneAsync(Today, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(FavoritesResult.Success(new[] { favorite })));
        CatalogViewModel viewModel = CreateViewModel(
            FreshService(g, pg),
            favorites,
            searchDebounce: TimeSpan.Zero
        );
        await viewModel.InitializeAsync();

        viewModel.SelectRatingFilter(MovieRatingFilter.RatingSoon);

        Assert.AreEqual(0, viewModel.CurrentCount);
        Assert.AreEqual(CatalogViewState.Empty, viewModel.State);
        Assert.AreEqual(CatalogMessageKey.NoMovies, viewModel.MessageKey);

        await viewModel.SwitchSectionAsync(CatalogSection.MyFavorites);
        Assert.AreSequenceEqual(
            new[] { 2 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );

        await viewModel.SwitchSectionAsync(CatalogSection.FindAMovie);
        viewModel.Query = "Favorite";
        await viewModel.SearchDebounceTask;
        Assert.AreSequenceEqual(
            new[] { 2 },
            viewModel.MovieCards.Select(card => card.MovieId).ToArray()
        );

        await viewModel.SwitchSectionAsync(CatalogSection.ComingSoon);
        Assert.AreEqual(0, viewModel.CurrentCount);
        Assert.AreEqual(MovieRatingFilter.RatingSoon, viewModel.SelectedRatingFilter);
    }

    [TestMethod]
    public async Task FavoriteToggle_WithActiveRatingFilter_PreservesGroupedCollections()
    {
        Movie movie = MovieWithRating(1, "Keep my place", Today.AddDays(1), "G");
        FavoriteEntry favorite = movie.CreateFavoriteEntry()!.Value;
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        favorites
            .ToggleAsync(favorite, Today, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new FavoriteToggleResult(FavoriteToggleStatus.Added, favorite))
            );
        CatalogViewModel viewModel = CreateViewModel(FreshService(movie), favorites);
        await viewModel.InitializeAsync();
        viewModel.SelectRatingFilter(MovieRatingFilter.G);
        var groups = viewModel.MovieGroups;
        var cards = viewModel.MovieCards;

        await viewModel.MovieCards.Single().ToggleFavoriteCommand.ExecuteAsync(null);

        Assert.AreSame(groups, viewModel.MovieGroups);
        Assert.AreSame(cards, viewModel.MovieCards);
        Assert.IsTrue(viewModel.MovieCards.Single().IsFavorite);
    }

    [TestMethod]
    public async Task FavoriteToggle_UpdatesCardsOnlyAfterPersistence_AndRemovesFromFavorites()
    {
        Movie movie = MovieWithRelease(1, "Save me", Today.AddDays(1));
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        favorites
            .ToggleAsync(Arg.Any<FavoriteEntry>(), Today, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new FavoriteToggleResult(
                        FavoriteToggleStatus.Added,
                        movie.CreateFavoriteEntry()!.Value
                    )
                ),
                Task.FromResult(
                    new FavoriteToggleResult(
                        FavoriteToggleStatus.Failed,
                        movie.CreateFavoriteEntry()!.Value,
                        error: new IOException("read-only")
                    )
                ),
                Task.FromResult(
                    new FavoriteToggleResult(
                        FavoriteToggleStatus.Removed,
                        movie.CreateFavoriteEntry()!.Value
                    )
                )
            );
        IMovieCatalogService service = FreshService(movie);
        CatalogViewModel viewModel = CreateViewModel(service, favorites);
        await viewModel.InitializeAsync();
        MovieCardViewModel card = viewModel.MovieCards.Single();

        await card.ToggleFavoriteCommand.ExecuteAsync(null);
        Assert.IsTrue(card.IsFavorite);
        Assert.AreEqual(1, viewModel.FavoriteCount);

        await viewModel.SwitchSectionAsync(CatalogSection.MyFavorites);
        Assert.AreEqual(1, viewModel.MovieCards.Count);
        FavoriteToggleResult failed = await viewModel.ToggleFavoriteAsync(
            viewModel.MovieCards.Single()
        );
        Assert.AreEqual(FavoriteToggleStatus.Failed, failed.Status);
        Assert.IsTrue(viewModel.MovieCards.Single().IsFavorite);
        Assert.AreEqual(CatalogMessageKey.FavoriteSaveFailed, viewModel.FavoriteMessageKey);

        FavoriteToggleResult removed = await viewModel.ToggleFavoriteAsync(
            viewModel.MovieCards.Single()
        );
        Assert.AreEqual(FavoriteToggleStatus.Removed, removed.Status);
        Assert.AreEqual(0, viewModel.MovieCards.Count);
        Assert.AreEqual(0, viewModel.FavoriteCount);
    }

    [TestMethod]
    public async Task OpenDetail_SynchronizesFavoriteStateAndUsesNavigationService()
    {
        Movie movie = MovieWithRelease(1, "Detail", Today);
        IMovieCatalogService service = FreshService(movie);
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        FavoriteEntry entry = movie.CreateFavoriteEntry()!.Value;
        favorites
            .ToggleAsync(entry, Today, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FavoriteToggleResult(FavoriteToggleStatus.Added, entry)));
        INavigationService navigation = Substitute.For<INavigationService>();
        CatalogViewModel viewModel = new(service, favorites, new FixedClock(Today), navigation);
        await viewModel.InitializeAsync();

        MovieCardViewModel card = viewModel.MovieCards.Single();
        await viewModel.OpenDetailAsync(card);
        Assert.AreEqual(movie, viewModel.SelectedMovie);
        await navigation
            .Received(1)
            .NavigateToMovieDetailAsync(movie.Id, Arg.Any<CancellationToken>());

        await viewModel.SelectedMovieDetail!.ToggleFavoriteAsync();

        Assert.IsTrue(card.IsFavorite);
        Assert.IsTrue(viewModel.SelectedMovieDetail.IsFavorite);
        Assert.AreEqual(1, viewModel.FavoriteCount);
    }

    [TestMethod]
    public void Grouping_PutsRetainedAndTodayFirst_ThenExactFutureDates()
    {
        MovieGroupViewModel[] groups = MovieGroupViewModel
            .CreateGroups(
                new[]
                {
                    MovieWithRelease(4, "Future B", Today.AddDays(3)),
                    MovieWithRelease(1, "Past", Today.AddDays(-13)),
                    MovieWithRelease(3, "Future A", Today.AddDays(3)),
                    MovieWithRelease(2, "Today", Today),
                },
                new FixedClock(Today)
            )
            .ToArray();

        Assert.AreEqual(2, groups.Length);
        Assert.AreEqual(MovieGroupKind.InTheatersNow, groups[0].GroupKind);
        CollectionAssert.AreEqual(
            new[] { 1, 2 },
            groups[0].Cards.Select(card => card.MovieId).ToArray()
        );
        Assert.AreEqual(Today.AddDays(3), groups[1].ReleaseDate);
        CollectionAssert.AreEqual(
            new[] { 3, 4 },
            groups[1].Cards.Select(card => card.MovieId).ToArray()
        );
        Assert.AreEqual(ReleaseStatus.Today, groups[0].Cards[1].Status);
        Assert.AreEqual(
            1,
            new MovieCardViewModel(
                MovieWithRelease(5, "Tomorrow", Today.AddDays(1)),
                new FixedClock(Today)
            ).Sleeps
        );
        Assert.AreEqual(
            4,
            new MovieCardViewModel(
                MovieWithRelease(6, "Later", Today.AddDays(4)),
                new FixedClock(Today)
            ).Sleeps
        );
    }

    [TestMethod]
    public void Grouping_ExposesCardsAsEnumerableForGroupedCollectionView()
    {
        MovieCardViewModel card = new(MovieWithRelease(1, "One", Today), new FixedClock(Today));
        MovieGroupViewModel group = new(MovieGroupKind.InTheatersNow, null, new[] { card });

        Assert.AreSame(card, group.Single());
        Assert.AreSame(card, ((IEnumerable<MovieCardViewModel>)group).Single());
    }

    private static CatalogViewModel CreateViewModel(
        IMovieCatalogService service,
        IFavoritesStore? favorites = null,
        TimeSpan? searchDebounce = null
    ) => new(service, favorites, new FixedClock(Today), searchDebounce: searchDebounce);

    private static IMovieCatalogService FreshService(params Movie[] movies)
    {
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Cache(CatalogResultStatus.FreshCache, movies)));
        return service;
    }

    private static CatalogResult Cache(CatalogResultStatus status, params Movie[] movies) =>
        new(
            status,
            movies,
            lastSuccessfulRefresh: DateTimeOffset.UtcNow,
            cacheAge: TimeSpan.Zero,
            isStale: status == CatalogResultStatus.StaleCache,
            usedCache: true,
            snapshot: MovieCatalogSnapshot.Create(movies, Today),
            cacheStatus: CatalogCacheStatus.Available
        );

    private static CatalogResult Refreshed(params Movie[] movies) =>
        new(
            CatalogResultStatus.Refreshed,
            movies,
            lastSuccessfulRefresh: DateTimeOffset.UtcNow,
            snapshot: MovieCatalogSnapshot.Create(movies, Today),
            cacheStatus: CatalogCacheStatus.Available
        );

    private static Movie MovieWithRelease(int id, string title, DateOnly releaseDate) =>
        new(
            id,
            title,
            "G",
            new TheatricalRelease(releaseDate, "US", TheatricalRelease.TheatricalType)
        );

    private static Movie MovieWithGenre(int id, string title, DateOnly releaseDate, string genre) =>
        new(
            id,
            title,
            "PG",
            new TheatricalRelease(releaseDate, "US", TheatricalRelease.TheatricalType),
            new[] { new MovieGenre(0, genre) }
        );

    private static Movie MovieWithRating(
        int id,
        string title,
        DateOnly releaseDate,
        string? certification
    ) =>
        new(
            id,
            title,
            certification,
            new TheatricalRelease(releaseDate, "US", TheatricalRelease.TheatricalType),
            certification is null
                ? new[] { new MovieGenre(MovieGenre.AnimationId, "Animation") }
                : null
        );

    private static TaskCompletionSource<T> Pending<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task<CatalogResult> WaitForCancellation(
        CancellationToken cancellationToken
    )
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return Refreshed();
    }

    private static async Task Eventually(Func<bool> predicate)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(3);
        while (!predicate() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.IsTrue(predicate(), "The expected state was not published in time.");
    }

    private sealed class FixedClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; } = today;
    }

    private sealed class MutableClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; set; } = today;
    }
}
