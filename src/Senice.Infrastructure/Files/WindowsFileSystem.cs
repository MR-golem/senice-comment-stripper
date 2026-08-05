using Microsoft.Extensions.Logging;
using Senice.Core.Abstractions;
using Senice.Core.Models;

namespace Senice.Infrastructure.Files;

public sealed class WindowsFileSystem : IFileSystem
{
    private readonly ILogger<WindowsFileSystem> _logger;

    public WindowsFileSystem(ILogger<WindowsFileSystem> logger)
    {
        _logger = logger;
    }

    public bool FileExists(string path) => File.Exists(path);

    public bool IsDirectory(string path) => Directory.Exists(path);

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken)
        => File.ReadAllBytesAsync(path, cancellationToken);

    public async Task WriteAllBytesAsync(string path, byte[] contents, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string tempPath = Path.Combine(directory ?? string.Empty, Path.GetFileName(path) + ".senice.tmp");
        try
        {
            await File.WriteAllBytesAsync(tempPath, contents, cancellationToken);
            Replace(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file {Path}.", tempPath);
                }
            }
        }
    }

    public async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await sourceStream.CopyToAsync(destinationStream, 81920, cancellationToken);
    }

    public IReadOnlyList<string> EnumerateFiles(string rootPath, ProcessingOptions options)
    {
        var result = new List<string>();
        var excluded = options.ExcludedDirectories is { Count: > 0 }
            ? new HashSet<string>(options.ExcludedDirectories, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        EnumerateRecursive(rootPath, options, excluded, result, 0);
        return result;
    }

    private void EnumerateRecursive(string directory, ProcessingOptions options, HashSet<string> excluded, List<string> result, int depth)
    {
        if (depth > options.MaxDepth)
            return;

        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            string name = Path.GetFileName(entry);
            bool isDir = Directory.Exists(entry);

            if (isDir)
            {
                if (excluded.Contains(name) || !options.IncludeHiddenFiles && name.StartsWith("."))
                    continue;
                EnumerateRecursive(entry, options, excluded, result, depth + 1);
                continue;
            }

            if (excluded.Contains(name))
                continue;

            if (IsSupportedFile(entry, options))
                result.Add(entry);
        }
    }

    private static bool IsSupportedFile(string path, ProcessingOptions options)
    {
        string name = Path.GetFileName(path);
        if (name.EndsWith(".senice.bak", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".senice.tmp", StringComparison.OrdinalIgnoreCase))
            return false;

        if (options.MatchAnyFile)
            return true;

        if (options.Extensions is { Count: > 0 })
        {
            string extension = Path.GetExtension(path);
            return options.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        return true;
    }

    private static void Replace(string source, string destination)
    {
        string? destinationDirectory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        if (File.Exists(destination))
            File.Delete(destination);

        File.Move(source, destination);
    }
}
