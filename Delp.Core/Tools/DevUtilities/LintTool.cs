using System.Globalization;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUglify;

namespace Delp.Core.Tools.DevUtilities;

/// <summary>Languages <see cref="LintTool"/> can check, in increasing order of "basic-ness".</summary>
public enum LintLanguage
{
    CSharp,
    JavaScript,
    Json,
    Python,
    PlainText,
}

public enum LintSeverity
{
    Error,
    Warning,
    Info,
}

public sealed record LintIssue(int Line, int Col, LintSeverity Severity, string Code, string Message);

/// <summary>
/// Lints a code snippet. Depth varies by language: C# gets full Roslyn syntax + semantic
/// diagnostics, JavaScript gets NUglify's parser diagnostics, JSON gets a single parse error
/// (if any), and Python/Plain text get hand-rolled structural checks only.
/// </summary>
public static class LintTool
{
    private const int MaxLineLength = 120;

    public static IReadOnlyList<LintIssue> Lint(string code, LintLanguage lang)
    {
        ArgumentNullException.ThrowIfNull(code);

        // An empty (or whitespace-only) snippet never has issues, regardless of language —
        // this also sidesteps e.g. JSON's "no tokens" parse error on a blank editor.
        if (string.IsNullOrWhiteSpace(code))
            return [];

        List<LintIssue> issues = lang switch
        {
            LintLanguage.CSharp => LintCSharp(code),
            LintLanguage.JavaScript => LintJavaScript(code),
            LintLanguage.Json => LintJson(code),
            LintLanguage.Python => LintGeneric(code, isPython: true),
            LintLanguage.PlainText => LintGeneric(code, isPython: false),
            _ => throw new ArgumentOutOfRangeException(nameof(lang)),
        };

        issues.Sort((a, b) => a.Line != b.Line ? a.Line.CompareTo(b.Line) : a.Col.CompareTo(b.Col));
        return issues;
    }

    // ============================================================== C# (Roslyn)

    // Building the metadata reference set means reading + decoding assembly metadata, which is
    // by far the most expensive part of standing up a Compilation — cache it once, for the life
    // of the process, instead of paying that cost on every keystroke.
    private static readonly Lazy<IReadOnlyList<MetadataReference>> CSharpReferences =
        new(BuildCSharpReferences);

    // Deliberately small: just enough for object, common console/LINQ/collection snippets to
    // bind so undefined names produce real CS0103-style diagnostics instead of drowning in
    // "type not found" noise from a missing reference.
    private static readonly string[] WantedAssemblyNames =
    [
        "System.Private.CoreLib",
        "System.Runtime",
        "System.Console",
        "System.Linq",
        "System.Collections",
    ];

