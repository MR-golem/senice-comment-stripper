namespace Senice.Core.Models;

public readonly record struct TextRange(int Start, int End)
{
    public int Length => End - Start;

    public bool Contains(int position) => position >= Start && position < End;

    public bool Contains(TextRange other) => other.Start >= Start && other.End <= End;

    public bool Overlaps(TextRange other) => Start < other.End && other.Start < End;
}
