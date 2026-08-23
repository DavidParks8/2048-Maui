namespace GoodMovies.Infrastructure;

internal sealed class FileSystemPathProvider : IFileSystemPathProvider
{
    private readonly string _rootDirectory;

    public FileSystemPathProvider(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("A root directory is required.", nameof(rootDirectory));
        }

        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public string GetPath(string fileName)
    {
        if (!IsFileName(fileName))
        {
            throw new ArgumentException("Only a file name is allowed.", nameof(fileName));
        }

        return Path.Combine(_rootDirectory, fileName);
    }

    internal static bool IsFileName(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("/", StringComparison.Ordinal)
        && !value.Contains("\\", StringComparison.Ordinal)
        && string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal);
}
