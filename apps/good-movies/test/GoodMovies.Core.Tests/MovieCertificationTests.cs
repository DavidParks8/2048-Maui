using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class MovieCertificationTests
{
    [TestMethod]
    [DataRow("G", "G")]
    [DataRow("g", "G")]
    [DataRow("  G  ", "G")]
    [DataRow("PG", "PG")]
    [DataRow("pg", "PG")]
    [DataRow("  pG  ", "PG")]
    public void TryCreate_AllowedCertification_NormalizesCaseAndWhitespace(
        string raw,
        string expected
    )
    {
        bool created = MovieCertification.TryCreate(raw, out MovieCertification? certification);

        Assert.IsTrue(created);
        Assert.IsNotNull(certification);
        Assert.AreEqual(expected, certification.Code);
    }

    [TestMethod]
    public void TryCreate_MissingOrUnsupportedCertification_Rejects()
    {
        string?[] values =
        {
            null,
            string.Empty,
            " ",
            "NR",
            "PG-13",
            "R",
            "G/PG",
            "G rated",
            " P G ",
        };

        foreach (string? value in values)
        {
            bool created = MovieCertification.TryCreate(
                value,
                out MovieCertification? certification
            );

            Assert.IsFalse(created, $"Unexpected certification: {value}");
            Assert.IsNull(certification);
        }
    }

    [TestMethod]
    public void MovieSafetyPolicy_InvalidRawCertification_IsUnsafe()
    {
        DateOnly today = new(2026, 8, 21);
        TheatricalRelease release = new(today, "US", TheatricalRelease.TheatricalType);
        Assert.IsTrue(MovieSafetyPolicy.IsSafe(new Movie(1, "G Movie", "G", new[] { release })));
        Assert.IsTrue(MovieSafetyPolicy.IsSafe(new Movie(2, "PG Movie", "PG", new[] { release })));
        Assert.IsFalse(MovieSafetyPolicy.IsSafe(new Movie(3, "Unrated", null, new[] { release })));
        Assert.IsFalse(
            MovieSafetyPolicy.IsSafe(new Movie(4, "PG-13 Movie", "PG-13", new[] { release }))
        );
    }

    [TestMethod]
    public void MovieSafetyPolicy_NotYetRatedMovie_IsSafeOnlyWhenItIsAFamilyTitle()
    {
        DateOnly today = new(2026, 8, 21);
        TheatricalRelease release = new(today, "US", TheatricalRelease.TheatricalType);
        Movie familyMovie = new(
            1,
            "Not rated yet cartoon",
            certification: null,
            releases: new[] { release },
            genres: new[] { new MovieGenre(MovieGenre.AnimationId, "Animation") },
            originalLanguage: "en"
        );
        Movie grownUpMovie = new(
            2,
            "Not rated yet thriller",
            certification: null,
            releases: new[] { release },
            genres: new[] { new MovieGenre(53, "Thriller") },
            originalLanguage: "en"
        );
        Movie nonEnglishFamilyMovie = new(
            3,
            "Not rated yet foreign cartoon",
            certification: null,
            releases: new[] { release },
            genres: new[] { new MovieGenre(MovieGenre.AnimationId, "Animation") },
            originalLanguage: "fr"
        );
        Movie ratedTeenFamilyMovie = new(
            4,
            "PG-13 cartoon",
            certification: "PG-13",
            releases: new[] { release },
            genres: new[] { new MovieGenre(MovieGenre.AnimationId, "Animation") },
            originalLanguage: "en"
        );

        Assert.IsTrue(familyMovie.IsNotYetRated);
        Assert.IsTrue(familyMovie.IsFamilyAudience);
        Assert.IsTrue(MovieSafetyPolicy.IsSafe(familyMovie));

        Assert.IsTrue(grownUpMovie.IsNotYetRated);
        Assert.IsFalse(grownUpMovie.IsFamilyAudience);
        Assert.IsFalse(MovieSafetyPolicy.IsSafe(grownUpMovie));
        Assert.IsFalse(MovieSafetyPolicy.IsSafe(nonEnglishFamilyMovie));

        // A rating we do not allow is not the same as having no rating yet.
        Assert.IsFalse(ratedTeenFamilyMovie.IsNotYetRated);
        Assert.IsFalse(MovieSafetyPolicy.IsSafe(ratedTeenFamilyMovie));
    }
}
