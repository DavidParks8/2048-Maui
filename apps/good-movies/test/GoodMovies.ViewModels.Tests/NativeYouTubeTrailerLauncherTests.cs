using GoodMovies.ViewModels;
using NSubstitute;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class NativeYouTubeTrailerLauncherTests
{
    private const string ValidKey = "dQw4w9WgXcQ";

    [TestMethod]
    public async Task LaunchAsync_YouTubeIsAvailable_OpensOnlyTheNativeAppUri()
    {
        Uri expectedUri = YouTubeTrailerUri.Create(ValidKey);
        INativeUriLauncher uriLauncher = Substitute.For<INativeUriLauncher>();
        uriLauncher.CanOpenAsync(expectedUri, Arg.Any<CancellationToken>()).Returns(true);
        uriLauncher.OpenAsync(expectedUri, Arg.Any<CancellationToken>()).Returns(true);
        NativeYouTubeTrailerLauncher launcher = new(uriLauncher);

        bool launched = await launcher.LaunchAsync(ValidKey);

        Assert.IsTrue(launched);
        Assert.AreEqual("youtube", expectedUri.Scheme);
        Assert.IsFalse(expectedUri.Scheme is "http" or "https");
        await uriLauncher.Received(1).CanOpenAsync(expectedUri, Arg.Any<CancellationToken>());
        await uriLauncher.Received(1).OpenAsync(expectedUri, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task LaunchAsync_YouTubeIsUnavailable_DoesNotOpenAnyFallback()
    {
        INativeUriLauncher uriLauncher = Substitute.For<INativeUriLauncher>();
        uriLauncher.CanOpenAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>()).Returns(false);
        NativeYouTubeTrailerLauncher launcher = new(uriLauncher);

        bool launched = await launcher.LaunchAsync(ValidKey);

        Assert.IsFalse(launched);
        await uriLauncher.DidNotReceive().OpenAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task LaunchAsync_NativeAppCannotOpenVideo_ReturnsFailureWithoutFallback()
    {
        INativeUriLauncher uriLauncher = Substitute.For<INativeUriLauncher>();
        uriLauncher.CanOpenAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>()).Returns(true);
        uriLauncher.OpenAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>()).Returns(false);
        NativeYouTubeTrailerLauncher launcher = new(uriLauncher);

        bool launched = await launcher.LaunchAsync(ValidKey);

        Assert.IsFalse(launched);
        await uriLauncher
            .Received(1)
            .OpenAsync(
                Arg.Is<Uri>(uri => uri.Scheme == YouTubeTrailerUri.Scheme),
                Arg.Any<CancellationToken>()
            );
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
