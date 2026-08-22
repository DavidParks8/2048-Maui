using Microsoft.Extensions.DependencyInjection;

namespace GoodMovies.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGoodMoviesCore(this IServiceCollection services)
    {
        services.AddSingleton<SystemClock>();
        services.AddSingleton<IClock>(static serviceProvider =>
            serviceProvider.GetRequiredService<SystemClock>()
        );
        services.AddSingleton<ReleaseWindowPolicy>();
        services.AddSingleton<MovieSafetyPolicy>();
        services.AddSingleton<TrailerSelectionPolicy>();

        return services;
    }
}

/// <summary>
/// Compatibility entry point retained for the initial Good Movies scaffold.
/// </summary>
public static class GoodMoviesCoreServiceCollectionExtensions
{
    public static IServiceCollection AddGoodMoviesCore(IServiceCollection services) =>
        ServiceCollectionExtensions.AddGoodMoviesCore(services);
}
