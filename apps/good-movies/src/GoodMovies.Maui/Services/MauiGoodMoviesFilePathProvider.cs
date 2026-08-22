using GoodMovies.Infrastructure;
using Microsoft.Maui.Storage;

namespace GoodMovies.Maui.Services;

/// <summary>
/// Uses the application-private MAUI data directory for Good Movies caches.
/// Filename validation remains centralized in the Infrastructure provider.
/// </summary>
public sealed class MauiGoodMoviesFilePathProvider : GoodMoviesFilePathProvider, IFilePathProvider
{
    public MauiGoodMoviesFilePathProvider()
        : base(FileSystem.AppDataDirectory) { }
}
