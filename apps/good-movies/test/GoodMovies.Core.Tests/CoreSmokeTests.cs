using GoodMovies.Core;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class CoreSmokeTests
{
    [TestMethod]
    public void CoreAssemblyLoads()
    {
        Assert.IsNotNull(typeof(GoodMoviesCoreServiceCollectionExtensions).Assembly);
    }
}
