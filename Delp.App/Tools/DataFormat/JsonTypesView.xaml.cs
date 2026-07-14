using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DataFormat;

namespace Delp.App.Tools.DataFormat;

[Tool("json-types", "JSON to C# / TypeScript Types", ToolCategory.DataFormat,
    "Infer a merged schema from JSON and generate idiomatic C# records or TypeScript interfaces.",
    Keywords = "json,csharp,typescript,types,quicktype,codegen", Order = 110)]
public partial class JsonTypesView : UserControl
{
    private const string SampleJson =
        "{\n  \"id\": 1,\n  \"name\": \"Ada Lovelace\",\n  \"active\": true,\n  \"tags\": [\"math\", \"computing\"]\n}";

    private readonly ICSharpCode.AvalonEdit.TextEditor _inputEditor;
    private readonly ICSharpCode.AvalonEdit.TextEditor _csEditor;
    private readonly ICSharpCode.AvalonEdit.TextEditor _tsEditor;
    private readonly DispatcherTimer _debounce;

    public JsonTypesView()
    {
        InitializeComponent();

        _inputEditor = CodeEditors.Create("Json", wordWrap: true);
        InputHost.Child = _inputEditor;
        _csEditor = CodeEditors.Create(null, readOnly: true, wordWrap: true);
        CsHost.Child = _csEditor;
        _tsEditor = CodeEditors.Create(null, readOnly: true, wordWrap: true);
        TsHost.Child = _tsEditor;

        _inputEditor.TextChanged += (_, _) => ScheduleRun();

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Run();
        };

        Loaded += (_, _) =>
        {
            _inputEditor.Text = SampleJson;
            Run();
        };
    }

    private void ScheduleRun()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            ScheduleRun();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            Run();
    }

    private void Run()
    {
        try
        {
            var rootName = string.IsNullOrWhiteSpace(RootNameBox.Text) ? "Root" : RootNameBox.Text.Trim();
            var schema = JsonTypesTool.Infer(_inputEditor.Text, rootName);

            var csOptions = new CSharpOptions(RecordsBox.IsChecked == true, JsonPropertyNamesBox.IsChecked == true);
            var tsOptions = new TsOptions(InterfacesBox.IsChecked == true);

            _csEditor.Text = JsonTypesTool.ToCSharp(schema, csOptions);
            _tsEditor.Text = JsonTypesTool.ToTypeScript(schema, tsOptions);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CopyCs_Click(object sender, RoutedEventArgs e) => Ui.Copy(_csEditor.Text, CopyCsBtn);
    private void CopyTs_Click(object sender, RoutedEventArgs e) => Ui.Copy(_tsEditor.Text, CopyTsBtn);
}
