using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GoodMovies.ViewModels;

public static class GoodMoviesViewModelsServiceCollectionExtensions
{
    public static IServiceCollection AddGoodMoviesViewModels(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<CatalogViewModel>();
        services.TryAddSingleton<NavigationViewModel>();
        return services;
    }
}
