using Microsoft.Extensions.Logging.Abstractions;
using Senice.Core.Abstractions;
using Senice.Core.Languages;
using Senice.Core.Scanning;
using Senice.Infrastructure.Scanning;

namespace Senice.Infrastructure.Tests;

public class TreeSitterCommentLocatorTests
{
    private readonly LanguageCatalog _catalog = new();
    private readonly TreeSitterCommentLocator _locator;

    public TreeSitterCommentLocatorTests()
    {
        _locator = new TreeSitterCommentLocator(
            new TreeSitterLanguageCatalog(),
            new LexicalCommentLocator(),
            NullLogger<TreeSitterCommentLocator>.Instance);
    }

    private SyntaxScanResult Scan(string source, string fileName)
    {
        var profile = _catalog.Resolve(fileName)!;
        return _locator.Scan(source, profile);
    }

    [Fact]
    public void CSharp_CommentsFoundReliably()
    {
        const string source = "// header\npublic class A {\n  // member\n  int x = 1;\n}\n";
        var result = Scan(source, "test.cs");

        Assert.True(result.UsedTreeSitter);
        Assert.Equal(ScanReliability.Reliable, result.Reliability);
        Assert.Equal(2, result.Comments.Count);
        Assert.Equal("// header", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void CSharp_CommentInString_NotDetected()
    {
        const string source = "string s = \"// literal\";\n// real\n";
        var result = Scan(source, "test.cs");

        Assert.Single(result.Comments);
        Assert.Equal("// real", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void Python_CommentsAndStrings()
    {
        const string source = "# header\nx = \"# literal\"\n# footer\n";
        var result = Scan(source, "test.py");

        Assert.True(result.UsedTreeSitter);
        Assert.Equal(ScanReliability.Reliable, result.Reliability);
        Assert.Equal(2, result.Comments.Count);
    }

    [Fact]
    public void Java_LineAndBlockComments()
    {
        const string source = "// line\nclass A { /* block */ }\n";
        var result = Scan(source, "test.java");

        Assert.True(result.UsedTreeSitter);
        Assert.Equal(ScanReliability.Reliable, result.Reliability);
        Assert.Equal(2, result.Comments.Count);
    }

    [Fact]
    public void LanguageWithoutGrammar_FallsBackToLexical()
    {
        const string source = "// line\nREM REM line\n";
        var result = Scan(source, "test.bat");

        Assert.False(result.UsedTreeSitter);
        Assert.Single(result.Comments);
    }

    [Fact]
    public void InvalidSyntax_StillFindsComments()
    {
        const string source = "class {\n// comment\nthis is not valid csharp @@\n";
        var result = Scan(source, "test.cs");

        Assert.Equal(ScanReliability.Reliable, result.Reliability);
        Assert.Contains(result.Comments, c => source[c.Range.Start..c.Range.End] == "// comment");
    }
}
