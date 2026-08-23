using GoodMovies.Core;
using GoodMovies.Infrastructure;
using GoodMovies.Maui.Controls;
using GoodMovies.Maui.Platforms.iOS;
using GoodMovies.Maui.Services;
using GoodMovies.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
#if DEBUG && GOOD_MOVIES_SAMPLE_DATA
using GoodMovies.Maui.Development;
#endif

namespace GoodMovies.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

        // The .NET 10 grouped CollectionView handler can index a section after
        // its group is removed. The compatibility handler does not have that
        // crash and is safer for the favorites feed.
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<
                Microsoft.Maui.Controls.CollectionView,
                Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler
            >();
            handlers.AddHandler<TrailerPlayerView, TrailerPlayerViewHandler>();
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddGoodMoviesInfrastructure(
            new GoodMoviesInfrastructureOptions
            {
                Token = TmdbBuildConfiguration.ReadAccessToken,
                StorageDirectory = FileSystem.AppDataDirectory,
                CacheLifetime = TimeSpan.FromHours(6),
            }
        );
        builder.Services.AddSingleton<INetworkStatusService, MauiNetworkStatusService>();
        builder.Services.AddSingleton<MauiScreenReaderService>();
        builder.Services.AddSingleton<CatalogViewModel>();
        builder.Services.AddSingleton<MovieDetailPage>();
        builder.Services.AddSingleton<INavigationService, MauiNavigationService>();
        builder.Services.AddSingleton<IWordLevelSpeechService, IosWordLevelSpeechService>();

        builder.Services.AddSingleton<MauiTrailerLauncher>();
        builder.Services.AddSingleton<ITrailerLauncher>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiTrailerLauncher>()
        );
        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<Func<AppShell>>(serviceProvider =>
            () => serviceProvider.GetRequiredService<AppShell>()
        );

#if DEBUG && GOOD_MOVIES_SAMPLE_DATA
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IMovieCatalogService, SampleMovieCatalogService>()
        );
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IMovieTrailerLookup, SampleTrailerLookup>()
        );
#endif

        MauiApp app = builder.Build();
        TrailerPlayerDiagnostics.Configure(app.Services.GetRequiredService<ILoggerFactory>());
        return app;
    }
}
