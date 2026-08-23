using GoodMovies.ViewModels;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class TrailerPlaybackStateCoordinatorTests
{
    [TestMethod]
    public void BeginReplacement_ClearsAndPublishesCurrentIdentityBeforeCompletion()
    {
        TrailerPlaybackStateCoordinator coordinator = new();
        List<string?> changes = new();
        coordinator.ActiveYoutubeKeyChanged += changes.Add;
        TrailerPlaybackStateCoordinator.LaunchOperation first = coordinator.BeginLaunch(
            "trailer-a"
        );
        Assert.IsTrue(coordinator.CompleteLaunch(first, succeeded: true));

        TrailerPlaybackStateCoordinator.LaunchOperation replacement = coordinator.BeginLaunch(
            "trailer-b"
        );

        Assert.IsFalse(replacement.WasAlreadyPlaying);
        Assert.IsNull(coordinator.ActiveYoutubeKey);
        CollectionAssert.AreEqual(new string?[] { "trailer-a", null }, changes);

        Assert.IsTrue(coordinator.CompleteLaunch(replacement, succeeded: true));
        Assert.AreEqual("trailer-b", coordinator.ActiveYoutubeKey);
        CollectionAssert.AreEqual(new string?[] { "trailer-a", null, "trailer-b" }, changes);
    }

    [TestMethod]
    public void FailedReplacement_LeavesNoActiveIdentity()
    {
        TrailerPlaybackStateCoordinator coordinator = CreatePlaying("trailer-a");
        TrailerPlaybackStateCoordinator.LaunchOperation replacement = coordinator.BeginLaunch(
            "trailer-b"
        );

        Assert.IsFalse(coordinator.CompleteLaunch(replacement, succeeded: false));
        Assert.IsNull(coordinator.ActiveYoutubeKey);
    }

    [TestMethod]
    public void CancelledReplacement_InvalidatesItsLaterCompletion()
    {
        TrailerPlaybackStateCoordinator coordinator = CreatePlaying("trailer-a");
        TrailerPlaybackStateCoordinator.LaunchOperation replacement = coordinator.BeginLaunch(
            "trailer-b"
        );

        Assert.IsTrue(coordinator.CancelLaunch(replacement));
        Assert.IsFalse(coordinator.CompleteLaunch(replacement, succeeded: true));
        Assert.IsNull(coordinator.ActiveYoutubeKey);
    }

    [TestMethod]
    public void OlderCompletion_CannotOverwriteNewerSuccessfulLaunch()
    {
        TrailerPlaybackStateCoordinator coordinator = new();
        TrailerPlaybackStateCoordinator.LaunchOperation first = coordinator.BeginLaunch(
            "trailer-a"
        );
        TrailerPlaybackStateCoordinator.LaunchOperation second = coordinator.BeginLaunch(
            "trailer-b"
        );

        Assert.IsTrue(coordinator.CompleteLaunch(second, succeeded: true));
        Assert.IsFalse(coordinator.CompleteLaunch(first, succeeded: true));
        Assert.AreEqual("trailer-b", coordinator.ActiveYoutubeKey);
    }

    [TestMethod]
    public void RetryingFormerTrailerDuringReplacement_IsANewLaunch()
    {
        TrailerPlaybackStateCoordinator coordinator = CreatePlaying("trailer-a");
        TrailerPlaybackStateCoordinator.LaunchOperation replacement = coordinator.BeginLaunch(
            "trailer-b"
        );
        TrailerPlaybackStateCoordinator.LaunchOperation retry = coordinator.BeginLaunch(
            "trailer-a"
        );

        Assert.IsFalse(retry.WasAlreadyPlaying);
        Assert.IsNull(coordinator.ActiveYoutubeKey);
        Assert.IsFalse(coordinator.CompleteLaunch(replacement, succeeded: true));
        Assert.IsTrue(coordinator.CompleteLaunch(retry, succeeded: true));
        Assert.AreEqual("trailer-a", coordinator.ActiveYoutubeKey);
    }

    private static TrailerPlaybackStateCoordinator CreatePlaying(string youtubeKey)
    {
        TrailerPlaybackStateCoordinator coordinator = new();
        TrailerPlaybackStateCoordinator.LaunchOperation operation = coordinator.BeginLaunch(
            youtubeKey
        );
        Assert.IsTrue(coordinator.CompleteLaunch(operation, succeeded: true));
        return coordinator;
    }
}
