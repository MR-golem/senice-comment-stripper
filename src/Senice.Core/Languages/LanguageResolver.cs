using Senice.Core.Abstractions;
using Senice.Core.Models;

namespace Senice.Core.Languages;

public sealed class LanguageResolver : ILanguageResolver
{
    private readonly LanguageCatalog _catalog;

    public LanguageResolver(LanguageCatalog catalog) => _catalog = catalog;

    public LanguageProfile? Resolve(string filePath) => _catalog.Resolve(filePath);
}
