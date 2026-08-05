using Senice.Core.Models;

namespace Senice.Core.Rewriting;

public sealed class ShebangGuard
{
    public bool IsShebang(CommentSpan comment, string source, LanguageProfile profile, ProcessingOptions options)
    {
        if (!options.PreserveShebang || profile.ShebangMarker is null || comment.Range.Start != 0)
            return false;

        return source.StartsWith(profile.ShebangMarker + "!", StringComparison.Ordinal);
    }
}
