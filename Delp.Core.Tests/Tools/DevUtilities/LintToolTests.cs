using Delp.Core.Tools.DevUtilities;

namespace Delp.Core.Tests.Tools.DevUtilities;

public class LintToolTests
{
    // ---------------------------------------------------------------- C#

    [Fact]
    public void CSharp_SyntaxError_ReportsPosition()
    {
        const string code = "using System;\nvar x = 1\nConsole.WriteLine(x);\n";
        var issues = LintTool.Lint(code, LintLanguage.CSharp);

        var issue = Assert.Single(issues);
        Assert.Equal(LintSeverity.Error, issue.Severity);
        Assert.Equal(2, issue.Line);
        Assert.Equal(10, issue.Col);
    }

    [Fact]
    public void CSharp_UndefinedVariable_ReportsCS0103()
    {
        const string code = "using System;\nConsole.WriteLine(y);\n";
        var issues = LintTool.Lint(code, LintLanguage.CSharp);

        Assert.Contains(issues, i => i.Code == "CS0103" && i.Severity == LintSeverity.Error && i.Line == 2);
    }

    [Fact]
    public void CSharp_CleanTopLevelProgram_HasNoErrors()
    {
        const string code = "using System;\nvar nums = new[] { 1, 2, 3 };\nforeach (var n in nums) Console.WriteLine(n);\n";
        var issues = LintTool.Lint(code, LintLanguage.CSharp);

        Assert.Empty(issues);
    }

    [Fact]
    public void CSharp_SnippetWithoutMain_DoesNotReportMissingEntryPoint()
    {
        const string code = "public class Foo\n{\n    public int Bar() => 1;\n}\n";
        var issues = LintTool.Lint(code, LintLanguage.CSharp);

        Assert.DoesNotContain(issues, i => i.Code == "CS5001");
    }

    // ---------------------------------------------------------------- JavaScript

    [Fact]
    public void JavaScript_SyntaxError_ReportsLine()
    {
        const string code = "function f(a, b {\n  return a + b;\n}\n";
        var issues = LintTool.Lint(code, LintLanguage.JavaScript);

        Assert.NotEmpty(issues);
        Assert.All(issues, i => Assert.Equal(1, i.Line));
        Assert.Contains(issues, i => i.Severity == LintSeverity.Error);
    }

    [Fact]
    public void JavaScript_CleanCode_HasNoErrors()
    {
        const string code = "function add(a, b) {\n  return a + b;\n}\n";
        var issues = LintTool.Lint(code, LintLanguage.JavaScript);

        Assert.Empty(issues);
    }

    /// <summary>NUglify correctly parses common modern (ES6+) syntax — arrow functions,
    /// template literals, array destructuring-free arrow bodies — without false-positiving.
    /// If a future NUglify version regresses here, that's the signal to downgrade affected
    /// diagnostics to Warning rather than Error (per TOOLSPEC's honesty-about-depth note).</summary>
    [Fact]
    public void JavaScript_ModernSyntax_ArrowFunctionsAndTemplateLiterals_HasNoErrors()
    {
        const string code =
            "const add = (a, b) => a + b;\n" +
            "const name = \"world\";\n" +
            "const greeting = `Hello, ${name}!`;\n" +
            "const nums = [1, 2, 3].map(n => n * 2);\n" +
            "console.log(greeting, add(1, 2), nums);\n";
        var issues = LintTool.Lint(code, LintLanguage.JavaScript);

        Assert.Empty(issues);
    }

    // ---------------------------------------------------------------- JSON

    [Fact]
    public void Json_MalformedInput_ReportsSingleError()
    {
        var issues = LintTool.Lint("{\"a\":}", LintLanguage.Json);

        var issue = Assert.Single(issues);
        Assert.Equal(LintSeverity.Error, issue.Severity);
        Assert.Equal(1, issue.Line);
        Assert.Equal(6, issue.Col);
    }

    [Fact]
    public void Json_ValidInput_HasNoIssues()
    {
        Assert.Empty(LintTool.Lint("{\"a\": [1, 2, 3], \"b\": null}", LintLanguage.Json));
    }

    // ---------------------------------------------------------------- Generic: brackets

    [Fact]
    public void Generic_UnclosedBracket_ReportsLineOfFirstImbalance()
    {
        const string code = "line one\nfoo(bar\nline three\n";
        var issues = LintTool.Lint(code, LintLanguage.PlainText);

        Assert.Contains(issues, i => i.Code == "BRACKET" && i.Line == 2 && i.Severity == LintSeverity.Error);
    }

    [Fact]
    public void Generic_ExtraClosingBracket_ReportsAtThatLine()
    {
        const string code = "ok\nfoo)\n";
        var issues = LintTool.Lint(code, LintLanguage.PlainText);

        var issue = Assert.Single(issues.Where(i => i.Code == "BRACKET"));
        Assert.Equal(2, issue.Line);
    }

