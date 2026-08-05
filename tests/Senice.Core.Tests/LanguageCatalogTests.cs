using Senice.Core.Languages;

namespace Senice.Core.Tests;

public class LanguageCatalogTests
{
    private readonly LanguageCatalog _catalog = new();

    [Theory]
    [InlineData("Program.cs", "csharp")]
    [InlineData("app.py", "python")]
    [InlineData("main.go", "go")]
    [InlineData("index.js", "javascript")]
    [InlineData("main.rs", "rust")]
    [InlineData("style.css", "css")]
    [InlineData("page.html", "html")]
    [InlineData("web.xml", "xml")]
    [InlineData("data.json", "json")]
    [InlineData("Dockerfile", "dockerfile")]
    [InlineData("Makefile", "makefile")]
    [InlineData(".gitignore", "gitignore")]
    [InlineData("Jenkinsfile", "jenkinsfile")]
    [InlineData("build.gradle", "groovy")]
    public void Resolve_ByExtensionAndFileName(string fileName, string expectedId)
    {
        var profile = _catalog.Resolve(fileName);

        Assert.NotNull(profile);
        Assert.Equal(expectedId, profile.Id);
    }

    [Fact]
    public void Resolve_UnknownExtension_ReturnsNull()
    {
        Assert.Null(_catalog.Resolve("mystery.xyz"));
    }

    [Fact]
    public void Catalog_CoversManyLanguages()
    {
        Assert.True(_catalog.KnownExtensions.Count > 100);
    }

    [Fact]
    public void Profiles_SupportComments()
    {
        var profile = _catalog.Resolve("Program.cs")!;
        Assert.True(profile.SupportsComments);
    }
}
