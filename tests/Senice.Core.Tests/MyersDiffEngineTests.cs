using Senice.Core.Abstractions;
using Senice.Core.Diffing;

namespace Senice.Core.Tests;

public class MyersDiffEngineTests
{
    private readonly MyersDiffEngine _engine = new();

    [Fact]
    public void IdenticalLines_NoEdits()
    {
        var edits = _engine.Compute(["a", "b"], ["a", "b"]);

        Assert.Empty(edits);
    }

    [Fact]
    public void SingleDeletion_IsDetected()
    {
        var edits = _engine.Compute(["a", "b", "c"], ["a", "c"]);

        Assert.Single(edits);
        Assert.Equal(DiffOperation.Delete, edits[0].Operation);
        Assert.Equal("b", edits[0].Text);
    }

    [Fact]
    public void SingleInsertion_IsDetected()
    {
        var edits = _engine.Compute(["a", "c"], ["a", "b", "c"]);

        Assert.Single(edits);
        Assert.Equal(DiffOperation.Insert, edits[0].Operation);
        Assert.Equal("b", edits[0].Text);
    }

    [Fact]
    public void EmptyOriginal_AllInsertions()
    {
        var edits = _engine.Compute([], ["a", "b"]);

        Assert.Equal(2, edits.Count);
        Assert.All(edits, e => Assert.Equal(DiffOperation.Insert, e.Operation));
    }

    [Fact]
    public void EmptyModified_AllDeletions()
    {
        var edits = _engine.Compute(["a", "b"], []);

        Assert.Equal(2, edits.Count);
        Assert.All(edits, e => Assert.Equal(DiffOperation.Delete, e.Operation));
    }

    [Fact]
    public void MixedDiff_CombinesEdits()
    {
        var original = new[] { "keep", "old1", "old2", "keep2" };
        var modified = new[] { "keep", "new1", "keep2", "new2" };

        var edits = _engine.Compute(original, modified);

        var operations = edits.Select(e => e.Operation).ToList();
        Assert.Contains(DiffOperation.Delete, operations);
        Assert.Contains(DiffOperation.Insert, operations);
    }
}
