using GoodMovies.Core;
using GoodMovies.ViewModels;
using NSubstitute;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class MovieDetailViewModelTests
{
    private static readonly DateOnly Today = new(2026, 8, 21);

    [TestMethod]
    public async Task Detail_FavoriteStateAndTrailerFlowRequireSuccessfulPersistenceAndLaunch()
    {
        Movie movie = Movie(
            7,
            "A Good Story",
            Today.AddDays(1),
            "A bright day brings a small surprise."
        );
        IFavoritesStore favorites = Substitute.For<IFavoritesStore>();
        FavoriteEntry entry = movie.CreateFavoriteEntry()!.Value;
        favorites
            .ToggleAsync(entry, Today, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new FavoriteToggleResult(FavoriteToggleStatus.Added, entry)),
                Task.FromResult(
                    new FavoriteToggleResult(
                        FavoriteToggleStatus.Failed,
                        entry,
                        error: new IOException("offline")
                    )
                )
            );
        IMovieTrailerLookup lookup = Substitute.For<IMovieTrailerLookup>();
        lookup
            .GetTrailerAsync(7, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    TrailerLookupResult.Found(
                        7,
                        new MovieTrailer("youtube-key", "Trailer", "YouTube", "Trailer", true, "en")
                    )
                )
            );
        IExternalTrailerLauncher launcher = Substitute.For<IExternalTrailerLauncher>();
        launcher.LaunchAsync("youtube-key", Arg.Any<CancellationToken>()).Returns(true);
        MovieDetailViewModel detail = new(
            movie,
            new FixedClock(Today),
            favorites,
            trailerLookup: lookup,
            trailerLauncher: launcher
        );

        FavoriteToggleResult added = await detail.ToggleFavoriteAsync();
        Assert.AreEqual(FavoriteToggleStatus.Added, added.Status);
        Assert.IsTrue(detail.IsFavorite);
        FavoriteToggleResult failed = await detail.ToggleFavoriteAsync();
        Assert.AreEqual(FavoriteToggleStatus.Failed, failed.Status);
        Assert.IsTrue(detail.IsFavorite);
        Assert.AreEqual(CatalogMessageKey.FavoriteSaveFailed, detail.FavoriteMessageKey);

        TrailerPlaybackResult prepared = await detail.PrepareTrailerAsync();
        Assert.AreEqual(TrailerPlaybackState.Ready, prepared.State);
        await launcher.DidNotReceive().LaunchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        TrailerPlaybackResult trailer = await detail.PlayTrailerAsync();
        Assert.AreEqual(TrailerPlaybackState.Launched, trailer.State);
        Assert.IsTrue(detail.IsTrailerLaunched);
        await lookup.Received(1).GetTrailerAsync(7, Arg.Any<CancellationToken>());
        await launcher.Received(1).LaunchAsync("youtube-key", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Detail_TrailerExposesNotFoundMissingConfigurationAndLaunchFailure()
    {
        Movie movie = Movie(8, "Trailer states", Today, "Overview");
        IMovieTrailerLookup lookup = Substitute.For<IMovieTrailerLookup>();
        IExternalTrailerLauncher launcher = Substitute.For<IExternalTrailerLauncher>();
        MovieDetailViewModel detail = new(
            movie,
            new FixedClock(Today),
            trailerLookup: lookup,
            trailerLauncher: launcher
        );

        lookup
            .GetTrailerAsync(8, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TrailerLookupResult.NotFound(8)));
        TrailerPlaybackResult missing = await detail.PrepareTrailerAsync();
        Assert.AreEqual(TrailerPlaybackState.NotFound, missing.State);

        lookup
            .GetTrailerAsync(8, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    TrailerLookupResult.MissingConfiguration(
                        8,
                        new InvalidOperationException("missing token")
                    )
                )
            );
        detail = new(
            movie,
            new FixedClock(Today),
            trailerLookup: lookup,
            trailerLauncher: launcher
        );
        TrailerPlaybackResult configuration = await detail.PrepareTrailerAsync();
        Assert.AreEqual(TrailerPlaybackState.MissingConfiguration, configuration.State);

        lookup
            .GetTrailerAsync(8, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    TrailerLookupResult.Found(
                        8,
                        new MovieTrailer("youtube-key", "Trailer", "YouTube", "Trailer", true, "en")
                    )
                )
            );
        launcher.LaunchAsync("youtube-key", Arg.Any<CancellationToken>()).Returns(false);
        detail = new(
            movie,
            new FixedClock(Today),
            trailerLookup: lookup,
            trailerLauncher: launcher
        );
        TrailerPlaybackResult failed = await detail.PlayTrailerAsync();
        Assert.AreEqual(TrailerPlaybackState.LaunchFailed, failed.State);
        Assert.IsFalse(failed.Succeeded);
        Assert.IsTrue(detail.IsTrailerLaunchFailed);
        Assert.IsNotNull(failed.Error);

        IOException launchError = new("YouTube is blocked");
        launcher
            .LaunchAsync("youtube-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(launchError));
        detail = new(
            movie,
            new FixedClock(Today),
            trailerLookup: lookup,
            trailerLauncher: launcher
        );
        TrailerPlaybackResult errored = await detail.PlayTrailerAsync();
        Assert.AreEqual(TrailerPlaybackState.LaunchFailed, errored.State);
        Assert.AreSame(launchError, errored.Error);
    }

    [TestMethod]
    public async Task Detail_ReadAloudTokenizesHighlightsRangesAndStopsOnDeactivate()
    {
        Movie movie = Movie(9, "Read me", Today, "One bright day.");
        IWordLevelSpeechService speech = Substitute.For<IWordLevelSpeechService>();
        speech
            .SpeakAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        speech
            .SpeakWordAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        MovieDetailViewModel detail = new(movie, new FixedClock(Today), speechService: speech);

        Assert.AreSequenceEqual(
            new[] { "One", "bright", "day." },
            detail.WordTokens.Select(token => token.Text).ToArray()
        );
        await detail.PlayReadAloudAsync();
        Assert.IsFalse(detail.IsReading);
        speech.SpokenRange += Raise.Event<EventHandler<SpeechRangeEventArgs>>(
            speech,
            new SpeechRangeEventArgs(4, 6)
        );
        Assert.IsTrue(detail.WordTokens[1].IsHighlighted);
        Assert.IsFalse(detail.WordTokens[0].IsHighlighted);

        await detail.SpeakWordAsync(detail.WordTokens[2]);
        await speech.Received(1).SpeakWordAsync("day.", Arg.Any<CancellationToken>());
        Assert.IsTrue(detail.WordTokens[2].IsHighlighted);

        detail.Deactivate();
        speech.Received(1).Stop();
        speech.SpokenRange += Raise.Event<EventHandler<SpeechRangeEventArgs>>(
            speech,
            new SpeechRangeEventArgs(0, 3)
        );
        Assert.IsFalse(detail.WordTokens[0].IsHighlighted);
    }

    [TestMethod]
    public async Task Detail_StopAndDeactivateCompleteReadAloudWithoutSurfacingCancellation()
    {
        Movie movie = Movie(11, "Stop safely", Today, "Please stop this story.");
        IWordLevelSpeechService speech = Substitute.For<IWordLevelSpeechService>();
        TaskCompletionSource speaking = new(TaskCreationOptions.RunContinuationsAsynchronously);
        speech.SpeakAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(speaking.Task);
        speech.When(service => service.Stop()).Do(_ => speaking.TrySetCanceled());
        MovieDetailViewModel detail = new(movie, new FixedClock(Today), speechService: speech);

        Task operation = detail.PlayReadAloudAsync();
        Assert.IsTrue(detail.IsReading);

        detail.Deactivate();
        await operation;

        Assert.IsFalse(detail.IsReading);
        speech.Received(1).Stop();
    }

    [TestMethod]
    public async Task Detail_ReadAloudStillPropagatesCallerCancellation()
    {
        Movie movie = Movie(12, "Cancel safely", Today, "Please cancel this story.");
        IWordLevelSpeechService speech = Substitute.For<IWordLevelSpeechService>();
        using CancellationTokenSource cancellation = new();
        speech
            .SpeakAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromCanceled(callInfo.Arg<CancellationToken>()));
        MovieDetailViewModel detail = new(movie, new FixedClock(Today), speechService: speech);
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await detail.PlayReadAloudAsync(cancellation.Token)
        );
    }

    [TestMethod]
    public void Detail_ReappliesReleaseStatusWhenTheLocalDateChanges()
    {
        MutableClock clock = new(Today);
        MovieDetailViewModel detail = new(
            Movie(10, "Tomorrow", Today.AddDays(1), "A movie arriving tomorrow."),
            clock
        );
        Assert.AreEqual(ReleaseStatus.Future, detail.Status);
        Assert.AreEqual(1, detail.Sleeps);

        clock.Today = Today.AddDays(1);
        detail.ReapplyCurrentDatePolicies();

        Assert.AreEqual(ReleaseStatus.Today, detail.Status);
        Assert.AreEqual(0, detail.Sleeps);
    }

    [TestMethod]
    public void KindIcon_UsesKidReadableGenreMappingAndFallback()
    {
        Assert.AreEqual("🧭", MovieKindIconMapper.GetIcon(new MovieGenre(12, "Adventure")));
        Assert.AreEqual("🚀", MovieKindIconMapper.GetIcon(new MovieGenre(0, "Sci-Fi")));
        Assert.AreEqual("🎬", MovieKindIconMapper.GetIcon(new MovieGenre(999, "Other")));
    }

    private static Movie Movie(int id, string title, DateOnly date, string overview) =>
        new(
            id,
            title,
            "PG",
            new TheatricalRelease(date, "US", TheatricalRelease.TheatricalType),
            overview: overview
        );

    private sealed class FixedClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; } = today;
    }

    private sealed class MutableClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; set; } = today;
    }
}
