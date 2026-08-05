using Senice.Core.Abstractions;
using Senice.Core.Models;

namespace Senice.Core.Scanning;

public sealed class LexicalCommentLocator : ICommentLocator
{
    public bool CanHandle(LanguageProfile profile) => profile.SupportsComments;

    public SyntaxScanResult Scan(string source, LanguageProfile profile)
    {
        var comments = new List<CommentSpan>();
        var strings = new List<TextRange>();
        var heredocs = profile.SupportsHeredocs ? FindHeredocs(source) : Array.Empty<TextRange>();
        bool unterminated = false;

        int i = 0;
        int n = source.Length;
        int heredocIndex = 0;

        while (i < n)
        {
            while (heredocIndex < heredocs.Count && heredocs[heredocIndex].End <= i)
                heredocIndex++;

            if (heredocIndex < heredocs.Count && i >= heredocs[heredocIndex].Start)
            {
                strings.Add(heredocs[heredocIndex]);
                i = heredocs[heredocIndex].End;
                continue;
            }

            if (TryReadString(source, profile.StringLiterals, i, out var stringStart, out var stringEnd, out var stringUnterminated))
            {
                strings.Add(new TextRange(stringStart, stringEnd));
                unterminated |= stringUnterminated;
                i = stringEnd;
                continue;
            }

            if (TryReadBlockComment(source, profile, i, out var blockStart, out var blockEnd, out var blockUnterminated))
            {
                comments.Add(new CommentSpan(new TextRange(blockStart, blockEnd), CommentKind.Block));
                unterminated |= blockUnterminated;
                i = blockEnd;
                continue;
            }

            if (TryReadLineComment(source, profile, i, out var lineEnd))
            {
                comments.Add(new CommentSpan(new TextRange(i, lineEnd), CommentKind.Line));
                i = lineEnd;
                continue;
            }

            i++;
        }

        return new SyntaxScanResult
        {
            Comments = comments,
            StringRanges = strings,
            Reliability = unterminated ? ScanReliability.Degraded : ScanReliability.Reliable,
            UsedTreeSitter = false,
            Message = unterminated ? "Unterminated string or block comment; results are best effort." : null,
        };
    }

    private static bool TryReadString(
        string source,
        IReadOnlyList<StringLiteral> literals,
        int position,
        out int start,
        out int end,
        out bool unterminated)
    {
        start = position;
        end = position;
        unterminated = false;

        foreach (var literal in literals)
        {
            if (!StartsWith(source, position, literal.Open))
                continue;

            int j = position + literal.Open.Length;
            int n = source.Length;

            while (j < n)
            {
                if (literal.Escape != '\0' && source[j] == literal.Escape && j + 1 < n)
                {
                    j += 2;
                    continue;
                }

                if (literal.DoubledEscape && StartsWith(source, j, literal.Close) &&
                    j + literal.Close.Length < n && StartsWith(source, j + literal.Close.Length, literal.Close))
                {
                    j += literal.Close.Length * 2;
                    continue;
                }

                if (StartsWith(source, j, literal.Close))
                {
                    end = j + literal.Close.Length;
                    return true;
                }

                if (!literal.MultiLine && (source[j] == '\n' || source[j] == '\r'))
                {
                    end = j;
                    unterminated = true;
                    return true;
                }

                j++;
            }

            end = n;
            unterminated = true;
            return true;
        }

        return false;
    }

    private static bool TryReadBlockComment(
        string source,
        LanguageProfile profile,
        int position,
        out int start,
        out int end,
        out bool unterminated)
    {
        start = position;
        end = position;
        unterminated = false;

        foreach (var pair in profile.BlockComments)
        {
            if (!StartsWith(source, position, pair.Open))
                continue;

            int depth = 1;
            int j = position + pair.Open.Length;
            int n = source.Length;

            while (j < n)
            {
                if (profile.NestedBlockComments && StartsWith(source, j, pair.Open))
                {
                    depth++;
                    j += pair.Open.Length;
                    continue;
                }

                if (StartsWith(source, j, pair.Close))
                {
                    depth--;
                    j += pair.Close.Length;
                    if (depth == 0)
                    {
                        end = j;
                        return true;
                    }
                    continue;
                }

                j++;
            }

            end = n;
            unterminated = true;
            return true;
        }

        return false;
    }

    private static bool TryReadLineComment(
        string source,
        LanguageProfile profile,
        int position,
        out int end)
    {
        end = position;
        bool atLineStart = position == 0 || source[position - 1] == '\n' || source[position - 1] == '\r';
        int n = source.Length;

        foreach (var marker in profile.LineComments)
        {
            if (marker.RequiresLineStart && !atLineStart)
                continue;

            if (!StartsWith(source, position, marker.Marker))
                continue;

            int j = position + marker.Marker.Length;
            while (j < n && source[j] != '\n' && source[j] != '\r')
                j++;

            end = j;
            return true;
        }

        return false;
    }

    private static IReadOnlyList<TextRange> FindHeredocs(string source)
    {
        var ranges = new List<TextRange>();
        int n = source.Length;
        int i = 0;

        while (i < n - 2)
        {
            int open = source.IndexOf("<<", i, StringComparison.Ordinal);
            if (open < 0)
                break;

            int after = open + 2;
            if (after < n && (source[after] == '=' || source[after] == '<' || source[after] == '~'))
            {
                i = after + 1;
                continue;
            }

            int p = after;
            if (p < n && (source[p] == '-' || source[p] == '\\'))
                p++;
            while (p < n && char.IsWhiteSpace(source[p]))
                p++;

            string word;
            int bodyStart;
            if (p < n && (source[p] == '"' || source[p] == '\''))
            {
                char quote = source[p];
                int q = p + 1;
                while (q < n && source[q] != quote)
                    q++;
                if (q >= n)
                {
                    i = after;
                    continue;
                }
                word = source.Substring(p + 1, q - p - 1);
                bodyStart = q + 1;
            }
            else
            {
                int wordStart = p;
                while (p < n && (char.IsLetterOrDigit(source[p]) || source[p] == '_'))
                    p++;
                if (p == wordStart)
                {
                    i = after;
                    continue;
                }
                word = source.Substring(wordStart, p - wordStart);
                bodyStart = p;
            }

            if (word.Length == 0)
            {
                i = after;
                continue;
            }

            int newline = source.IndexOf('\n', bodyStart);
            if (newline < 0)
            {
                i = after;
                continue;
            }

            int search = newline + 1;
            int end = -1;
            while (search < n)
            {
                int lineEnd = source.IndexOf('\n', search);
                if (lineEnd < 0)
                    lineEnd = n;

                int trimmed = search;
                while (trimmed < lineEnd && (source[trimmed] == ' ' || source[trimmed] == '\t' || source[trimmed] == '\r'))
                    trimmed++;

                if (lineEnd - trimmed >= word.Length &&
                    string.CompareOrdinal(source, trimmed, word, 0, word.Length) == 0)
                {
                    int rest = trimmed + word.Length;
                    if (rest >= lineEnd || source[rest] == ';' || source[rest] == ',' || char.IsWhiteSpace(source[rest]))
                    {
                        end = lineEnd;
                        break;
                    }
                }

                search = lineEnd + 1;
            }

            if (end > bodyStart)
                ranges.Add(new TextRange(bodyStart, end));

            i = after;
        }

        return ranges;
    }

    private static bool StartsWith(string source, int position, string value)
    {
        if (position + value.Length > source.Length)
            return false;
        return string.CompareOrdinal(source, position, value, 0, value.Length) == 0;
    }
}
