using Senice.Core.Abstractions;
using Senice.Core.Languages;
using Senice.Core.Models;
using Senice.Core.Scanning;

namespace Senice.Core.Tests;

public class LexicalCommentLocatorTests
{
    private readonly LexicalCommentLocator _locator = new();
    private readonly LanguageCatalog _catalog = new();

    private LanguageProfile Resolve(string fileName) => _catalog.Resolve(fileName)!;

    private static SyntaxScanResult Scan(string source, string fileName, LanguageCatalog catalog, LexicalCommentLocator locator)
    {
        var profile = catalog.Resolve(fileName)!;
        return locator.Scan(source, profile);
    }

    [Fact]
    public void CSharp_LineAndBlockComments_AreFound()
    {
        const string source = "// first\nint x = 1; // second\n/* block */";
        var result = Scan(source, "test.cs", _catalog, _locator);

        Assert.Equal(3, result.Comments.Count);
        Assert.Equal(ScanReliability.Reliable, result.Reliability);
        Assert.Equal("// first", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void CommentInsideString_IsNotDetected()
    {
        const string source = "var s = \"// not a comment\";\n// real\n";
        var result = Scan(source, "test.cs", _catalog, _locator);

        Assert.Single(result.Comments);
        Assert.Equal("// real", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void BlockCommentInsideString_IsNotDetected()
    {
        const string source = "var s = \"/* nope */\";\n/* yes */\n";
        var result = Scan(source, "test.cs", _catalog, _locator);

        Assert.Single(result.Comments);
        Assert.Equal("/* yes */", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void CSharp_BlockComment_ClosesAtFirstDelimiter()
    {
        const string source = "/* a /* b */ c */";
        var result = Scan(source, "test.cs", _catalog, _locator);

        Assert.Single(result.Comments);
        Assert.Equal("/* a /* b */", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void Rust_NestedBlockComment_IsSingleSpan()
    {
        const string source = "/* outer /* inner */ done */";
        var result = Scan(source, "test.rs", _catalog, _locator);

        Assert.Single(result.Comments);
        Assert.Equal("/* outer /* inner */ done */", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void Python_HashCommentsAndTripleStrings()
    {
        const string source = "# one\ns = '''# not a comment'''\nx = 1  # two\n";
        var result = Scan(source, "test.py", _catalog, _locator);

        Assert.Equal(2, result.Comments.Count);
        Assert.Single(result.StringRanges);
    }

    [Fact]
    public void Python_HeredocStyle_IsStringNotComment()
    {
        const string source = "s = \"\"\"\n# not a comment\n\"\"\"\n# real\n";
        var result = Scan(source, "test.py", _catalog, _locator);

        Assert.Single(result.Comments);
        Assert.Equal("# real", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void UnterminatedString_MarksDegraded()
    {
        const string source = "x = \"unterminated\n";
        var result = Scan(source, "test.py", _catalog, _locator);

        Assert.Equal(ScanReliability.Degraded, result.Reliability);
    }

    [Fact]
    public void Html_BlockComment_IsFound()
    {
        const string source = "<div><!-- note --></div>";
        var result = Scan(source, "test.html", _catalog, _locator);

        Assert.Single(result.Comments);
        Assert.Equal("<!-- note -->", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }

    [Fact]
    public void Batch_RemAtLineStart_IsFound()
    {
        const string source = "echo hi\r\nREM a comment\r\n:: another\r\n";
        var result = Scan(source, "test.bat", _catalog, _locator);

        Assert.Equal(2, result.Comments.Count);
    }

    [Fact]
    public void Batch_RemMidLine_IsNotComment()
    {
        const string source = "echo hello REM not a comment\n";
        var result = Scan(source, "test.bat", _catalog, _locator);

        Assert.Empty(result.Comments);
    }

    [Fact]
    public void Sql_DoubledQuote_IsInsideString()
    {
        const string source = "SELECT 'it''s -- fine' FROM t;\n-- real\n";
        var result = Scan(source, "test.sql", _catalog, _locator);

        Assert.Single(result.Comments);
        Assert.Equal("-- real", source[result.Comments[0].Range.Start..result.Comments[0].Range.End]);
    }
}
