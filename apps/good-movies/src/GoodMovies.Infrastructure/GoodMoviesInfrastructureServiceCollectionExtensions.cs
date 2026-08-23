using GoodMovies.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GoodMovies.Infrastructure;

public static class GoodMoviesInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGoodMoviesInfrastructure(
        this IServiceCollection services,
        GoodMoviesInfrastructureOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        services.TryAddSingleton<IClock, SystemClock>();
        services.AddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IFileSystemPathProvider>(_ => new FileSystemPathProvider(
            options.StorageDirectory ?? GetDefaultStorageDirectory()
        ));
        services.TryAddSingleton<IAtomicFileWriter, AtomicFileWriter>();
        services.AddTransient<TmdbBearerTokenHandler>();

        services
            .AddHttpClient<TmdbMovieCatalogClient>(httpClient =>
                httpClient.BaseAddress = options.ApiBaseAddress
            )
            .AddHttpMessageHandler<TmdbBearerTokenHandler>();
        services.AddTransient<IMovieCatalogProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<TmdbMovieCatalogClient>()
        );
        services.AddTransient<IMovieTrailerLookup>(serviceProvider =>
            serviceProvider.GetRequiredService<TmdbMovieCatalogClient>()
        );

        services.AddSingleton<IMovieCatalogCache, JsonMovieCatalogCache>();
        services.AddSingleton<IFavoritesStore, JsonFavoritesStore>();
        services.AddSingleton<IMovieCatalogService, MovieCatalogService>();

        return services;
    }

    private static string GetDefaultStorageDirectory()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(root)
            ? Path.Combine(AppContext.BaseDirectory, "GoodMovies")
            : Path.Combine(root, "GoodMovies");
    }
}
