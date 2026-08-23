using GoodMovies.Infrastructure;

namespace GoodMovies.Infrastructure.Tests;

[TestClass]
public sealed class GoodMoviesInfrastructureOptionsTests
{
    [TestMethod]
    [DataRow(GoodMoviesInfrastructureOptions.MinimumPageCount - 1)]
    [DataRow(GoodMoviesInfrastructureOptions.MaximumPageCount + 1)]
    public void Validate_RejectsPageCountsOutsideBounds(int maxPages)
    {
        GoodMoviesInfrastructureOptions options = new() { MaxPages = maxPages };

        Assert.Throws<GoodMoviesConfigurationException>(options.Validate);
    }

    [TestMethod]
    [DataRow(GoodMoviesInfrastructureOptions.MinimumConcurrentRequestCount - 1)]
    [DataRow(GoodMoviesInfrastructureOptions.MaximumConcurrentRequestCount + 1)]
    public void Validate_RejectsConcurrencyOutsideBounds(int maxConcurrentRequests)
    {
        GoodMoviesInfrastructureOptions options = new()
        {
            MaxConcurrentRequests = maxConcurrentRequests,
        };

        Assert.Throws<GoodMoviesConfigurationException>(options.Validate);
    }

    [TestMethod]
    [DataRow(
        GoodMoviesInfrastructureOptions.MinimumPageCount,
        GoodMoviesInfrastructureOptions.MinimumConcurrentRequestCount
    )]
    [DataRow(
        GoodMoviesInfrastructureOptions.MaximumPageCount,
        GoodMoviesInfrastructureOptions.MaximumConcurrentRequestCount
    )]
    public void Validate_AcceptsRequestBoundaries(int maxPages, int maxConcurrentRequests)
    {
        GoodMoviesInfrastructureOptions options = new()
        {
            MaxPages = maxPages,
            MaxConcurrentRequests = maxConcurrentRequests,
        };

        options.Validate();
    }

    [TestMethod]
    public void Validate_AllowsMissingToken()
    {
        GoodMoviesInfrastructureOptions options = new() { Token = null };

        options.Validate();
    }
}
