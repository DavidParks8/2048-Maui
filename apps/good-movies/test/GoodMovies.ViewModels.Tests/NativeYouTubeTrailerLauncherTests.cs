using GoodMovies.ViewModels;
using NSubstitute;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class NativeYouTubeTrailerLauncherTests
{
    private const string ValidKey = "dQw4w9WgXcQ";

    [TestMethod]
    public async Task LaunchAsync_YouTubeKidsIsAvailable_OpensKidsWithoutQueryingYouTube()
    {
        Uri kidsUri = YouTubeKidsTrailerUri.Create(ValidKey);
        Uri youtubeUri = YouTubeTrailerUri.Create(ValidKey);
        INativeUriLauncher uriLauncher = Substitute.For<INativeUriLauncher>();
        uriLauncher.CanOpenAsync(kidsUri, Arg.Any<CancellationToken>()).Returns(true);
        uriLauncher.OpenAsync(kidsUri, Arg.Any<CancellationToken>()).Returns(true);
        NativeYouTubeTrailerLauncher launcher = new(uriLauncher);

        bool launched = await launcher.LaunchAsync(ValidKey);

        Assert.IsTrue(launched);
        Assert.AreEqual("vnd.youtube.kids", kidsUri.Scheme);
        Assert.IsFalse(kidsUri.Scheme is "http" or "https");
        await uriLauncher.Received(1).CanOpenAsync(kidsUri, Arg.Any<CancellationToken>());
        await uriLauncher.Received(1).OpenAsync(kidsUri, Arg.Any<CancellationToken>());
        await uriLauncher.DidNotReceive().CanOpenAsync(youtubeUri, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task LaunchAsync_YouTubeKidsIsUnavailable_OpensStandardYouTube()
    {
        Uri kidsUri = YouTubeKidsTrailerUri.Create(ValidKey);
        Uri youtubeUri = YouTubeTrailerUri.Create(ValidKey);
        INativeUriLauncher uriLauncher = Substitute.For<INativeUriLauncher>();
        uriLauncher.CanOpenAsync(kidsUri, Arg.Any<CancellationToken>()).Returns(false);
        uriLauncher.CanOpenAsync(youtubeUri, Arg.Any<CancellationToken>()).Returns(true);
        uriLauncher.OpenAsync(youtubeUri, Arg.Any<CancellationToken>()).Returns(true);
        NativeYouTubeTrailerLauncher launcher = new(uriLauncher);

        bool launched = await launcher.LaunchAsync(ValidKey);

        Assert.IsTrue(launched);
        await uriLauncher.Received(1).OpenAsync(youtubeUri, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task LaunchAsync_NoYouTubeAppIsAvailable_DoesNotOpenAnyBrowserFallback()
    {
        INativeUriLauncher uriLauncher = Substitute.For<INativeUriLauncher>();
        uriLauncher.CanOpenAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>()).Returns(false);
        NativeYouTubeTrailerLauncher launcher = new(uriLauncher);

        bool launched = await launcher.LaunchAsync(ValidKey);

        Assert.IsFalse(launched);
        await uriLauncher.DidNotReceive().OpenAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>());
        await uriLauncher
            .DidNotReceive()
            .CanOpenAsync(
                Arg.Is<Uri>(uri => uri.Scheme == "http" || uri.Scheme == "https"),
                Arg.Any<CancellationToken>()
            );
    }

    [TestMethod]
    public async Task LaunchAsync_YouTubeKidsRejectsVideo_TriesStandardYouTubeOnly()
    {
        Uri kidsUri = YouTubeKidsTrailerUri.Create(ValidKey);
        Uri youtubeUri = YouTubeTrailerUri.Create(ValidKey);
        INativeUriLauncher uriLauncher = Substitute.For<INativeUriLauncher>();
        uriLauncher.CanOpenAsync(kidsUri, Arg.Any<CancellationToken>()).Returns(true);
        uriLauncher.OpenAsync(kidsUri, Arg.Any<CancellationToken>()).Returns(false);
        uriLauncher.CanOpenAsync(youtubeUri, Arg.Any<CancellationToken>()).Returns(true);
        uriLauncher.OpenAsync(youtubeUri, Arg.Any<CancellationToken>()).Returns(true);
        NativeYouTubeTrailerLauncher launcher = new(uriLauncher);

        bool launched = await launcher.LaunchAsync(ValidKey);

        Assert.IsTrue(launched);
        await uriLauncher.Received(1).OpenAsync(kidsUri, Arg.Any<CancellationToken>());
        await uriLauncher.Received(1).OpenAsync(youtubeUri, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("short")]
    [DataRow("dQw4w9WgXcQ!")]
    public async Task LaunchAsync_InvalidKey_DoesNotQueryOrOpenApps(string? key)
    {
        INativeUriLauncher uriLauncher = Substitute.For<INativeUriLauncher>();
        NativeYouTubeTrailerLauncher launcher = new(uriLauncher);

        bool launched = await launcher.LaunchAsync(key!);

        Assert.IsFalse(launched);
        await uriLauncher
            .DidNotReceive()
            .CanOpenAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>());
        await uriLauncher.DidNotReceive().OpenAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>());
    }
}
