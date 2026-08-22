using GoodMovies.Core;
using GoodMovies.ViewModels;
using NSubstitute;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class NavigationViewModelTests
{
    [TestMethod]
    public async Task Navigation_ExposesCountsAndSwitchesCatalogSection()
    {
        DateOnly today = new(2026, 8, 21);
        Movie movie = new(
            1,
            "Safe movie",
            "G",
            new TheatricalRelease(today, "US", TheatricalRelease.TheatricalType)
        );
        IMovieCatalogService service = Substitute.For<IMovieCatalogService>();
        service
            .LoadAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new CatalogResult(
                        CatalogResultStatus.FreshCache,
                        new[] { movie },
                        usedCache: true,
                        snapshot: MovieCatalogSnapshot.Create(new[] { movie }, today)
                    )
                )
            );
        CatalogViewModel catalog = new(service, new FixedClock(today));
        using NavigationViewModel navigation = new(catalog);

        await catalog.InitializeAsync();
        navigation.SwitchSectionCommand.Execute(CatalogSection.FindAMovie);

        Assert.AreEqual(1, navigation.ComingSoonCount);
        Assert.AreEqual(CatalogSection.FindAMovie, navigation.SelectedSection);
        Assert.AreEqual(CatalogSection.FindAMovie, catalog.SelectedSection);
    }

    private sealed class FixedClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; } = today;
    }
}
