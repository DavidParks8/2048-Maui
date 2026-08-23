namespace GoodMovies.Infrastructure;

internal interface IFileSystemPathProvider
{
    string GetPath(string fileName);
}
