using GoodMovies.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GoodMovies.Infrastructure;

public static class GoodMoviesInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGoodMoviesInfrastructure(this IServiceCollection services)
    {
        return AddGoodMoviesInfrastructure(services, static _ => { });
    }

    public static IServiceCollection AddGoodMoviesInfrastructure(
        this IServiceCollection services,
        Action<GoodMoviesInfrastructureOptions> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        GoodMoviesInfrastructureOptions options = new();
        configure(options);
        return AddGoodMoviesInfrastructure(services, options);
    }

    public static IServiceCollection AddGoodMoviesInfrastructure(
        this IServiceCollection services,
        GoodMoviesInfrastructureOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        services.AddGoodMoviesCore();
        services.AddSingleton(options);
        services.TryAddSingleton<IGoodMoviesTimeProvider, SystemGoodMoviesTimeProvider>();
        services.TryAddSingleton<IGoodMoviesTokenProvider, OptionsGoodMoviesTokenProvider>();
        services.TryAddSingleton<ITmdbTokenProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<IGoodMoviesTokenProvider>() as ITmdbTokenProvider
            ?? new OptionsGoodMoviesTokenProvider(
                serviceProvider.GetRequiredService<GoodMoviesInfrastructureOptions>()
            )
        );
        services.TryAddSingleton<IFileSystemPathProvider>(serviceProvider =>
            string.IsNullOrWhiteSpace(options.StorageDirectory)
                ? new DefaultGoodMoviesFilePathProvider()
                : new GoodMoviesFilePathProvider(options.StorageDirectory)
        );
        services.TryAddSingleton<IGoodMoviesFilePathProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<IFileSystemPathProvider>()
                as IGoodMoviesFilePathProvider
            ?? new GoodMoviesFilePathProvider(
                string.IsNullOrWhiteSpace(options.StorageDirectory)
                    ? new DefaultGoodMoviesFilePathProvider().RootDirectory
                    : options.StorageDirectory
            )
        );
        services.TryAddSingleton<IAtomicFileWriter, AtomicFileWriter>();
        services.TryAddSingleton<IPosterUrlBuilder, PosterUrlBuilder>();
        services.AddTransient<TmdbBearerTokenHandler>();

        services
            .AddHttpClient<TmdbMovieCatalogClient>(
                (serviceProvider, httpClient) =>
                {
                    httpClient.BaseAddress = serviceProvider
                        .GetRequiredService<GoodMoviesInfrastructureOptions>()
                        .ApiBaseAddress;
                }
            )
            .AddHttpMessageHandler<TmdbBearerTokenHandler>();
        services.AddTransient<ITmdbMovieCatalogClient>(serviceProvider =>
            serviceProvider.GetRequiredService<TmdbMovieCatalogClient>()
        );
        services.AddTransient<IMovieCatalogProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<ITmdbMovieCatalogClient>()
        );
        services.AddTransient<IMovieCatalogClient>(serviceProvider =>
            serviceProvider.GetRequiredService<ITmdbMovieCatalogClient>()
        );
        services.AddTransient<IMovieTrailerLookup>(serviceProvider =>
            serviceProvider.GetRequiredService<ITmdbMovieCatalogClient>()
        );
        services.AddTransient<IMovieTrailerService>(serviceProvider =>
            serviceProvider.GetRequiredService<ITmdbMovieCatalogClient>()
        );
        services.AddTransient<ITrailerLookup>(serviceProvider =>
            serviceProvider.GetRequiredService<ITmdbMovieCatalogClient>()
        );

        services.AddSingleton<IMovieCatalogCache, JsonMovieCatalogCache>();
        services.AddSingleton<IFavoritesStore, JsonFavoritesStore>();
        services.AddSingleton<IFavoritesService>(serviceProvider =>
            serviceProvider.GetRequiredService<IFavoritesStore>() as IFavoritesService
            ?? throw new InvalidOperationException("The favorites store is not configured.")
        );
        services.AddSingleton<IFavoritesRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<IFavoritesStore>() as IFavoritesRepository
            ?? throw new InvalidOperationException("The favorites store is not configured.")
        );
        services.AddSingleton<MovieCatalogService>();
        services.AddSingleton<GoodMoviesCatalogService>();
        services.AddSingleton<IMovieCatalogService>(serviceProvider =>
            serviceProvider.GetRequiredService<MovieCatalogService>()
        );

        return services;
    }

    public static IServiceCollection AddGoodMoviesInfrastructure(
        this IServiceCollection services,
        GoodMoviesInfrastructureOptions options,
        IGoodMoviesTokenProvider tokenProvider
    )
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        AddGoodMoviesInfrastructure(services, options);
        services.AddSingleton<IGoodMoviesTokenProvider>(tokenProvider);
        return services;
    }

    public static IServiceCollection AddGoodMoviesInfrastructure(
        this IServiceCollection services,
        Action<GoodMoviesInfrastructureOptions> configure,
        IGoodMoviesTokenProvider tokenProvider
    )
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        AddGoodMoviesInfrastructure(services, configure);
        services.AddSingleton<IGoodMoviesTokenProvider>(tokenProvider);
        return services;
    }
}
