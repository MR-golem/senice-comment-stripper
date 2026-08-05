namespace Senice.Core.Models;

public readonly record struct CommentSpan(TextRange Range, CommentKind Kind);
