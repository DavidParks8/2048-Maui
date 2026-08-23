using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class TrailerSelectionPolicyTests
{
    [TestMethod]
    public void Select_UsesOfficialEnglishYoutubeTrailerFirst()
    {
        MovieTrailer officialTrailer = Trailer("Trailer", true, "en-US");
        MovieTrailer regularTrailer = Trailer("Trailer", false, "en");
        MovieTrailer officialTeaser = Trailer("Teaser", true, "en");

        MovieTrailer? selected = TrailerSelectionPolicy.Select(
            new[] { officialTeaser, regularTrailer, officialTrailer }
        );
        Assert.AreSame(officialTrailer, selected);
    }

    [TestMethod]
    public void Select_UsesOfficialTeaserBeforeUnofficialTrailer()
    {
        MovieTrailer regularTrailer = Trailer("Trailer", false, "en-GB");
        MovieTrailer officialTeaser = Trailer("Teaser", true, "en");
        MovieTrailer regularTeaser = Trailer("Teaser", false, "en");

        Assert.AreSame(
            officialTeaser,
            TrailerSelectionPolicy.Select(new[] { regularTeaser, officialTeaser, regularTrailer })
        );
    }

    [TestMethod]
    public void Select_UsesOfficialTeaserAndRejectsUnofficialTeaser()
    {
        MovieTrailer officialTeaser = Trailer("Teaser", true, "en");
        MovieTrailer regularTeaser = Trailer("Teaser", false, "en-US");

        Assert.AreSame(
            officialTeaser,
            TrailerSelectionPolicy.Select(new[] { regularTeaser, officialTeaser })
        );
        Assert.IsNull(TrailerSelectionPolicy.Select(new[] { regularTeaser }));
    }

    [TestMethod]
    public void Select_RejectsNonYoutubeNonEnglishAndUnsupportedTypes()
    {
        MovieTrailer vimeoTrailer = new("vimeo", "Vimeo", "Trailer", true, "en");
        MovieTrailer spanishTrailer = Trailer("Trailer", true, "es");
        MovieTrailer feature = Trailer("Feature", true, "en");
        MovieTrailer noLanguage = Trailer("Trailer", true, null);

        Assert.IsNull(
            TrailerSelectionPolicy.Select(
                new[] { vimeoTrailer, spanishTrailer, feature, noLanguage }
            )
        );
    }

    [TestMethod]
    public void Select_RejectsOfficialTrailerWithInvalidYouTubeKey()
    {
        MovieTrailer invalid = new("short", "YouTube", "Trailer", true, "en");

        Assert.IsNull(TrailerSelectionPolicy.Select(new[] { invalid }));
    }

    [TestMethod]
    public void Select_IsCaseInsensitiveAndPreservesEnglishIsoPrefix()
    {
        MovieTrailer trailer = new("Abc_123-def", " youtube ", " trailer ", true, " EN-us ");

        Assert.IsTrue(trailer.IsYouTube);
        Assert.AreSame(trailer, TrailerSelectionPolicy.Select(new[] { trailer }));
    }

    private static MovieTrailer Trailer(string type, bool official, string? language) =>
        new("Abc_123-def", "YouTube", type, official, language);
}
