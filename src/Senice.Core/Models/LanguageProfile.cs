namespace Senice.Core.Models;

public sealed record LineCommentMarker(string Marker, bool RequiresLineStart = false);

public sealed record BlockComment(string Open, string Close);

public sealed record StringLiteral(
    string Open,
    string Close,
    char Escape = '\0',
    bool DoubledEscape = false,
    bool MultiLine = false);

public sealed class LanguageProfile
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<string> Extensions { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> FileNames { get; init; } = Array.Empty<string>();

    public IReadOnlyList<LineCommentMarker> LineComments { get; init; } = Array.Empty<LineCommentMarker>();

    public IReadOnlyList<BlockComment> BlockComments { get; init; } = Array.Empty<BlockComment>();

    public IReadOnlyList<StringLiteral> StringLiterals { get; init; } = Array.Empty<StringLiteral>();

    public bool NestedBlockComments { get; init; }

    public bool SupportsHeredocs { get; init; }

    public string? ShebangMarker { get; init; }

    public string? TreeSitterGrammar { get; init; }

    public bool SupportsComments => LineComments.Count > 0 || BlockComments.Count > 0;
}
