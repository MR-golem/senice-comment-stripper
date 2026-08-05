namespace Senice.Core.Abstractions;

public enum DiffOperation
{
    Equal,
    Insert,
    Delete,
}

public readonly record struct DiffEdit(DiffOperation Operation, string Text, int OriginalIndex, int ModifiedIndex);

public interface IDiffEngine
{
    IReadOnlyList<DiffEdit> Compute(IReadOnlyList<string> original, IReadOnlyList<string> modified);
}
