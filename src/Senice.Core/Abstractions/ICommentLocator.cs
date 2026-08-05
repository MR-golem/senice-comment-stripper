using Senice.Core.Models;

namespace Senice.Core.Abstractions;

public enum ScanReliability
{
    Reliable,
    Degraded,
    Failed,
}

public sealed class SyntaxScanResult
{
    public IReadOnlyList<CommentSpan> Comments { get; init; } = Array.Empty<CommentSpan>();

    public IReadOnlyList<TextRange> StringRanges { get; init; } = Array.Empty<TextRange>();

    public ScanReliability Reliability { get; init; }

    public bool UsedTreeSitter { get; init; }

    public string? Message { get; init; }

    public static SyntaxScanResult Failed(string message) => new()
    {
        Reliability = ScanReliability.Failed,
        Message = message,
    };
}

public interface ICommentLocator
{
    bool CanHandle(LanguageProfile profile);

    SyntaxScanResult Scan(string source, LanguageProfile profile);
}
