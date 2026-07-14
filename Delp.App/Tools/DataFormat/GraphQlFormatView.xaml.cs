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

    public GraphQlFormatView()
    {
        InitializeComponent();

        _input = CodeEditors.Create();
        _output = CodeEditors.Create(readOnly: true);
        InputHost.Child = _input;
        OutputHost.Child = _output;

        _validateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _validateTimer.Tick += (_, _) => { _validateTimer.Stop(); Validate(); };

        _input.TextChanged += (_, _) =>
        {
            _validateTimer.Stop();
            _validateTimer.Start();
        };
    }

    private void Validate()
    {
        if (_input.Text.Trim().Length == 0)
        {
            ValidityText.Text = "";
            return;
        }

        var error = GraphQlTool.Validate(_input.Text);
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

    private void Format_Click(object sender, RoutedEventArgs e) =>
        Run(() => _output.Text = GraphQlTool.Format(_input.Text));

    private void Minify_Click(object sender, RoutedEventArgs e) =>
        Run(() => _output.Text = GraphQlTool.Minify(_input.Text));

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyBtn);

    /// <summary>Runs a conversion with inline error reporting.</summary>
    private void Run(Action convert)
    {
        try
        {
            convert();
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
