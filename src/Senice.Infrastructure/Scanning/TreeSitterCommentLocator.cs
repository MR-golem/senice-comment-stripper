using Microsoft.Extensions.Logging;
using Senice.Core.Abstractions;
using Senice.Core.Models;
using Senice.Core.Scanning;
using TreeSitter;

namespace Senice.Infrastructure.Scanning;

public sealed class TreeSitterCommentLocator : ICommentLocator
{
    private readonly TreeSitterLanguageCatalog _catalog;
    private readonly LexicalCommentLocator _lexical;
    private readonly ILogger<TreeSitterCommentLocator> _logger;

    public TreeSitterCommentLocator(
        TreeSitterLanguageCatalog catalog,
        LexicalCommentLocator lexical,
        ILogger<TreeSitterCommentLocator> logger)
    {
        _catalog = catalog;
        _lexical = lexical;
        _logger = logger;
    }

    public bool CanHandle(LanguageProfile profile) =>
        !string.IsNullOrEmpty(profile.TreeSitterGrammar) && _catalog.TryGet(profile.TreeSitterGrammar, out _);

    public SyntaxScanResult Scan(string source, LanguageProfile profile)
    {
        if (string.IsNullOrEmpty(profile.TreeSitterGrammar) || !_catalog.TryGet(profile.TreeSitterGrammar, out var language))
            return _lexical.Scan(source, profile);

        try
        {
            return ScanWithTreeSitter(source, language, profile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tree-sitter parse failed for {Language}; falling back to lexical scanning.", profile.Name);
            return _lexical.Scan(source, profile);
        }
    }

    private SyntaxScanResult ScanWithTreeSitter(string source, Language language, LanguageProfile profile)
    {
        var parser = new Parser();
        parser.Language = language;
        var tree = parser.Parse(source);
        if (tree is null)
            return DegradedFallback(source, profile, false);

        var root = tree.RootNode;

        if (root.HasError || root.Type == "ERROR" || root.Children.Count == 0)
            return DegradedFallback(source, profile, root.HasError);

        var comments = new List<CommentSpan>();
        var strings = new List<TextRange>();
        CollectNodes(root, comments, strings);

        if (comments.Count == 0)
            return DegradedFallback(source, profile, false);

        return new SyntaxScanResult
        {
            Reliability = ScanReliability.Reliable,
            UsedTreeSitter = true,
            Comments = comments,
            StringRanges = strings,
        };
    }

    private SyntaxScanResult DegradedFallback(string source, LanguageProfile profile, bool rootHadError)
    {
        var lexical = _lexical.Scan(source, profile);
        if (lexical.Reliability == ScanReliability.Reliable)
        {
            return new SyntaxScanResult
            {
                Reliability = ScanReliability.Reliable,
                UsedTreeSitter = true,
                Comments = lexical.Comments,
                StringRanges = lexical.StringRanges,
                Message = rootHadError ? "Tree-sitter found parse errors; used lexical scan." : null,
            };
        }

        return new SyntaxScanResult
        {
            Reliability = lexical.Reliability,
            UsedTreeSitter = true,
            Comments = lexical.Comments,
            StringRanges = lexical.StringRanges,
            Message = lexical.Message,
        };
    }

    private void CollectNodes(Node node, List<CommentSpan> comments, List<TextRange> strings)
    {
        if (_catalog.IsCommentNode(node.Type))
        {
            comments.Add(new CommentSpan(new TextRange(node.StartIndex, node.EndIndex), CommentKind.Block));
            return;
        }

        if (_catalog.IsStringNode(node.Type))
            strings.Add(new TextRange(node.StartIndex, node.EndIndex));

        foreach (var child in node.Children)
            CollectNodes(child, comments, strings);
    }
}
