namespace Delp.Core.Tools.WebDev;

/// <summary>Shared result shape for the CSS/JS/HTML minifiers (all backed by NUglify).</summary>
public sealed record MinifyResult(string? Code, IReadOnlyList<string> Errors, int BeforeBytes, int AfterBytes)
{
    /// <summary>Percentage byte reduction from <see cref="BeforeBytes"/> to <see cref="AfterBytes"/>.</summary>
    public double SavingsPercent =>
        BeforeBytes == 0 || string.IsNullOrEmpty(Code)
            ? 0
            : Math.Round((1 - (double)AfterBytes / BeforeBytes) * 100, 1);
}

/// <summary>Formats NUglify's error list into human-readable one-line messages.</summary>
internal static class NUglifyErrors
{
    public static IReadOnlyList<string> Format(IEnumerable<NUglify.UglifyError>? errors) =>
        (errors ?? Enumerable.Empty<NUglify.UglifyError>())
            .Select(e => $"Line {e.StartLine}, Col {e.StartColumn}: {e.Message}")
            .ToList();
}
