using Senice.Core.Abstractions;

namespace Senice.Core.Diffing;

public sealed class MyersDiffEngine : IDiffEngine
{
    private const int MaxDepth = 256;
    private const long MaxCells = 4_000_000;

    public IReadOnlyList<DiffEdit> Compute(IReadOnlyList<string> original, IReadOnlyList<string> modified)
    {
        var edits = new List<DiffEdit>();

        if (original.Count + modified.Count > 200_000)
        {
            CoarseDiff(original, modified, edits);
            return edits;
        }

        DiffRange(original, 0, original.Count, modified, 0, modified.Count, edits, 0);
        return edits;
    }

    private static void DiffRange(
        IReadOnlyList<string> a,
        int aLo,
        int aHi,
        IReadOnlyList<string> b,
        int bLo,
        int bHi,
        List<DiffEdit> edits,
        int depth)
    {
        while (aLo < aHi && bLo < bHi && string.Equals(a[aLo], b[bLo], StringComparison.Ordinal))
        {
            aLo++;
            bLo++;
        }

        while (aLo < aHi && bLo < bHi && string.Equals(a[aHi - 1], b[bHi - 1], StringComparison.Ordinal))
        {
            aHi--;
            bHi--;
        }

        int n = aHi - aLo;
        int m = bHi - bLo;

        if (n == 0)
        {
            for (int i = bLo; i < bHi; i++)
                edits.Add(new DiffEdit(DiffOperation.Insert, b[i], -1, i));
            return;
        }

        if (m == 0)
        {
            for (int i = aLo; i < aHi; i++)
                edits.Add(new DiffEdit(DiffOperation.Delete, a[i], i, -1));
            return;
        }

        if (depth >= MaxDepth || (long)n * m > MaxCells)
        {
            for (int i = aLo; i < aHi; i++)
                edits.Add(new DiffEdit(DiffOperation.Delete, a[i], i, -1));
            for (int i = bLo; i < bHi; i++)
                edits.Add(new DiffEdit(DiffOperation.Insert, b[i], -1, i));
            return;
        }

        int max = n + m;
        int delta = n - m;
        int offset = max + 1;
        int size = 2 * max + 3;
        var forward = new int[size];
        var backward = new int[size];
        forward[offset + 1] = 0;
        backward[offset + 1] = 0;

        int leftX = -1;
        int leftY = -1;
        int rightX = -1;
        int rightY = -1;

        for (int d = 0; d <= max; d++)
        {
            for (int k = -d; k <= d; k += 2)
            {
                int x;
                if (k == -d || (k != d && forward[offset + k - 1] < forward[offset + k + 1]))
                    x = forward[offset + k + 1];
                else
                    x = forward[offset + k - 1] + 1;

                int y = x - k;
                int snakeStartX = x;
                int snakeStartY = y;

                while (x < n && y < m && string.Equals(a[aLo + x], b[bLo + y], StringComparison.Ordinal))
                {
                    x++;
                    y++;
                }

                forward[offset + k] = x;

                if (d > 0 && (delta & 1) != 0 && k >= delta - (d - 1) && k <= delta + (d - 1) &&
                    forward[offset + k] + backward[offset + delta - k] >= n)
                {
                    leftX = snakeStartX;
                    leftY = snakeStartY;
                    rightX = x;
                    rightY = y;
                    goto found;
                }
            }

            for (int k = -d; k <= d; k += 2)
            {
                int x;
                if (k == -d || (k != d && backward[offset + k - 1] < backward[offset + k + 1]))
                    x = backward[offset + k + 1];
                else
                    x = backward[offset + k - 1] + 1;

                int y = x - k;
                int snakeStartX = x;
                int snakeStartY = y;

                while (x < n && y < m && string.Equals(a[aHi - 1 - x], b[bHi - 1 - y], StringComparison.Ordinal))
                {
                    x++;
                    y++;
                }

                backward[offset + k] = x;

                if (d > 0 && (delta & 1) == 0 && k >= delta - d && k <= delta + d &&
                    backward[offset + k] + forward[offset + delta - k] >= n)
                {
                    leftX = n - x;
                    leftY = m - y;
                    rightX = n - snakeStartX;
                    rightY = m - snakeStartY;
                    goto found;
                }
            }
        }

        found:
        if (leftX < 0)
        {
            for (int i = aLo; i < aHi; i++)
                edits.Add(new DiffEdit(DiffOperation.Delete, a[i], i, -1));
            for (int i = bLo; i < bHi; i++)
                edits.Add(new DiffEdit(DiffOperation.Insert, b[i], -1, i));
            return;
        }

        DiffRange(a, aLo, aLo + leftX, b, bLo, bLo + leftY, edits, depth + 1);
        DiffRange(a, aLo + rightX, aHi, b, bLo + rightY, bHi, edits, depth + 1);
    }

    private static void CoarseDiff(IReadOnlyList<string> a, IReadOnlyList<string> b, List<DiffEdit> edits)
    {
        for (int i = 0; i < a.Count; i++)
            edits.Add(new DiffEdit(DiffOperation.Delete, a[i], i, -1));
        for (int i = 0; i < b.Count; i++)
            edits.Add(new DiffEdit(DiffOperation.Insert, b[i], -1, i));
    }
}
