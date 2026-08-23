namespace GoodMovies.Infrastructure;

internal interface IAtomicFileWriter
{
    Task WriteAsync(
        string targetPath,
        Func<Stream, Task> writeContentAsync,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Writes a sibling temporary file, flushes it to disk, and then renames it
/// over the destination. The destination is never removed on a failed write.
/// </summary>
internal sealed class AtomicFileWriter : IAtomicFileWriter
{
    public async Task WriteAsync(
        string targetPath,
        Func<Stream, Task> writeContentAsync,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentNullException.ThrowIfNull(writeContentAsync);
        cancellationToken.ThrowIfCancellationRequested();

        string? directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new IOException("The target path must include a directory.");
        }

        Directory.CreateDirectory(directory);
        string temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (
                FileStream stream = new(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 16 * 1024,
                    options: FileOptions.SequentialScan
                )
            )
            {
                await writeContentAsync(stream).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            // Only this operation's sibling temp file is ever cleaned up.
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                // Preserve the original write error, if there was one.
            }
        }
    }
}