    private static IReadOnlyList<MetadataReference> BuildCSharpReferences()
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrEmpty(trustedAssemblies))
            return [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];

        var refs = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!seen.Add(name))
                continue;
            if (Array.Exists(WantedAssemblyNames, w => w.Equals(name, StringComparison.OrdinalIgnoreCase)))
                refs.Add(MetadataReference.CreateFromFile(path));
        }

        // Should never happen on a normal .NET install, but never leave the compiler with zero
        // references (every diagnostic would otherwise be swamped by "object not found").
        if (refs.Count == 0)
            refs.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        return refs;
    }

    private static List<LintIssue> LintCSharp(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest));

        // Top-level statements only compile under an "executable" output kind (else: CS8805).
        // A snippet with no top-level statements (e.g. just a class) has no entry point, which
        // would spuriously fail under an executable kind (CS5001) — so pick the kind per-snippet.
        var hasTopLevelStatements = tree.GetCompilationUnitRoot().Members.OfType<GlobalStatementSyntax>().Any();
        var outputKind = hasTopLevelStatements ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;

        var compilation = CSharpCompilation.Create(
            "DelpLintSnippet",
            [tree],
            CSharpReferences.Value,
            new CSharpCompilationOptions(outputKind));

        var issues = new List<LintIssue>();
        foreach (var diagnostic in compilation.GetDiagnostics())
        {
            if (diagnostic.Severity == DiagnosticSeverity.Hidden)
                continue;

            var line = 1;
            var col = 1;
            if (diagnostic.Location.IsInSource)
            {
                var span = diagnostic.Location.GetLineSpan();
                line = span.StartLinePosition.Line + 1;
                col = span.StartLinePosition.Character + 1;
            }

            var severity = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => LintSeverity.Error,
                DiagnosticSeverity.Warning => LintSeverity.Warning,
                _ => LintSeverity.Info,
            };

            issues.Add(new LintIssue(line, col, severity, diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture)));
        }

        return issues;
    }

    // ============================================================== JavaScript (NUglify)

    private static List<LintIssue> LintJavaScript(string code)
    {
        // Minified output is irrelevant here — only the diagnostics matter.
        var result = Uglify.Js(code);
        var issues = new List<LintIssue>();
        foreach (var error in result.Errors)
        {
            issues.Add(new LintIssue(
                Math.Max(error.StartLine, 1),
                Math.Max(error.StartColumn, 1),
                error.IsError ? LintSeverity.Error : LintSeverity.Warning,
                string.IsNullOrEmpty(error.ErrorCode) ? "JS" : error.ErrorCode,
                error.Message));
        }
        return issues;
    }

    // ============================================================== JSON

    private static List<LintIssue> LintJson(string code)
    {
        try
        {
            using var _ = JsonDocument.Parse(code);
            return [];
        }
        catch (JsonException ex)
        {
            var line = (int)(ex.LineNumber ?? 0) + 1;
            var col = (int)(ex.BytePositionInLine ?? 0) + 1;
            return [new LintIssue(line, col, LintSeverity.Error, "JSON", ex.Message)];
        }
    }

    // ============================================================== Generic (Python / Plain text)

    private static List<LintIssue> LintGeneric(string code, bool isPython)
    {
        var issues = new List<LintIssue>();
        var lines = code.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var normalized = string.Join('\n', lines);

        CheckBrackets(normalized, issues);
        CheckMixedIndentation(lines, issues);
        CheckTrailingWhitespace(lines, issues);
        CheckLongLines(lines, issues);
        CheckTodoFixme(lines, issues);

        if (isPython)
            CheckPythonIndentConsistency(lines, issues);

        return issues;
    }

    /// <summary>Reports a single Error at the first point () [] {} become unbalanced.</summary>
    private static void CheckBrackets(string code, List<LintIssue> issues)
    {
        const string opens = "([{";
        const string closes = ")]}";
        var stack = new List<(char Ch, int Line, int Col)>();
        var line = 1;
        var col = 1;

        foreach (var ch in code)
        {
            if (ch == '\n')
            {
                line++;
                col = 1;
                continue;
            }

            var openIdx = opens.IndexOf(ch);
            if (openIdx >= 0)
            {
                stack.Add((ch, line, col));
            }
            else
            {
                var closeIdx = closes.IndexOf(ch);
                if (closeIdx >= 0)
                {
                    if (stack.Count == 0)
                    {
                        issues.Add(new LintIssue(line, col, LintSeverity.Error, "BRACKET",
                            $"Unmatched '{ch}' has no matching opening bracket."));
                        return;
                    }

                    var top = stack[^1];
                    stack.RemoveAt(stack.Count - 1);
                    var expectedClose = closes[opens.IndexOf(top.Ch)];
                    if (expectedClose != ch)
                    {
                        issues.Add(new LintIssue(top.Line, top.Col, LintSeverity.Error, "BRACKET",
                            $"'{top.Ch}' opened here is closed with mismatched '{ch}'."));
                        return;
                    }
                }
            }

            col++;
        }

        if (stack.Count > 0)
        {
            var first = stack[0];
            issues.Add(new LintIssue(first.Line, first.Col, LintSeverity.Error, "BRACKET",
                $"'{first.Ch}' is never closed."));
        }
    }

    private static void CheckMixedIndentation(string[] lines, List<LintIssue> issues)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var end = 0;
            while (end < line.Length && (line[end] == ' ' || line[end] == '\t'))
                end++;
            var leading = line[..end];
            if (leading.Contains(' ') && leading.Contains('\t'))
                issues.Add(new LintIssue(i + 1, 1, LintSeverity.Warning, "MIXED-INDENT",
                    "Line mixes tabs and spaces in its indentation."));
        }
    }

    private static void CheckTrailingWhitespace(string[] lines, List<LintIssue> issues)
    {
        var offenders = 0;
        var firstLine = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length == 0 || (line[^1] != ' ' && line[^1] != '\t'))
                continue;
            if (offenders == 0)
                firstLine = i + 1;
            offenders++;
        }

        if (offenders > 0)
            issues.Add(new LintIssue(firstLine, 1, LintSeverity.Info, "TRAILING-WS",
                $"Trailing whitespace on {offenders} line{(offenders == 1 ? "" : "s")}."));
    }

    private static void CheckLongLines(string[] lines, List<LintIssue> issues)
    {
        var offenders = 0;
        var firstLine = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length <= MaxLineLength)
                continue;
            if (offenders == 0)
                firstLine = i + 1;
            offenders++;
        }

        if (offenders > 0)
            issues.Add(new LintIssue(firstLine, MaxLineLength + 1, LintSeverity.Info, "LONG-LINE",
                $"{offenders} line{(offenders == 1 ? "" : "s")} exceed{(offenders == 1 ? "s" : "")} {MaxLineLength} characters."));
    }

    private static void CheckTodoFixme(string[] lines, List<LintIssue> issues)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var (tag, idx) = FindTodoOrFixme(line);
            if (idx >= 0)
                issues.Add(new LintIssue(i + 1, idx + 1, LintSeverity.Info, tag, $"{tag} comment found."));
        }
    }

    private static (string Tag, int Index) FindTodoOrFixme(string line)
    {
        var todoIdx = line.IndexOf("TODO", StringComparison.Ordinal);
        var fixmeIdx = line.IndexOf("FIXME", StringComparison.Ordinal);
        if (todoIdx < 0)
            return ("FIXME", fixmeIdx);
        if (fixmeIdx < 0)
            return ("TODO", todoIdx);
        return todoIdx <= fixmeIdx ? ("TODO", todoIdx) : ("FIXME", fixmeIdx);
    }

    /// <summary>
    /// Heuristic: the width of the first indent increase in the file becomes the baseline; any
    /// later indent increase by a different width is flagged. Lines that lead with a tab are
    /// skipped here (mixed-indentation is already reported by <see cref="CheckMixedIndentation"/>).
    /// </summary>
    private static void CheckPythonIndentConsistency(string[] lines, List<LintIssue> issues)
    {
        int? baseline = null;
        var prevIndent = 0;

        foreach (var (line, i) in lines.Select((l, i) => (l, i)))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var indent = 0;
            while (indent < line.Length && line[indent] == ' ')
                indent++;
            if (indent < line.Length && line[indent] == '\t')
            {
                prevIndent = indent;
                continue;
            }

            if (indent > prevIndent)
            {
                var delta = indent - prevIndent;
                if (baseline is null)
                    baseline = delta;
                else if (delta != baseline)
                    issues.Add(new LintIssue(i + 1, indent + 1, LintSeverity.Warning, "PY-INDENT",
                        $"Indent width {delta} differs from the file's established width of {baseline}."));
            }

            prevIndent = indent;
        }
    }
}
