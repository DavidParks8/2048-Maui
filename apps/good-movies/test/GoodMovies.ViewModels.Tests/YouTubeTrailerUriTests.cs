using GoodMovies.ViewModels;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class YouTubeTrailerUriTests
{
    private const string ValidKey = "dQw4w9WgXcQ";

    [TestMethod]
    public void Create_ValidKey_BuildsPrivacyEnhancedFullscreenEmbed()
    {
        Assert.IsTrue(YouTubeTrailerUri.TryCreate(ValidKey, out Uri? uri));
        Assert.IsNotNull(uri);

        Assert.AreEqual(Uri.UriSchemeHttps, uri.Scheme);
        Assert.AreEqual("www.youtube-nocookie.com", uri.Host);
        Assert.AreEqual($"/embed/{ValidKey}", uri.AbsolutePath);
        Assert.AreEqual("?autoplay=1&controls=1&playsinline=0&rel=0", uri.Query);
        Assert.IsTrue(YouTubeTrailerUri.TryGetTrustedVideoKey(uri, out string key));
        Assert.AreEqual(ValidKey, key);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("short")]
    [DataRow("dQw4w9WgXcQ!")]
    public void TryCreate_InvalidKey_ReturnsFalse(string? key)
    {
        Assert.IsFalse(YouTubeTrailerUri.TryCreate(key, out _));
    }

    [TestMethod]
    [DataRow("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [DataRow("https://www.youtube-nocookie.com/watch?v=dQw4w9WgXcQ")]
    [DataRow("http://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [DataRow("https://www.youtube-nocookie.com/embed/not-a-key")]
    [DataRow("https://www.youtube-nocookie.com:444/embed/dQw4w9WgXcQ")]
    public void TryGetTrustedVideoKey_UnapprovedDestination_ReturnsFalse(string uri)
    {
        Assert.IsFalse(YouTubeTrailerUri.TryGetTrustedVideoKey(new Uri(uri), out _));
    }
}
