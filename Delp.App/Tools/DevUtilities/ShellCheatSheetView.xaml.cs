using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Delp.App.Infrastructure;
using Delp.Core.Tools.DevUtilities;

namespace Delp.App.Tools.DevUtilities;

[Tool("shell-cheatsheet", "Shell Command Cheat Sheet", ToolCategory.DevUtilities,
    "Look up bash and PowerShell equivalents for common command-line tasks.",
    Keywords = "bash,powershell,linux,unix,commands,cheatsheet,terminal", Order = 140)]
public partial class ShellCheatSheetView : UserControl
{
    private bool _ready;

    public ShellCheatSheetView()
    {
        InitializeComponent();

        CategoryBox.Items.Add("All");
        foreach (var category in ShellCheatSheetData.Categories)
            CategoryBox.Items.Add(category);
        CategoryBox.SelectedIndex = 0;

        _ready = true;
        ApplyFilter();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (_ready)
            ApplyFilter();
    }

    private void ApplyFilter()
    {
        var category = CategoryBox.SelectedItem as string;
        var results = ShellCheatSheetData.Search(SearchBox.Text, category == "All" ? null : category);

        ResultsList.Items.Clear();
        foreach (var entry in results)
            ResultsList.Items.Add(BuildCard(entry));

        StatusText.Text = $"{results.Count} of {ShellCheatSheetData.All.Count} shown";
    }

    private Border BuildCard(ShellEntry entry)
    {
        var stack = new StackPanel();

        stack.Children.Add(new TextBlock
        {
            Text = entry.Task,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = Res("Brush.Fg0"),
        });

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());

        AddCommandRow(grid, 0, "bash $", entry.Bash);
        AddCommandRow(grid, 1, "PS >", entry.PowerShell);

        stack.Children.Add(grid);

        if (!string.IsNullOrWhiteSpace(entry.Notes))
        {
            stack.Children.Add(new TextBlock
            {
                Text = entry.Notes,
                Margin = new Thickness(0, 8, 0, 0),
                Style = (Style)FindResource("Text.Sub"),
            });
        }

        return new Border
        {
            Style = (Style)FindResource("Card"),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10),
            Child = stack,
        };
    }

    private void AddCommandRow(Grid grid, int row, string label, string command)
    {
        var margin = new Thickness(0, row == 0 ? 0 : 6, 10, 0);

        var labelText = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = margin,
            FontFamily = (FontFamily)FindResource("Font.Mono"),
            FontSize = 12,
            Foreground = Res("Brush.Fg2"),
            MinWidth = 46,
        };
        Grid.SetRow(labelText, row);
        Grid.SetColumn(labelText, 0);

        var box = new TextBox
        {
            Style = (Style)FindResource("TextBox.Mono"),
            IsReadOnly = true,
            Text = command,
            Margin = margin,
        };
        Grid.SetRow(box, row);
        Grid.SetColumn(box, 1);

        var copyBtn = new Button { Content = "Copy", Margin = margin };
        copyBtn.Click += (_, _) => Ui.Copy(command, copyBtn);
        Grid.SetRow(copyBtn, row);
        Grid.SetColumn(copyBtn, 2);

        grid.Children.Add(labelText);
        grid.Children.Add(box);
        grid.Children.Add(copyBtn);
    }

    private Brush Res(string key) => (Brush)FindResource(key);
}
