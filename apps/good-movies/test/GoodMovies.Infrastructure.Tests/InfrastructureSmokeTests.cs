using GoodMovies.Infrastructure;

namespace GoodMovies.Infrastructure.Tests;

[TestClass]
public sealed class InfrastructureSmokeTests
{
    [TestMethod]
    public void InfrastructureAssemblyLoads()
    {
        Assert.IsNotNull(typeof(GoodMoviesInfrastructureServiceCollectionExtensions).Assembly);
    }
}
