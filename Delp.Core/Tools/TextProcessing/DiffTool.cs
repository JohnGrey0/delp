using DiffPlex.DiffBuilder;

namespace Delp.Core.Tools.TextProcessing;

/// <summary>Mirrors DiffPlex's <c>ChangeType</c> so the App layer never references DiffPlex directly.</summary>
public enum DiffKind
{
    Unchanged,
    Inserted,
    Deleted,
    Modified,
    Imaginary,
}

/// <summary>A sub-span of a line, used to highlight the word(s) that actually changed.</summary>
public sealed record DiffPiece(string Text, bool IsHighlighted);

/// <summary>One rendered line of a diff pane.</summary>
public sealed record DiffLine(int? Number, string Text, DiffKind Kind, IReadOnlyList<DiffPiece> SubPieces);

/// <summary>One side (old or new) of a side-by-side diff.</summary>
public sealed record DiffPane(IReadOnlyList<DiffLine> Lines);

/// <summary>Options for <see cref="DiffTool.Compute"/>.</summary>
public sealed record DiffToolOptions(bool IgnoreCase = false, bool IgnoreWhitespace = false);

/// <summary>Side-by-side diff of two texts, plus a one-line change summary.</summary>
public sealed record DiffResult(DiffPane Old, DiffPane New, int Insertions, int Deletions);

/// <summary>
/// Line-level, word-highlighted diff built on DiffPlex, mapped into Delp's own
/// serializable records so the App layer never needs a DiffPlex reference.
/// </summary>
public static class DiffTool
{
    public static DiffResult Compute(string? oldText, string? newText, DiffToolOptions options)
    {
        var model = SideBySideDiffBuilder.Diff(
            oldText ?? "",
            newText ?? "",
            ignoreWhiteSpace: options.IgnoreWhitespace,
            ignoreCase: options.IgnoreCase);

        var oldPane = MapPane(model.OldText);
        var newPane = MapPane(model.NewText);

        var insertions = newPane.Lines.Count(l => l.Kind is DiffKind.Inserted or DiffKind.Modified);
        var deletions = oldPane.Lines.Count(l => l.Kind is DiffKind.Deleted or DiffKind.Modified);

        return new DiffResult(oldPane, newPane, insertions, deletions);
    }

    private static DiffPane MapPane(DiffPlex.DiffBuilder.Model.DiffPaneModel pane)
    {
        var lines = pane.Lines.Select(line =>
        {
            var subPieces = line.SubPieces is { Count: > 0 }
                ? line.SubPieces
                    .Select(sp => new DiffPiece(sp.Text ?? "", sp.Type != DiffPlex.DiffBuilder.Model.ChangeType.Unchanged))
                    .ToList()
                : new List<DiffPiece>();

            return new DiffLine(line.Position, line.Text ?? "", MapKind(line.Type), subPieces);
        }).ToList();

        return new DiffPane(lines);
    }

    private static DiffKind MapKind(DiffPlex.DiffBuilder.Model.ChangeType type) => type switch
    {
        DiffPlex.DiffBuilder.Model.ChangeType.Unchanged => DiffKind.Unchanged,
        DiffPlex.DiffBuilder.Model.ChangeType.Inserted => DiffKind.Inserted,
        DiffPlex.DiffBuilder.Model.ChangeType.Deleted => DiffKind.Deleted,
        DiffPlex.DiffBuilder.Model.ChangeType.Modified => DiffKind.Modified,
        DiffPlex.DiffBuilder.Model.ChangeType.Imaginary => DiffKind.Imaginary,
        _ => DiffKind.Unchanged,
    };
}
