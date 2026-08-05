namespace Senice.Core.Models;

public enum FileStatus
{
    Changed,
    Unchanged,
    Skipped,
    Error,
    Cancelled,
}

public sealed class FileProcessingResult
{
    public required string Path { get; init; }

    public required FileStatus Status { get; init; }

    public string? Language { get; init; }

    public string? DetectedEncoding { get; init; }

    public string? LineEndings { get; init; }

    public bool UsedTreeSitter { get; init; }

    public long OriginalBytes { get; init; }

    public long ResultBytes { get; init; }

    public int CommentsRemoved { get; init; }

    public int EmojisRemoved { get; init; }

    public string? BackupPath { get; init; }

    public string? Error { get; init; }

    public string? SkippedReason { get; init; }

    public TimeSpan Elapsed { get; init; }
}
