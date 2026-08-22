using GoodMovies.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GoodMovies.ViewModels.Tests;

[TestClass]
public sealed class ViewModelsSmokeTests
{
    [TestMethod]
    public void AddGoodMoviesViewModels_ReturnsTheProvidedServices()
    {
        IServiceCollection services = Substitute.For<IServiceCollection>();

        IServiceCollection result = services.AddGoodMoviesViewModels();

        Assert.AreSame(services, result);
    }

    [TestMethod]
    public void AddGoodMoviesViewModels_RegistersCatalogViewModel()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddGoodMoviesViewModels();

        Assert.IsTrue(
            services.Any(descriptor => descriptor.ServiceType == typeof(CatalogViewModel))
        );
    }
}
