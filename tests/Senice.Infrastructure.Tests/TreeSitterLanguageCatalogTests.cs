using Senice.Infrastructure.Scanning;

namespace Senice.Infrastructure.Tests;

public class TreeSitterLanguageCatalogTests
{
    [Fact]
    public void Catalog_LoadsCoreGrammars()
    {
        var catalog = new TreeSitterLanguageCatalog();

        Assert.True(catalog.TryGet("c", out _));
        Assert.True(catalog.TryGet("c-sharp", out _));
        Assert.True(catalog.TryGet("python", out _));
        Assert.True(catalog.TryGet("javascript", out _));
        Assert.True(catalog.TryGet("java", out _));
        Assert.True(catalog.TryGet("typescript", out _));
        Assert.True(catalog.TryGet("php", out _));
    }

    [Fact]
    public void Catalog_DoesNotLoadUnavailableGrammar()
    {
        var catalog = new TreeSitterLanguageCatalog();

        Assert.False(catalog.TryGet("elm", out _));
        Assert.False(catalog.TryGet("xml", out _));
    }

    [Fact]
    public void Catalog_IdentifiesCommentNodes()
    {
        var catalog = new TreeSitterLanguageCatalog();

        Assert.True(catalog.IsCommentNode("comment"));
        Assert.True(catalog.IsCommentNode("line_comment"));
        Assert.True(catalog.IsCommentNode("block_comment"));
        Assert.True(catalog.IsCommentNode("doc_comment"));
        Assert.False(catalog.IsCommentNode("string_literal"));
    }

    [Fact]
    public void Catalog_IdentifiesStringNodes()
    {
        var catalog = new TreeSitterLanguageCatalog();

        Assert.True(catalog.IsStringNode("string"));
        Assert.True(catalog.IsStringNode("string_literal"));
        Assert.True(catalog.IsStringNode("template_string"));
        Assert.False(catalog.IsStringNode("identifier"));
    }
}
