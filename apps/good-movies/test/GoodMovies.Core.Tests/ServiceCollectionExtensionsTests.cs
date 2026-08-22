using GoodMovies.Core;
using Microsoft.Extensions.DependencyInjection;

namespace GoodMovies.Core.Tests;

[TestClass]
public sealed class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddGoodMoviesCore_RegistersClockAndStatelessPolicies()
    {
        ServiceCollection services = new();
        services.AddGoodMoviesCore();

        Assert.IsTrue(
            services.Any(descriptor =>
                descriptor.ServiceType == typeof(IClock)
                && descriptor.ImplementationFactory is not null
            )
        );
        Assert.IsTrue(
            services.Any(descriptor =>
                descriptor.ServiceType == typeof(SystemClock)
                && descriptor.ImplementationType == typeof(SystemClock)
            )
        );
        Assert.IsTrue(
            services.Any(descriptor =>
                descriptor.ServiceType == typeof(ReleaseWindowPolicy)
                && descriptor.ImplementationType == typeof(ReleaseWindowPolicy)
            )
        );
        Assert.IsTrue(
            services.Any(descriptor =>
                descriptor.ServiceType == typeof(MovieSafetyPolicy)
                && descriptor.ImplementationType == typeof(MovieSafetyPolicy)
            )
        );
        Assert.IsTrue(
            services.Any(descriptor =>
                descriptor.ServiceType == typeof(TrailerSelectionPolicy)
                && descriptor.ImplementationType == typeof(TrailerSelectionPolicy)
            )
        );
    }
}
