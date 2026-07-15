using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Delp.App.Controls;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;
using ICSharpCode.AvalonEdit;

namespace Delp.App.Tools.DevUtilities;

[Tool("gitignore", ".gitignore Generator", ToolCategory.DevUtilities,
    "Compose a .gitignore file from language, IDE, OS, and framework templates.",
    Keywords = "gitignore,git,ignore,templates,generator", Order = 35)]
public partial class GitignoreView : UserControl
{
    private sealed record TemplateRow(string Name, CheckBox Box);
    private sealed record GroupBlock(TextBlock Header, WrapPanel Panel, List<TemplateRow> Rows);

    private readonly List<GroupBlock> _groups = [];

    // Membership only (not insertion order): Recompose() always walks GitignoreData.Templates in
    // its canonical order and keeps whichever of those are in this set, so the merged output's
    // section order is stable regardless of the order the user clicked checkboxes in.
    private readonly HashSet<string> _selected = new(StringComparer.Ordinal);

    private TextEditor _output = null!;

    public GitignoreView()
    {
        InitializeComponent();

        _output = CodeEditors.Create(readOnly: true);
        OutputHost.Child = _output;

        BuildChecklist();
        UpdateSelectedStatus();
        Recompose();
    }

    private void BuildChecklist()
    {
        foreach (var group in GitignoreData.Groups)
        {
            var header = new TextBlock { Text = group.ToUpperInvariant(), Margin = new Thickness(0, 8, 0, 4) };
            header.SetResourceReference(StyleProperty, "Text.Section");
            ChecklistPanel.Children.Add(header);

            var panel = new WrapPanel();
            ChecklistPanel.Children.Add(panel);

            var rows = new List<TemplateRow>();
            foreach (var template in GitignoreData.Templates.Where(t => t.Group == group))
            {
                var box = new CheckBox
                {
                    Content = template.Name,
                    Tag = template.Name,
                    Margin = new Thickness(0, 0, 16, 8),
                };
                box.Checked += Template_Toggled;
                box.Unchecked += Template_Toggled;
                panel.Children.Add(box);
                rows.Add(new TemplateRow(template.Name, box));
            }

            _groups.Add(new GroupBlock(header, panel, rows));
        }
    }

    private void Template_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: string name } box)
            return;

        if (box.IsChecked == true)
            _selected.Add(name);
        else
            _selected.Remove(name);

        UpdateSelectedStatus();
        Recompose();
    }

    private void UpdateSelectedStatus() =>
        SelectedCountText.Text = _selected.Count == 1 ? "1 template selected" : $"{_selected.Count} templates selected";

    private void Recompose()
    {
        var names = GitignoreData.Templates.Select(t => t.Name).Where(_selected.Contains).ToList();
        try
        {
            _output.Text = GitignoreTool.Compose(names);
            ErrorText.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        foreach (var group in _groups)
            foreach (var row in group.Rows)
                row.Box.IsChecked = false;
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim();
        foreach (var group in _groups)
        {
            var anyVisible = false;
            foreach (var row in group.Rows)
            {
                var visible = q.Length == 0 || row.Name.Contains(q, StringComparison.OrdinalIgnoreCase);
                row.Box.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                anyVisible |= visible;
            }

            var groupVisibility = anyVisible ? Visibility.Visible : Visibility.Collapsed;
            group.Header.Visibility = groupVisibility;
            group.Panel.Visibility = groupVisibility;
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => Ui.Copy(_output.Text, CopyBtn);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = ".gitignore|*.gitignore|All files|*.*", FileName = ".gitignore" };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, _output.Text);
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
