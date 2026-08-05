using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Senice.Core.Abstractions;
using Senice.Core.Emoji;
using Senice.Core.Encodings;
using Senice.Core.Models;
using Senice.Core.Rewriting;

namespace Senice.Core.Processing;

public sealed class FileProcessor
{
    private readonly ILanguageResolver _resolver;
    private readonly ICommentLocator _locator;
    private readonly IEncodingService _encoding;
    private readonly IFileSystem _fileSystem;
    private readonly IBackupService _backup;
    private readonly ILogger<FileProcessor> _logger;
    private readonly SourceRewriter _rewriter = new();
    private readonly EmojiScanner _emoji = new();
    private readonly ShebangGuard _shebang = new();

    public FileProcessor(
        ILanguageResolver resolver,
        ICommentLocator locator,
        IEncodingService encoding,
        IFileSystem fileSystem,
        IBackupService backup,
        ILogger<FileProcessor> logger)
    {
        _resolver = resolver;
        _locator = locator;
        _encoding = encoding;
        _fileSystem = fileSystem;
        _backup = backup;
        _logger = logger;
    }

    public async Task<FileProcessingResult> ProcessAsync(string path, ProcessingOptions options, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        byte[] bytes;

        try
        {
            bytes = await _fileSystem.ReadAllBytesAsync(path, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Cancel(path, stopwatch);
        }
        catch (Exception ex)
        {
            return Fail(path, ex);
        }

        if (bytes.Length > options.MaxFileSize)
            return Skip(path, $"File exceeds the {options.MaxFileSize} byte size limit.");

        if (LooksBinary(bytes))
            return Skip(path, "File appears to be binary.");

        DecodedText decoded;
        try
        {
            decoded = _encoding.Decode(bytes);
        }
        catch (Exception ex)
        {
            return Fail(path, ex);
        }

        var profile = _resolver.Resolve(path);
        if (profile is null)
            return Skip(path, "Language is not recognized.");

        SyntaxScanResult scan;
        try
        {
            scan = _locator.Scan(decoded.Text, profile);
        }
        catch (OperationCanceledException)
        {
            return Cancel(path, stopwatch);
        }
        catch (Exception ex)
        {
            return Fail(path, ex);
        }

        if (scan.Reliability == ScanReliability.Failed)
            return Skip(path, scan.Message ?? "Parser failed.");

        if (scan.Reliability == ScanReliability.Degraded && options.SkipUncertainFiles)
            return Skip(path, scan.Message ?? "Parse results were uncertain.");

        var (operations, emojiCount) = BuildOperations(scan, options, decoded.Text, profile);
        string result = _rewriter.Apply(decoded.Text, operations);

        if (string.Equals(result, decoded.Text, StringComparison.Ordinal))
        {
            stopwatch.Stop();
            return new FileProcessingResult
            {
                Path = path,
                Status = FileStatus.Unchanged,
                Language = profile.Name,
                DetectedEncoding = decoded.Encoding.DisplayName,
                LineEndings = LineEndingDetector.Detect(decoded.Text),
                UsedTreeSitter = scan.UsedTreeSitter,
                OriginalBytes = bytes.Length,
                ResultBytes = bytes.Length,
                Elapsed = stopwatch.Elapsed,
            };
        }

        byte[] output;
        try
        {
            output = _encoding.Encode(result, decoded.Encoding);
        }
        catch (Exception ex)
        {
            return Fail(path, ex);
        }

        string? backupPath = null;
        if (options.BackupBeforeWrite)
        {
            try
            {
                backupPath = await _backup.BackupAsync(path, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Cancel(path, stopwatch);
            }
            catch (Exception ex)
            {
                return Fail(path, ex);
            }
        }

        try
        {
            await _fileSystem.WriteAllBytesAsync(path, output, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Cancel(path, stopwatch);
        }
        catch (Exception ex)
        {
            return Fail(path, ex);
        }

        stopwatch.Stop();
        return new FileProcessingResult
        {
            Path = path,
            Status = FileStatus.Changed,
            Language = profile.Name,
            DetectedEncoding = decoded.Encoding.DisplayName,
            LineEndings = LineEndingDetector.Detect(decoded.Text),
            UsedTreeSitter = scan.UsedTreeSitter,
            OriginalBytes = bytes.Length,
            ResultBytes = output.Length,
            CommentsRemoved = scan.Comments.Count,
            EmojisRemoved = emojiCount,
            BackupPath = backupPath,
            Elapsed = stopwatch.Elapsed,
        };
    }

    private (IReadOnlyList<EditOperation> Operations, int EmojiCount) BuildOperations(
        SyntaxScanResult scan,
        ProcessingOptions options,
        string source,
        LanguageProfile profile)
    {
        var operations = new List<EditOperation>();
        int emojiCount = 0;

        foreach (var comment in scan.Comments)
        {
            if (_shebang.IsShebang(comment, source, profile, options))
                continue;

            if (options.RemoveComments)
            {
                operations.Add(new EditOperation(comment.Range, EditKind.PreserveNewLines));
            }
            else if (options.RemoveEmojisInComments)
            {
                foreach (var emoji in _emoji.ScanInRange(source, comment.Range))
                {
                    operations.Add(new EditOperation(emoji, EditKind.Delete));
                    emojiCount++;
                }
            }
        }

        if (options.RemoveEmojisInStrings)
        {
            foreach (var range in scan.StringRanges)
            {
                foreach (var emoji in _emoji.ScanInRange(source, range))
                {
                    operations.Add(new EditOperation(emoji, EditKind.Delete));
                    emojiCount++;
                }
            }
        }

        return (operations, emojiCount);
    }

    private static bool LooksBinary(byte[] bytes)
    {
        int limit = Math.Min(bytes.Length, 4096);
        for (int i = 0; i < limit; i++)
        {
            if (bytes[i] == 0)
                return true;
        }

        return false;
    }

    private static FileProcessingResult Skip(string path, string reason) => new()
    {
        Path = path,
        Status = FileStatus.Skipped,
        SkippedReason = reason,
    };

    private static FileProcessingResult Fail(string path, Exception exception) => new()
    {
        Path = path,
        Status = FileStatus.Error,
        Error = exception.Message,
    };

    private static FileProcessingResult Cancel(string path, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        return new FileProcessingResult
        {
            Path = path,
            Status = FileStatus.Cancelled,
            Elapsed = stopwatch.Elapsed,
        };
    }
}
