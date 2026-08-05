using Senice.Core.Models;

namespace Senice.Core.Abstractions;

public interface IFileSystem
{
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

    Task WriteAllBytesAsync(string path, byte[] contents, CancellationToken cancellationToken = default);

    Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);

    bool FileExists(string path);

    bool IsDirectory(string path);

    IReadOnlyList<string> EnumerateFiles(string rootPath, ProcessingOptions options);
}
