using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class TrailerSelectionPolicyTests
{
    [TestMethod]
    public void Select_UsesOfficialEnglishYoutubeTrailerFirst()
    {
        MovieTrailer officialTrailer = Trailer("official trailer", "Trailer", true, "en-US");
        MovieTrailer regularTrailer = Trailer("regular trailer", "Trailer", false, "en");
        MovieTrailer officialTeaser = Trailer("official teaser", "Teaser", true, "en");

        MovieTrailer? selected = TrailerSelectionPolicy.Select(
            new[] { officialTeaser, regularTrailer, officialTrailer }
        );
        MovieTrailer? selectedThroughPolicy = new TrailerSelectionPolicy().Select(
            new[] { officialTeaser, regularTrailer, officialTrailer }
        );

        Assert.AreSame(officialTrailer, selected);
        Assert.AreSame(officialTrailer, selectedThroughPolicy);
    }

    [TestMethod]
    public void Select_UsesOfficialTeaserBeforeUnofficialTrailer()
    {
        MovieTrailer regularTrailer = Trailer("regular trailer", "Trailer", false, "en-GB");
        MovieTrailer officialTeaser = Trailer("official teaser", "Teaser", true, "en");
        MovieTrailer regularTeaser = Trailer("regular teaser", "Teaser", false, "en");

        Assert.AreSame(
            officialTeaser,
            TrailerSelectionPolicy.Select(new[] { regularTeaser, officialTeaser, regularTrailer })
        );
    }

    [TestMethod]
    public void Select_UsesOfficialTeaserAndRejectsUnofficialTeaser()
    {
        MovieTrailer officialTeaser = Trailer("official teaser", "Teaser", true, "en");
        MovieTrailer regularTeaser = Trailer("regular teaser", "Teaser", false, "en-US");

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
        MovieTrailer spanishTrailer = Trailer("spanish", "Trailer", true, "es");
        MovieTrailer feature = Trailer("feature", "Feature", true, "en");
        MovieTrailer noLanguage = Trailer("no-language", "Trailer", true, null);

        Assert.IsNull(
            TrailerSelectionPolicy.Select(
                new[] { vimeoTrailer, spanishTrailer, feature, noLanguage }
            )
        );
    }

    [TestMethod]
    public void Select_RejectsOfficialTrailerWithInvalidYouTubeKey()
    {
        MovieTrailer invalid = new("short", "Trailer", "YouTube", "Trailer", true, "en");

        Assert.IsNull(TrailerSelectionPolicy.Select(new[] { invalid }));
    }

    [TestMethod]
    public void Select_IsCaseInsensitiveAndPreservesEnglishIsoPrefix()
    {
        MovieTrailer trailer = new(
            "Abc_123-def",
            "name",
            " youtube ",
            " trailer ",
            true,
            " EN-us "
        );

        Assert.IsTrue(trailer.IsYouTube);
        Assert.IsTrue(trailer.IsEnglish);
        Assert.AreSame(trailer, TrailerSelectionPolicy.Select(new[] { trailer }));
    }

    private static MovieTrailer Trailer(string key, string type, bool official, string? language) =>
        new("Abc_123-def", key, "YouTube", type, official, language);
}
