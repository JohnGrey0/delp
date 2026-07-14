using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DevUtilities;

[Tool("code-linter", "Code Linter", ToolCategory.DevUtilities,
    "Check C#, JavaScript, JSON, Python, or plain text for syntax problems and common issues.",
    Keywords = "lint,linter,syntax,check,errors,csharp,javascript", Order = 150)]
public partial class CodeLinterView : UserControl
{
    private static readonly (LintLanguage Value, string Label)[] Languages =
    [
        (LintLanguage.CSharp, "C#"),
        (LintLanguage.JavaScript, "JavaScript"),
        (LintLanguage.Json, "JSON"),
        (LintLanguage.Python, "Python (basic checks)"),
        (LintLanguage.PlainText, "Plain text"),
    ];

    private const string SampleCode =
        "using System;\n\nvar items = new[] { 1, 2, 3 };\nforeach (var item in items)\n{\n    Console.WriteLine(item);\n}\n";

    private readonly TextEditor _editor;
    private readonly DispatcherTimer _debounce;
    private readonly Brush _dangerBrush;
    private readonly Brush _warningBrush;
    private readonly Brush _infoBrush;

    private int _lintToken;

    public CodeLinterView()
    {
        InitializeComponent();

        _dangerBrush = (Brush)FindResource("Brush.Danger");
        _warningBrush = (Brush)FindResource("Brush.Warning");
        _infoBrush = (Brush)FindResource("Brush.Fg2");

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _ = RunLintAsync();
        };

        _editor = CodeEditors.Create();
        EditorHost.Child = _editor;
        // Subscribe before seeding text so the sample below triggers the first (debounced) lint,
        // same trick used by the other editor-hosting tools (e.g. SvgPathView's sample path).
        _editor.TextChanged += (_, _) => Debounce();
        _editor.Text = SampleCode;

        LanguageBox.ItemsSource = Languages.Select(l => l.Label).ToList();
        LanguageBox.SelectedIndex = 0;

        Unloaded += (_, _) => _debounce.Stop();
    }

    private LintLanguage SelectedLanguage => Languages[Math.Max(LanguageBox.SelectedIndex, 0)].Value;

    // Roslyn compilation (for C#) is expensive, so re-linting on every keystroke is debounced,
    // and the actual Lint() call runs off the UI thread (see RunLintAsync).
    private void Debounce()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Language_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            Debounce();
    }

    private async Task RunLintAsync()
    {
        var token = ++_lintToken;
        var code = _editor.Text;
        var language = SelectedLanguage;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var issues = await Task.Run(() => LintTool.Lint(code, language));
            stopwatch.Stop();

            // A newer lint request may have started (and even finished) while this one was
            // running in the background — never let a stale result clobber a fresher one.
            if (token != _lintToken)
                return;

            IssuesList.ItemsSource = issues.Select(ToRow).ToList();
            StatusText.Text = Summarize(issues, stopwatch.ElapsedMilliseconds);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (token != _lintToken)
                return;
            IssuesList.ItemsSource = null;
            StatusText.Text = "";
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private static string Summarize(IReadOnlyList<LintIssue> issues, long elapsedMs)
    {
        var errors = issues.Count(i => i.Severity == LintSeverity.Error);
        var warnings = issues.Count(i => i.Severity == LintSeverity.Warning);
        var infos = issues.Count(i => i.Severity == LintSeverity.Info);
        return $"{errors} error{(errors == 1 ? "" : "s")} · {warnings} warning{(warnings == 1 ? "" : "s")} · " +
               $"{infos} info in {elapsedMs} ms";
    }

    private IssueRow ToRow(LintIssue issue) => new(
        $"{issue.Line}:{issue.Col}",
        issue.Message,
        issue.Severity switch
        {
            LintSeverity.Error => _dangerBrush,
            LintSeverity.Warning => _warningBrush,
            _ => _infoBrush,
        },
        issue.Line);

    private void IssuesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IssuesList.SelectedItem is not IssueRow row)
            return;

        var line = Math.Clamp(row.Line, 1, Math.Max(_editor.Document.LineCount, 1));
        _editor.ScrollToLine(line);
        _editor.TextArea.Caret.Line = line;
        _editor.TextArea.Caret.Column = 1;
        _editor.Focus();
    }

    private sealed record IssueRow(string Position, string Message, Brush SeverityBrush, int Line);
}
