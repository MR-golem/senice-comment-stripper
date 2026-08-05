using System.Text;

namespace Senice.Core.Rewriting;

public sealed class SourceRewriter
{
    public string Apply(string source, IReadOnlyList<EditOperation> operations)
    {
        if (operations.Count == 0)
            return source;

        var sorted = new List<EditOperation>(operations);
        sorted.Sort((a, b) =>
        {
            int byStart = a.Range.Start.CompareTo(b.Range.Start);
            return byStart != 0 ? byStart : a.Range.End.CompareTo(b.Range.End);
        });

        var builder = new StringBuilder(source.Length);
        int position = 0;

        foreach (var operation in sorted)
        {
            int start = Math.Max(operation.Range.Start, position);
            int end = Math.Min(operation.Range.End, source.Length);
            if (end <= start)
                continue;

            builder.Append(source, position, start - position);

            if (operation.Kind == EditKind.PreserveNewLines)
            {
                for (int i = start; i < end; i++)
                {
                    if (source[i] is '\r' or '\n')
                        builder.Append(source[i]);
                }
            }

            position = end;
        }

        if (position < source.Length)
            builder.Append(source, position, source.Length - position);

        return builder.ToString();
    }
}
