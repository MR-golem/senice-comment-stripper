namespace Senice.Core.Models;

public sealed class ProcessingSummary
{
    public int Total { get; init; }

    public int Changed { get; init; }

    public int Unchanged { get; init; }

    public int Skipped { get; init; }

    public int Errors { get; init; }

    public int Cancelled { get; init; }

    public long BytesSaved { get; init; }

    public TimeSpan Elapsed { get; init; }

    public IReadOnlyList<FileProcessingResult> Results { get; init; } = Array.Empty<FileProcessingResult>();
}
