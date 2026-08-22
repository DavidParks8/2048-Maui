using GoodMovies.Core;
using GoodMovies.Infrastructure;
using GoodMovies.Maui.Services;
using GoodMovies.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
#if IOS
using GoodMovies.Maui.Platforms.iOS;
#endif

#if DEBUG && GOOD_MOVIES_SAMPLE_DATA
using GoodMovies.Maui.Development;
#endif

namespace GoodMovies.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>().ConfigureFonts(_ => { });

#if IOS
        // The .NET 10 grouped CollectionView handler can index a section after
        // its group is removed. The compatibility handler does not have that
        // crash and is safer for the favorites feed.
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<
                Microsoft.Maui.Controls.CollectionView,
                Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler
            >();
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<
            IGoodMoviesFilePathProvider,
            MauiGoodMoviesFilePathProvider
        >();
        builder.Services.AddSingleton<IFileSystemPathProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<IGoodMoviesFilePathProvider>()
        );
        builder.Services.AddSingleton<IFilePathProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<IGoodMoviesFilePathProvider>() as IFilePathProvider
            ?? throw new InvalidOperationException("The Good Movies file path provider is invalid.")
        );
        builder.Services.AddGoodMoviesInfrastructure(
            new GoodMoviesInfrastructureOptions
            {
                Token = TmdbBuildConfiguration.ReadAccessToken,
                StorageDirectory = FileSystem.AppDataDirectory,
                CacheLifetime = TimeSpan.FromHours(6),
            }
        );
        builder.Services.AddSingleton<INetworkStatusService, MauiNetworkStatusService>();
        builder.Services.AddSingleton<IScreenReaderService, MauiScreenReaderService>();
        builder.Services.AddGoodMoviesViewModels();
        builder.Services.AddSingleton<IMovieDetailPageFactory, MauiMovieDetailPageFactory>();
        builder.Services.TryAddSingleton<
            IMovieDetailNavigationHost,
            MauiMovieDetailNavigationHost
        >();
        builder.Services.TryAddSingleton<MauiNavigationService>();
        builder.Services.TryAddSingleton<INavigationService>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiNavigationService>()
        );
        builder.Services.TryAddSingleton<IMovieNavigationService>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiNavigationService>()
        );
#if IOS
        builder.Services.AddSingleton<IosWordLevelSpeechService>();
        builder.Services.AddSingleton<IWordLevelSpeechService>(serviceProvider =>
            serviceProvider.GetRequiredService<IosWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<IWordSpeechService>(serviceProvider =>
            serviceProvider.GetRequiredService<IosWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<ISpeechService>(serviceProvider =>
            serviceProvider.GetRequiredService<IosWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<IReadAloudService>(serviceProvider =>
            serviceProvider.GetRequiredService<IosWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<ITextToSpeechService>(serviceProvider =>
            serviceProvider.GetRequiredService<IosWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<IWordLevelSpeech>(serviceProvider =>
            serviceProvider.GetRequiredService<IosWordLevelSpeechService>()
        );
#else
        builder.Services.AddSingleton<IWordLevelSpeechService, MauiNoopWordLevelSpeechService>();
        builder.Services.AddSingleton<IWordSpeechService>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiNoopWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<ISpeechService>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiNoopWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<IReadAloudService>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiNoopWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<ITextToSpeechService>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiNoopWordLevelSpeechService>()
        );
        builder.Services.AddSingleton<IWordLevelSpeech>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiNoopWordLevelSpeechService>()
        );
#endif
        builder.Services.AddSingleton<INativeUriLauncher>(_ => new MauiNativeUriLauncher());
        builder.Services.AddSingleton<MauiExternalTrailerLauncher>();
        builder.Services.AddSingleton<IExternalTrailerLauncher>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiExternalTrailerLauncher>()
        );
        builder.Services.AddSingleton<ITrailerLauncher>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiExternalTrailerLauncher>()
        );
        builder.Services.AddSingleton<IYouTubeTrailerLauncher>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiExternalTrailerLauncher>()
        );
        builder.Services.AddSingleton<IExternalLinkLauncher>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiExternalTrailerLauncher>()
        );
        builder.Services.AddSingleton<IExternalLauncher>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiExternalTrailerLauncher>()
        );
        builder.Services.AddSingleton<IExternalTrailerService>(serviceProvider =>
            serviceProvider.GetRequiredService<MauiExternalTrailerLauncher>()
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
        builder.Services.Replace(
            ServiceDescriptor.Singleton<IMovieTrailerService, SampleTrailerLookup>()
        );
        builder.Services.Replace(
            ServiceDescriptor.Singleton<ITrailerLookup, SampleTrailerLookup>()
        );
#endif

        return builder.Build();
    }
}
