using Senice.Core.Models;
using Senice.Core.Rewriting;

namespace Senice.Core.Tests;

public class SourceRewriterTests
{
    private readonly SourceRewriter _rewriter = new();

    [Fact]
    public void RemoveLineComment_PreservesNewline()
    {
        const string source = "int x = 1;\n// note\nint y = 2;\n";
        var commentStart = source.IndexOf("// note", StringComparison.Ordinal);
        var ops = new List<EditOperation>
        {
            new(new TextRange(commentStart, commentStart + 7), EditKind.PreserveNewLines),
        };

        var result = _rewriter.Apply(source, ops);

        Assert.Equal("int x = 1;\n\nint y = 2;\n", result);
    }

    [Fact]
    public void RemoveBlockComment_PreservesInnerNewlines()
    {
        const string source = "x = 1\n/* a\nb */\ny = 2\n";
        var ops = new List<EditOperation>
        {
            new(new TextRange(6, 16), EditKind.PreserveNewLines),
        };

        var result = _rewriter.Apply(source, ops);

        Assert.Equal("x = 1\n\n\ny = 2\n", result);
        Assert.DoesNotContain("a", result);
        Assert.DoesNotContain("b", result);
    }

    [Fact]
    public void NoOperations_ReturnsSourceUnchanged()
    {
        const string source = "hello world";
        Assert.Equal(source, _rewriter.Apply(source, Array.Empty<EditOperation>()));
    }

    [Fact]
    public void OverlappingOperations_ApplyOnce()
    {
        const string source = "abcde";
        var ops = new List<EditOperation>
        {
            new(new TextRange(1, 3), EditKind.Delete),
            new(new TextRange(2, 4), EditKind.Delete),
        };

        var result = _rewriter.Apply(source, ops);

        Assert.Equal("ae", result);
    }

    [Fact]
    public void DeleteOperations_RemoveText()
    {
        const string source = "abcde";
        var ops = new List<EditOperation>
        {
            new(new TextRange(1, 2), EditKind.Delete),
            new(new TextRange(3, 4), EditKind.Delete),
        };

        var result = _rewriter.Apply(source, ops);

        Assert.Equal("ace", result);
    }
}