    [Fact]
    public void Generic_BalancedBrackets_NoBracketIssue()
    {
        var issues = LintTool.Lint("foo(bar[baz]{qux})", LintLanguage.PlainText);
        Assert.DoesNotContain(issues, i => i.Code == "BRACKET");
    }

    /// <summary>Documents a known, deliberate limitation: the generic checks are plain
    /// character scans with no notion of string literals or comments, so a bracket character
    /// that only exists inside a string is still counted (and can misreport imbalance). This
    /// pins the behavior rather than letting it silently change; the UI note calls it out.</summary>
    [Fact]
    public void Generic_UnbalancedBracketInsideStringLiteral_IsStillFlagged_KnownLimitation()
    {
        const string code = "var s = \"unbalanced ( in a string\";\nvar t = 42;\n";
        var issues = LintTool.Lint(code, LintLanguage.PlainText);

        Assert.Contains(issues, i => i.Code == "BRACKET" && i.Line == 1);
    }

    // ---------------------------------------------------------------- Generic: mixed indentation

    [Fact]
    public void Generic_MixedTabsAndSpaces_ReportsWarning()
    {
        var issues = LintTool.Lint("normal\n\t  mixed indent\nnormal2\n", LintLanguage.PlainText);

        var issue = Assert.Single(issues.Where(i => i.Code == "MIXED-INDENT"));
        Assert.Equal(LintSeverity.Warning, issue.Severity);
        Assert.Equal(2, issue.Line);
    }

    // ---------------------------------------------------------------- Generic: trailing whitespace / long lines

    [Fact]
    public void Generic_TrailingWhitespace_AggregatesCount()
    {
        var issues = LintTool.Lint("a  \nb\nc\t\n", LintLanguage.PlainText);

        var issue = Assert.Single(issues.Where(i => i.Code == "TRAILING-WS"));
        Assert.Equal(LintSeverity.Info, issue.Severity);
        Assert.Contains("2", issue.Message);
        Assert.Equal(1, issue.Line);
    }

    [Fact]
    public void Generic_LongLines_AggregatesCount()
    {
        var longLine = new string('x', 130);
        var code = $"short\n{longLine}\n{longLine}\n";
        var issues = LintTool.Lint(code, LintLanguage.PlainText);

        var issue = Assert.Single(issues.Where(i => i.Code == "LONG-LINE"));
        Assert.Contains("2", issue.Message);
        Assert.Equal(2, issue.Line);
    }

    // ---------------------------------------------------------------- Generic: TODO/FIXME

    [Fact]
    public void Generic_TodoAndFixme_ReportedPerLine()
    {
        var issues = LintTool.Lint("a\nTODO fix this\nFIXME also this\n", LintLanguage.PlainText)
            .Where(i => i.Code is "TODO" or "FIXME")
            .ToList();

        Assert.Equal(2, issues.Count);
        Assert.Contains(issues, i => i.Code == "TODO" && i.Line == 2);
        Assert.Contains(issues, i => i.Code == "FIXME" && i.Line == 3);
    }

    // ---------------------------------------------------------------- Python indent heuristic

    [Fact]
    public void Python_InconsistentIndentWidth_ReportsWarning()
    {
        const string code = "def f():\n    x = 1\n    if x:\n      y = 2\n";
        var issues = LintTool.Lint(code, LintLanguage.Python);

        var issue = Assert.Single(issues.Where(i => i.Code == "PY-INDENT"));
        Assert.Equal(LintSeverity.Warning, issue.Severity);
        Assert.Equal(4, issue.Line);
    }

    [Fact]
    public void Python_ConsistentIndentWidth_NoIndentIssue()
    {
        const string code = "def f():\n    x = 1\n    if x:\n        y = 2\n";
        var issues = LintTool.Lint(code, LintLanguage.Python);

        Assert.DoesNotContain(issues, i => i.Code == "PY-INDENT");
    }

    [Fact]
    public void PlainText_DoesNotRunPythonIndentCheck()
    {
        const string code = "def f():\n    x = 1\n    if x:\n      y = 2\n";
        var issues = LintTool.Lint(code, LintLanguage.PlainText);

        Assert.DoesNotContain(issues, i => i.Code == "PY-INDENT");
    }

    // ---------------------------------------------------------------- Empty input / sorting

    [Fact]
    public void EmptyInput_HasNoIssues_ForEveryLanguage()
    {
        foreach (var lang in Enum.GetValues<LintLanguage>())
            Assert.Empty(LintTool.Lint("", lang));

        foreach (var lang in Enum.GetValues<LintLanguage>())
            Assert.Empty(LintTool.Lint("   \n  \n", lang));
    }

    [Fact]
    public void Issues_AreSortedByLine()
    {
        const string code = "TODO one\nTODO two\nTODO zero-before-two-but-after-sort\n";
        var issues = LintTool.Lint(code, LintLanguage.PlainText);

        var lines = issues.Select(i => i.Line).ToList();
        var sorted = lines.OrderBy(l => l).ToList();
        Assert.Equal(sorted, lines);
    }
}
