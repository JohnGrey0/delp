using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DataFormat;

[Tool("graphql-format", "GraphQL Formatter", ToolCategory.DataFormat,
    "Format, minify and validate GraphQL queries, fragments and SDL schemas.",
    Keywords = "graphql,format,query,schema,gql", Order = 100)]
public partial class GraphQlFormatView : UserControl
{
    private readonly TextEditor _input;
    private readonly TextEditor _output;
    private readonly DispatcherTimer _validateTimer;
    private int _validateToken;
    private int _formatToken;

    public GraphQlFormatView()
    {
        InitializeComponent();

        _input = CodeEditors.Create();
        _output = CodeEditors.Create(readOnly: true);
        InputHost.Child = _input;
        OutputHost.Child = _output;

        _validateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _validateTimer.Tick += async (_, _) => { _validateTimer.Stop(); await ValidateAsync(); };

        _input.TextChanged += (_, _) =>
        {
            _validateTimer.Stop();
            _validateTimer.Start();
        };

        // Stop pending work when navigated away so a cached-but-hidden view doesn't keep
        // computing/writing to itself; also invalidate any validation/format already in flight.
        Unloaded += (_, _) =>
        {
            _validateTimer.Stop();
            _validateToken++;
            _formatToken++;
        };
    }

    /// <summary>GraphQL-Parser's parse+visit cost is the dominant cost here (measured 100ms+/MB), so this runs off the UI thread.</summary>
    private async Task ValidateAsync()
    {
        var text = _input.Text;
        if (text.Trim().Length == 0)
        {
            ValidityText.Text = "";
            return;
        }

        var token = ++_validateToken;
        var error = await Task.Run(() => GraphQlTool.Validate(text));
        if (token != _validateToken)
            return; // superseded by a newer edit while this validation was running

        if (error is null)
        {
            ValidityText.Text = "Valid GraphQL";
            ValidityText.Foreground = (Brush)FindResource("Brush.Success");
        }
        else
        {
            ValidityText.Text = $"Line {error.Line}, Col {error.Column}: {error.Message}";
            ValidityText.Foreground = (Brush)FindResource("Brush.Danger");
        }
    }

    private void Format_Click(object sender, RoutedEventArgs e) => _ = RunAsync(GraphQlTool.Format);

    private void Minify_Click(object sender, RoutedEventArgs e) => _ = RunAsync(GraphQlTool.Minify);

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyBtn);

    /// <summary>Runs a conversion on the thread pool (safe for 1MB+ input) with inline error reporting.</summary>
    private async Task RunAsync(Func<string, string> convert)
    {
        var text = _input.Text;
        var token = ++_formatToken;
        try
        {
            var result = await Task.Run(() => convert(text));
            if (token != _formatToken) return;
            _output.Text = result;
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (token != _formatToken) return;
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
