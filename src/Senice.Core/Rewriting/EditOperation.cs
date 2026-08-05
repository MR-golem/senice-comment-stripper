using Senice.Core.Models;

namespace Senice.Core.Rewriting;

public enum EditKind
{
    PreserveNewLines,
    Delete,
}

public readonly record struct EditOperation(TextRange Range, EditKind Kind);
