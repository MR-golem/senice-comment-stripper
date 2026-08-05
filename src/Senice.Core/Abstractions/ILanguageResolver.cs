using Senice.Core.Models;

namespace Senice.Core.Abstractions;

public interface ILanguageResolver
{
    LanguageProfile? Resolve(string filePath);
}
