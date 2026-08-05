using Microsoft.Extensions.Logging;
using Senice.Core.Abstractions;
using Senice.Core.Models;

namespace Senice.Core.Processing;

public sealed class ProcessingService
{
    private readonly FileProcessor _processor;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ProcessingService> _logger;

    public ProcessingService(
        FileProcessor processor,
        IFileSystem fileSystem,
        ILogger<ProcessingService> logger)
    {
        _processor = processor;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<ProcessingSummary> ProcessFilesAsync(
        IEnumerable<string> paths,
        ProcessingOptions options,
        IProgress<ProcessingProgress>? progress = null,
        IProgress<ProcessingEvent>? events = null,
        CancellationToken cancellationToken = default)
    {
        var files = new List<string>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_fileSystem.IsDirectory(path))
            {
                files.AddRange(_fileSystem.EnumerateFiles(path, options));
            }
            else if (_fileSystem.FileExists(path))
            {
                files.Add(path);
            }
        }

        files = files.Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int total = files.Count;
        int processed = 0;
        int changed = 0;
        int unchanged = 0;
        int skipped = 0;
        int cancelled = 0;
        int errors = 0;
        long bytesSaved = 0;
        var results = new List<FileProcessingResult>(total);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await Parallel.ForEachAsync(files, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount),
        }, async (file, token) =>
        {
            var result = await _processor.ProcessAsync(file, options, token);

            lock (results)
            {
                results.Add(result);
                processed++;
                switch (result.Status)
                {
                    case FileStatus.Changed:
                        changed++;
                        bytesSaved += Math.Max(0, result.OriginalBytes - result.ResultBytes);
                        break;
                    case FileStatus.Unchanged:
                        unchanged++;
                        break;
                    case FileStatus.Skipped:
                        skipped++;
                        break;
                    case FileStatus.Error:
                        errors++;
                        _logger.LogWarning("Failed to process {File}: {Error}", file, result.Error);
                        events?.Report(new ProcessingEvent(file, result.Error ?? "Unknown error.", ProcessingEventLevel.Error, DateTime.Now));
                        break;
                    case FileStatus.Cancelled:
                        cancelled++;
                        break;
                }
            }

            progress?.Report(new ProcessingProgress(processed, total, file));
        });

        stopwatch.Stop();

        return new ProcessingSummary
        {
            Total = total,
            Changed = changed,
            Unchanged = unchanged,
            Skipped = skipped,
            Errors = errors,
            Cancelled = cancelled,
            BytesSaved = bytesSaved,
            Elapsed = stopwatch.Elapsed,
            Results = results,
        };
    }
}
