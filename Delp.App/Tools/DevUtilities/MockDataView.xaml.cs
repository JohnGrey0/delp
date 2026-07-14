using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("mock-data", "Mock Data Generator", ToolCategory.DevUtilities,
    "Generate seeded, correlated fake data rows and export them as JSON, CSV, or SQL inserts.",
    Keywords = "mock,fake,faker,test data,seed,json", Order = 110)]
public partial class MockDataView : UserControl
{
    private readonly List<(TextBox NameBox, ComboBox KindBox, TextBox OptionsBox, Grid Row)> _rows = new();
    private readonly ICSharpCode.AvalonEdit.TextEditor _outputEditor;
    private readonly IHighlightingDefinition? _jsonHighlighting;

    public MockDataView()
    {
        InitializeComponent();

        _outputEditor = CodeEditors.Create("Json", readOnly: true, wordWrap: true);
        _jsonHighlighting = _outputEditor.SyntaxHighlighting;
        OutputHost.Child = _outputEditor;

        AddFieldRow("id", FieldKind.Uuid, "");
        AddFieldRow("firstName", FieldKind.FirstName, "");
        AddFieldRow("lastName", FieldKind.LastName, "");
        AddFieldRow("email", FieldKind.Email, "");
        AddFieldRow("city", FieldKind.City, "");
        AddFieldRow("createdAt", FieldKind.IsoDateTime, "");
    }

    private string SelectedFormat =>
        SqlFormatRadio.IsChecked == true ? "SQL" : CsvFormatRadio.IsChecked == true ? "CSV" : "JSON";

    private void AddFieldRow(string name, FieldKind kind, string options)
    {
        var nameBox = new TextBox { Text = name, MinWidth = 100, Margin = new Thickness(0, 0, 8, 0) };
        var kindBox = new ComboBox
        {
            ItemsSource = Enum.GetValues<FieldKind>(),
            SelectedItem = kind,
            Width = 130,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var optionsBox = new TextBox
        {
            Text = options,
            Width = 110,
            Margin = new Thickness(0, 0, 8, 0),
            Tag = "range e.g. 1..100",
        };
        var deleteBtn = new Button { Content = "✕", Style = (Style)FindResource("Button.Icon") };

        var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(nameBox, 0);
        Grid.SetColumn(kindBox, 1);
        Grid.SetColumn(optionsBox, 2);
        Grid.SetColumn(deleteBtn, 3);
        row.Children.Add(nameBox);
        row.Children.Add(kindBox);
        row.Children.Add(optionsBox);
        row.Children.Add(deleteBtn);

        deleteBtn.Click += (_, _) =>
        {
            FieldsPanel.Children.Remove(row);
            _rows.RemoveAll(r => r.Row == row);
        };

        _rows.Add((nameBox, kindBox, optionsBox, row));
        FieldsPanel.Children.Add(row);
    }

    private void AddFieldBtn_Click(object sender, RoutedEventArgs e) =>
        AddFieldRow($"field{_rows.Count + 1}", FieldKind.FirstName, "");

    private void Format_Changed(object sender, RoutedEventArgs e)
    {
        if (TableNameRow is not null)
            TableNameRow.Visibility = SelectedFormat == "SQL" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var fields = ReadFields();
            var rows = ParseInt(RowCountBox.Text, "Row count");
            int? seed = string.IsNullOrWhiteSpace(SeedBox.Text) ? null : ParseInt(SeedBox.Text, "Seed");

            var sw = Stopwatch.StartNew();
            var data = MockDataTool.Generate(fields, rows, seed);
            var format = SelectedFormat;
            var output = format switch
            {
                "CSV" => MockDataTool.ToCsv(data),
                "SQL" => MockDataTool.ToSqlInserts(data, string.IsNullOrWhiteSpace(TableNameBox.Text) ? "mock_data" : TableNameBox.Text.Trim()),
                _ => MockDataTool.ToJson(data),
            };
            sw.Stop();

            _outputEditor.SyntaxHighlighting = format == "JSON" ? _jsonHighlighting : null;
            _outputEditor.Text = output;
            StatusText.Text = $"{rows} rows in {sw.ElapsedMilliseconds} ms";
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private List<FieldSpec> ReadFields()
    {
        var specs = new List<FieldSpec>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in _rows)
        {
            var name = row.NameBox.Text.Trim();
            if (name.Length == 0)
                throw new FormatException("Every field needs a name.");
            if (!names.Add(name))
                throw new FormatException($"Duplicate field name '{name}'.");

            var kind = row.KindBox.SelectedItem is FieldKind k ? k : FieldKind.FirstName;
            var options = string.IsNullOrWhiteSpace(row.OptionsBox.Text) ? null : row.OptionsBox.Text.Trim();
            specs.Add(new FieldSpec(name, kind, options));
        }

        if (specs.Count == 0)
            throw new FormatException("Add at least one field.");

        return specs;
    }

    private static int ParseInt(string text, string field)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 0)
            throw new FormatException($"{field} must be a non-negative whole number.");
        return value;
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(_outputEditor.Text, CopyBtn);
}
