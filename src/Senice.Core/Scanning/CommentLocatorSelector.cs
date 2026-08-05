using Senice.Core.Abstractions;
using Senice.Core.Models;

namespace Senice.Core.Scanning;

public sealed class CommentLocatorSelector : ICommentLocator
{
    private readonly IReadOnlyList<ICommentLocator> _locators;

    public CommentLocatorSelector(IEnumerable<ICommentLocator> locators)
        => _locators = locators.ToArray();

    public bool CanHandle(LanguageProfile profile) => _locators.Any(locator => locator.CanHandle(profile));

    public SyntaxScanResult Scan(string source, LanguageProfile profile)
    {
        SyntaxScanResult? degraded = null;

        foreach (var locator in _locators)
        {
            if (!locator.CanHandle(profile))
                continue;

            var result = locator.Scan(source, profile);
            if (result.Reliability == ScanReliability.Reliable)
                return result;
            if (result.Reliability == ScanReliability.Degraded && degraded is null)
                degraded = result;
        }

        return degraded ?? SyntaxScanResult.Failed("No usable parser for this language.");
    }
}
