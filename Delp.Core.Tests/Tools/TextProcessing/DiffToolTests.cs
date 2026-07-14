using Delp.Core.Tools.TextProcessing;

namespace Delp.Core.Tests.Tools.TextProcessing;

public class DiffToolTests
{
    private static readonly DiffToolOptions Default = new();

    [Fact]
    public void Compute_IdenticalInputs_AllLinesUnchanged()
    {
        const string text = "line one\nline two\nline three";

        var result = DiffTool.Compute(text, text, Default);

        Assert.All(result.Old.Lines, l => Assert.Equal(DiffKind.Unchanged, l.Kind));
        Assert.All(result.New.Lines, l => Assert.Equal(DiffKind.Unchanged, l.Kind));
        Assert.Equal(0, result.Insertions);
        Assert.Equal(0, result.Deletions);
    }

    [Fact]
    public void Compute_DetectsInsertedLine()
    {
        var result = DiffTool.Compute("a\nb", "a\nb\nc", Default);

        Assert.Contains(result.New.Lines, l => l.Kind == DiffKind.Inserted && l.Text == "c");
        Assert.True(result.Insertions >= 1);
    }

    [Fact]
    public void Compute_DetectsDeletedLine()
    {
        var result = DiffTool.Compute("a\nb\nc", "a\nc", Default);

        Assert.Contains(result.Old.Lines, l => l.Kind == DiffKind.Deleted && l.Text == "b");
        Assert.True(result.Deletions >= 1);
    }

    [Fact]
    public void Compute_DetectsModifiedLine()
    {
        var result = DiffTool.Compute("hello world", "hello there", Default);

        Assert.Contains(result.Old.Lines, l => l.Kind == DiffKind.Modified);
        Assert.Contains(result.New.Lines, l => l.Kind == DiffKind.Modified);
    }

    [Fact]
    public void Compute_IgnoreCase_TreatsDifferentCaseAsUnchanged()
    {
        var caseSensitive = DiffTool.Compute("Hello", "hello", Default);
        Assert.Contains(caseSensitive.Old.Lines, l => l.Kind != DiffKind.Unchanged);

        var ignoreCase = DiffTool.Compute("Hello", "hello", Default with { IgnoreCase = true });
        Assert.All(ignoreCase.Old.Lines, l => Assert.Equal(DiffKind.Unchanged, l.Kind));
        Assert.All(ignoreCase.New.Lines, l => Assert.Equal(DiffKind.Unchanged, l.Kind));
    }

    [Fact]
    public void Compute_IgnoreWhitespace_TreatsExtraSpacesAsUnchanged()
    {
        var strict = DiffTool.Compute("a  b", "a b", Default);
        Assert.Contains(strict.Old.Lines, l => l.Kind != DiffKind.Unchanged);

        var ignoreWhitespace = DiffTool.Compute("a  b", "a b", Default with { IgnoreWhitespace = true });
        Assert.All(ignoreWhitespace.Old.Lines, l => Assert.Equal(DiffKind.Unchanged, l.Kind));
    }

    [Fact]
    public void Compute_EmptySides_ProducesNoError()
    {
        var bothEmpty = DiffTool.Compute("", "", Default);
        Assert.Equal(0, bothEmpty.Insertions);
        Assert.Equal(0, bothEmpty.Deletions);

        var oldEmpty = DiffTool.Compute("", "new line", Default);
        Assert.True(oldEmpty.Insertions >= 1);

        var newEmpty = DiffTool.Compute("old line", "", Default);
        Assert.True(newEmpty.Deletions >= 1);
    }

    [Fact]
    public void Compute_NullInputs_TreatedAsEmpty()
    {
        var result = DiffTool.Compute(null, null, Default);
        Assert.NotNull(result);
    }
}
