using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.WebDev;

namespace Delp.App.Tools.WebDev;

public sealed record PartRow(string Label, string Value);

[Tool("url-parser", "URL Parser & Builder", ToolCategory.WebDev,
    "Break a URL into its parts, edit the query string, and rebuild it live.",
    Keywords = "url,parse,query,params,builder,punycode", Order = 100)]
public partial class UrlParserView : UserControl
{
    private bool _updating;
    private UrlParts? _parts;

    public UrlParserView()
    {
        InitializeComponent();
        UrlBox.Text = "https://user@münchen.example.com:8443/söme/path?q=hello+world&tag=a&tag=b&flag#section";
        ParseUrl();
    }

    private void UrlBox_TextChanged(object sender, TextChangedEventArgs e) => ParseUrl();

    private void ParseUrl()
    {
        if (_updating)
            return;
        _updating = true;
        try
        {
            var text = UrlBox.Text ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                _parts = null;
                PartsList.ItemsSource = null;
                QueryRowsPanel.Children.Clear();
                RebuiltBox.Text = "";
                AssumedSchemeNote.Visibility = Visibility.Collapsed;
                ErrorText.Visibility = Visibility.Collapsed;
                return;
            }

            _parts = UrlTool.Parse(text);
            AssumedSchemeNote.Visibility = text.Contains("://") ? Visibility.Collapsed : Visibility.Visible;

            PartsList.ItemsSource = new[]
            {
                new PartRow("SCHEME", _parts.Scheme),
                new PartRow("HOST", _parts.Host),
                new PartRow("HOST (UNICODE)", _parts.HostUnicode),
                new PartRow("PORT", _parts.Port?.ToString() ?? "(default)"),
                new PartRow("PATH", _parts.Path),
                new PartRow("FRAGMENT", _parts.Fragment),
                new PartRow("USERINFO", _parts.UserInfo ?? ""),
            };

            QueryRowsPanel.Children.Clear();
            foreach (var q in _parts.Query)
                AddQueryRow(q.Key, q.Value);

            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _parts = null;
            PartsList.ItemsSource = null;
            QueryRowsPanel.Children.Clear();
            RebuiltBox.Text = "";
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        finally
        {
            _updating = false;
        }

        RebuildOutput();
    }

    private void AddQueryRow(string key, string value)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var keyBox = new TextBox { Text = key };
        var valueBox = new TextBox { Text = value };
        keyBox.TextChanged += (_, _) => RebuildOutput();
        valueBox.TextChanged += (_, _) => RebuildOutput();
        Grid.SetColumn(keyBox, 0);
        Grid.SetColumn(valueBox, 2);

        var deleteBtn = new Button { Content = "✕", ToolTip = "Remove parameter" };
        if (Application.Current.TryFindResource("Button.Icon") is Style iconStyle)
            deleteBtn.Style = iconStyle;
        deleteBtn.Click += (_, _) =>
        {
            QueryRowsPanel.Children.Remove(row);
            RebuildOutput();
        };
        Grid.SetColumn(deleteBtn, 4);

        row.Children.Add(keyBox);
        row.Children.Add(valueBox);
        row.Children.Add(deleteBtn);
        QueryRowsPanel.Children.Add(row);
    }

    private void AddParameter_Click(object sender, RoutedEventArgs e)
    {
        AddQueryRow("", "");
        RebuildOutput();
    }

    private void RebuildOutput()
    {
        if (_parts is null)
            return;

        try
        {
            var query = QueryRowsPanel.Children.OfType<Grid>()
                .Select(g => new QueryParam(((TextBox)g.Children[0]).Text, ((TextBox)g.Children[1]).Text))
                .Where(q => !string.IsNullOrEmpty(q.Key))
                .ToList();

            var rebuilt = _parts with { Query = query };
            RebuiltBox.Text = UrlTool.Build(rebuilt);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void CopyRebuilt_Click(object sender, RoutedEventArgs e) => Ui.Copy(RebuiltBox.Text, CopyRebuiltBtn);
}
